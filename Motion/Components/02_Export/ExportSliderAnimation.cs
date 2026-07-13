using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.Animation;
using Motion.UI;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Motion.Export
{
    public class ExportSliderAnimation : MotionButtonComponent
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.ExportSliderAnimation;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("7b8d5ff6-c766-4ae3-a832-95861edb9fde");

        // 类成员：渲染状态锁与取消控制器
        private bool _isRendering = false;
        private CancellationTokenSource _cancellationTokenSource;

        public ExportSliderAnimation()
            : base(
                "Export Slider Animation",
                "Export Slider Animation",
                "Slider自动导出Rhino视窗图片，支持透明背景和Cycles模式，可指定slider范围",
                "Motion",
                "02_Export"
            )
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item, "Perspective");
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item, 1920);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item, 1080);

            Param_FilePath pathParam = new Param_FilePath();
            pathParam.SetPersistentData(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Motion");
            pManager.AddParameter(pathParam, "Full Path", "P", "输出路径", GH_ParamAccess.item);

            pManager.AddBooleanParameter("IsTransparent", "T", "图片是否透明", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("IsCycles", "Cy", "是否使用Cycles渲染", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Passes", "Pa", "Cycles渲染次数", GH_ParamAccess.item, 500);
            pManager.AddIntervalParameter("Range Domain", "D", "导出范围（可选）", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frame", "F", "当前帧", GH_ParamAccess.item);

            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 无输出端
        }

        public override void CreateAttributes()
        {
            Attributes = new MotionButton(this, "Export", "Open", (sender, e, isExport) =>
            {
                if (isExport)
                {
                    // 按钮点击时触发纯同步渲染
                    ExecuteRendering();
                    return;
                }

                string pathString = GetOutputPath();
                if (string.IsNullOrEmpty(pathString) || !Directory.Exists(pathString))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "找不到路径文件夹。");
                    return;
                }
                
                OpenDirectoryWithDirectoryOpus(pathString);
            });
        }

        private string GetOutputPath()
        {
            if (this.Params.Input[3].SourceCount <= 0)
            {
                return this.Params.Input[3].VolatileData.AllData(true).FirstOrDefault()?.ToString();
            }

            var pathData = this.Params.Input[3].VolatileData.AllData(true).FirstOrDefault();
            return pathData?.ToString();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!GetInputParams(DA, out RenderParameters parameters))
                return;

            if (!parameters.Run)
                return;

            // GH 解算器中触发纯同步渲染
            ExecuteRenderingWithParams(parameters);
        }

        private void ExecuteRendering()
        {
            var parameters = new RenderParameters();
            if (!GetCurrentParams(parameters))
                return;

            ExecuteRenderingWithParams(parameters);
        }

        private bool GetCurrentParams(RenderParameters parameters)
        {
            var indicesToCheck = new[] { 0, 1, 2, 3, 4, 5, 6, 9 };
            foreach (var idx in indicesToCheck)
            {
                if (idx >= this.Params.Input.Count || this.Params.Input[idx].VolatileDataCount == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"获取参数失败: 缺少必需的输入参数 [{this.Params.Input[idx].Name}]");
                    return false;
                }
            }

            try
            {
                var inputs = new object[]
                {
                    this.Params.Input[0].VolatileData.AllData(true).First(),
                    this.Params.Input[1].VolatileData.AllData(true).First(),
                    this.Params.Input[2].VolatileData.AllData(true).First(),
                    this.Params.Input[3].VolatileData.AllData(true).First(),
                    this.Params.Input[4].VolatileData.AllData(true).First(),
                    this.Params.Input[5].VolatileData.AllData(true).First(),
                    this.Params.Input[6].VolatileData.AllData(true).First(),
                    this.Params.Input[9].VolatileData.AllData(true).First()
                };

                parameters.ViewName = inputs[0].ToString();
                parameters.Width = Convert.ToInt32(inputs[1].ToString());
                parameters.Height = Convert.ToInt32(inputs[2].ToString());
                parameters.FullPath = inputs[3].ToString();
                parameters.IsTransparent = Convert.ToBoolean(inputs[4].ToString());
                parameters.IsCycles = Convert.ToBoolean(inputs[5].ToString());
                parameters.RealtimeRenderPasses = Convert.ToInt32(inputs[6].ToString());
                parameters.Frame = Convert.ToDouble(inputs[7].ToString());

                var rangeGoo = this.Params.Input[7].VolatileData.AllData(true).FirstOrDefault();
                if (rangeGoo != null && rangeGoo is GH_Interval ghInterval)
                {
                    parameters.Range = ghInterval.Value;
                    parameters.IsCustomRange = true;
                }

                parameters.Run = true;
                return true;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"解析参数发生错误: {ex.Message}");
                return false;
            }
        }

        // 核心执行方法：完全重构为纯同步的 void 方法
        private void ExecuteRenderingWithParams(RenderParameters parameters)
        {
            // 状态锁，防止并发重入引发崩溃
            if (_isRendering)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "渲染已经在运行中，请先等待当前任务结束。");
                return;
            }

            _isRendering = true;
            var stopwatch = Stopwatch.StartNew();
            bool wasAborted = false;

            try
            {
                // 1. 数据校验与合法性检查
                if (this.Params.Input[9].Sources.Count == 0) return;
                var source = this.Params.Input[9].Sources[0];
                if (!(source is MotionSlider motionSlider)) return;

                string driveLetter = Path.GetPathRoot(parameters.FullPath);
                if (string.IsNullOrEmpty(driveLetter) || !Directory.Exists(driveLetter))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"找不到盘符: {driveLetter}");
                    return;
                }

                var views = RhinoDoc.ActiveDoc.Views;
                var targetView = views.Find(parameters.ViewName, false);
                if (targetView == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"找不到视图: {parameters.ViewName}");
                    return;
                }

                if (parameters.IsCustomRange)
                {
                    var sliderRange = new Interval((double)motionSlider.Slider.Minimum, (double)motionSlider.Slider.Maximum);
                    if (!sliderRange.IncludesInterval(parameters.Range))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"自定义区间 [{parameters.Range.Min}-{parameters.Range.Max}] 超出Slider范围 [{sliderRange.Min}-{sliderRange.Max}]");
                        return;
                    }

                    if (parameters.Range.T0 > parameters.Range.T1)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "自定义区间最小值必须小于最大值");
                        return;
                    }
                }

                if (parameters.IsCycles && parameters.RealtimeRenderPasses < 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cycles渲染通道数必须大于0");
                    return;
                }

                // 修复原代码中的 Bug：此处应检查用户指定的目标视图 targetView 的显示模式，而不是活跃视窗 activeView
                var displayMode = targetView.ActiveViewport.DisplayMode;
                string modeName = displayMode.EnglishName;
                bool isRaytracedMode = modeName == "Raytraced" || modeName == "光线跟踪";

                if (parameters.IsCycles && !isRaytracedMode)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "请将目标视图切换至光线跟踪(Raytraced)模式!");
                    return;
                }

                views.RedrawEnabled = true;

                // 2. 初始化取消控制器（同步执行中用于监听 Esc 或外部中断）
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // 3. 进度与取消回调
                Action<int, int> updateProgress = (frame, total) =>
                {
                    this.Message = $"Rendering Frame.. {frame + 1}/{total}";
                    this.OnDisplayExpired(true);
                    
                    // 检测键盘 Esc 键
                    if (Control.ModifierKeys == Keys.Escape)
                    {
                        _cancellationTokenSource?.Cancel();
                        wasAborted = true;
                    }
                    
                    // 同步执行时，利用此方法泵送 Windows 消息队列。
                    // 它可以刷新界面、更新 this.Message 的气泡文字，并且允许用户在点击按钮时再次触发取消事件。
                    Application.DoEvents(); 
                };

                // 4. 执行同步阻塞渲染
                using (var sliderAnimator = new MotionSliderAnimator(motionSlider))
                {
                    sliderAnimator.Width = parameters.Width;
                    sliderAnimator.Height = parameters.Height;
                    sliderAnimator.Folder = parameters.FullPath;
                    sliderAnimator.CancellationToken = token;

                    if (parameters.IsCustomRange)
                    {
                        sliderAnimator.CustomRange = parameters.Range;
                        sliderAnimator.FrameCount = (int)(parameters.Range.Length + 1);
                        sliderAnimator.UseCustomRange = true;
                    }
                    else
                    {
                        sliderAnimator.FrameCount = (int)motionSlider.Slider.Maximum;
                        sliderAnimator.UseCustomRange = false;
                    }

                    if (!sliderAnimator.SetupAnimationProperties())
                        return;

                    sliderAnimator.MotionStartAnimation(
                        parameters.IsTransparent,
                        parameters.ViewName,
                        parameters.IsCycles,
                        parameters.RealtimeRenderPasses,
                        out _,
                        out wasAborted,
                        updateProgress
                    );
                }

                stopwatch.Stop();
                string elapsedTime = FormatTimeSpan(stopwatch.Elapsed);
                this.Message = wasAborted 
                    ? $"Render Cancelled!\nTime: {elapsedTime}"
                    : $"Render Finished!\nTime: {elapsedTime}";
                    
                RhinoApp.WriteLine($"Render {(wasAborted ? "cancelled" : "finished")} at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                this.Message = $"Error: {ex.Message}\nTime: {FormatTimeSpan(stopwatch.Elapsed)}";
                RhinoApp.WriteLine($"Render error occurred at {DateTime.Now:HH:mm:ss}");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"渲染发生异常: {ex.Message}");
            }
            finally
            {
                // 清理 Token 资源
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                // 必须释放状态锁，恢复后续的触发功能
                _isRendering = false;
            }
        }

        private bool GetInputParams(IGH_DataAccess DA, out RenderParameters parameters)
        {
            parameters = new RenderParameters();

            if (!DA.GetData(0, ref parameters.ViewName)) return false;
            if (!DA.GetData(1, ref parameters.Width)) return false;
            if (!DA.GetData(2, ref parameters.Height)) return false;
            if (!DA.GetData(3, ref parameters.FullPath)) return false;
            if (!DA.GetData(4, ref parameters.IsTransparent)) return false;
            if (!DA.GetData(5, ref parameters.IsCycles)) return false;
            if (!DA.GetData(6, ref parameters.RealtimeRenderPasses)) return false;

            parameters.IsCustomRange = DA.GetData(7, ref parameters.Range);

            if (!DA.GetData(8, ref parameters.Run)) return false;
            if (!DA.GetData(9, ref parameters.Frame)) return false;

            return true;
        }

        private class RenderParameters
        {
            public string ViewName = "";
            public int Width = 1920;
            public int Height = 1080;
            public string FullPath = "";
            public bool IsTransparent = false;
            public bool IsCycles = false;
            public int RealtimeRenderPasses = 1;
            public Interval Range = new Interval(0, 0);
            public bool Run = false;
            public double Frame = 0;
            public bool IsCustomRange = false;
        }

        private static void OpenDirectoryWithDirectoryOpus(string path)
        {
            try
            {
                bool hasDOpus = IsDirectoryOpusInstalled();
                if (hasDOpus)
                {
                    Process.Start("dopus.exe", $"/cmd \"{path}\"");
                    return;
                }
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"无法打开目录: {ex.Message}");
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception innerEx)
                {
                    RhinoApp.WriteLine($"备用方法也无法打开目录: {innerEx.Message}");
                }
            }
        }
        
        private static bool IsDirectoryOpusInstalled()
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return false;
                
            return path.Split(';')
                .Where(p => !string.IsNullOrEmpty(p))
                .Any(p => File.Exists(Path.Combine(p.Trim(), "dopus.exe")));
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            if (this.Params.Input[9].Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            var timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault();

            if (timelineSlider == null) return;
            
            var frameParam = Params.Input[9];
            frameParam.AddSource(timelineSlider);
            frameParam.WireDisplay = GH_ParamWireDisplay.faint;
            
            ExpireSolution(true);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h{timeSpan.Minutes}m{timeSpan.Seconds}s";
            
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m{timeSpan.Seconds}s";
            
            return $"{timeSpan.Seconds}s";
        }
        
        protected override void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
            base.Dispose();
        }
    }
}