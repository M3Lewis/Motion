using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.UI;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Motion.Export
{
    public class ExportSliderAnimation : MotionButtonComponent
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.ExportSliderAnimation;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("7b8d5ff6-c766-4ae3-a832-95861edb9fde");

        public ExportSliderAnimation()
            : base(
                "Export Slider Animation",
                "Export Slider Animation",
                "Slider自动导出Rhino视窗图片，支持透明背景和Cycles模式，可指定slider范围",
                "Motion",
                "02_Export"
            )
        { }

        public override void CreateAttributes()
        {
            Attributes = new MotionButton(this, "Export", "Open", async (sender, e, isExport) =>
            {
                if (isExport)
                {
                    await ExecuteRenderingAsync();
                    return;
                }

                // 从输入端获取路径
                string pathString = GetOutputPath();
                
                // 执行打开文件夹功能
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

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item, "Perspective");
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item, 1920);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item, 1080);

            // 添加文件路径参数
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
            // 输出端不需要，已被注释掉
            // pManager.AddTextParameter("Output Path", "Op", "·", GH_ParamAccess.list);
        }

        protected override async void SolveInstance(IGH_DataAccess DA)
        {
            // 获取所有输入参数
            if (!GetInputParams(DA, out RenderParameters parameters))
                return;

            if (!parameters.Run)
                return;

            await ExecuteRenderingWithParams(parameters, DA);
        }

        private async Task ExecuteRenderingAsync()
        {
            // 获取当前组件的参数
            var parameters = new RenderParameters();
            if (!GetCurrentParams(parameters))
                return;

            await ExecuteRenderingWithParams(parameters, null);
        }

        private bool GetCurrentParams(RenderParameters parameters)
        {
            try
            {
                // 获取并转换每个输入参数，使用索引数组简化代码
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

                // 处理可选的区间参数
                var rangeGoo = this.Params.Input[7].VolatileData.AllData(true).FirstOrDefault();
                if (rangeGoo != null && rangeGoo is GH_Interval ghInterval)
                {
                    parameters.Range = ghInterval.Value;
                    parameters.IsCustomRange = true;
                }

                parameters.Run = true;  // 按钮点击时总是为 true
                return true;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"获取参数失败: {ex.Message}");
                return false;
            }
        }

        private async Task ExecuteRenderingWithParams(RenderParameters parameters, IGH_DataAccess DA = null)
        {
            if (this.Params.Input[9].Sources.Count == 0)
                return;

            var source = this.Params.Input[9].Sources[0];
            if (!(source is GH_NumberSlider unionSlider))
                return;

            var views = RhinoDoc.ActiveDoc.Views;
            var activeView = views.ActiveView;
            var displayMode = activeView.ActiveViewport.DisplayMode;
            string modeName = displayMode.EnglishName;
            bool isRaytracedMode = modeName == "Raytraced" || modeName == "光线跟踪";

            views.RedrawEnabled = true;

            // 添加计时器
            var stopwatch = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource();
            bool wasAborted = false;

            try 
            {
                Action<int, int> updateProgress = (frame, total) =>
                {
                    this.Message = $"Rendering Frame.. {frame + 1}/{total}";
                    this.OnDisplayExpired(true);
                    
                    // 检查ESC键
                    if (Control.ModifierKeys == Keys.Escape)
                    {
                        cts.Cancel();
                        wasAborted = true;
                    }
                    
                    Application.DoEvents();
                };

                await Task.Run(() =>
                {
                    RhinoApp.InvokeOnUiThread(new Action(() =>
                    {
                        // 检查渲染模式
                        if (parameters.IsCycles && !isRaytracedMode)
                        {
                            this.Message = "请打开光线跟踪(Raytraced)模式!";
                            return;
                        }
                        
                        var sliderAnimator = new MotionSliderAnimator(unionSlider)
                        {
                            Width = parameters.Width,
                            Height = parameters.Height,
                            Folder = parameters.FullPath,
                        };

                        // 设置范围
                        if (parameters.IsCustomRange)
                        {
                            sliderAnimator.CustomRange = parameters.Range;
                            sliderAnimator.FrameCount = (int)(parameters.Range.Length + 1);
                            sliderAnimator.UseCustomRange = true;
                        }
                        else
                        {
                            sliderAnimator.FrameCount = (int)unionSlider.Slider.Maximum;
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

                        stopwatch.Stop();
                        string elapsedTime = FormatTimeSpan(stopwatch.Elapsed);
                        
                        this.Message = wasAborted 
                            ? $"Render Cancelled!\nTime: {elapsedTime}"
                            : $"Render Finished!\nTime: {elapsedTime}";
                            
                        RhinoApp.WriteLine($"Render {(wasAborted ? "cancelled" : "finished")} at {DateTime.Now:HH:mm:ss}");
                    }));
                });
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                this.Message = $"Render Cancelled!\nTime: {FormatTimeSpan(stopwatch.Elapsed)}";
                RhinoApp.WriteLine($"Render cancelled at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                this.Message = $"Error: {ex.Message}\nTime: {FormatTimeSpan(stopwatch.Elapsed)}";
                RhinoApp.WriteLine($"Render error occured at {DateTime.Now:HH:mm:ss}");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Render error occured: {ex.Message}");
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
                // 检查系统是否安装了Directory Opus
                bool hasDOpus = IsDirectoryOpusInstalled();
                
                if (hasDOpus)
                {
                    // 使用 Directory Opus 打开目录
                    Process.Start("dopus.exe", $"/cmd \"{path}\"");
                    return;
                }
                
                // 使用系统默认的资源管理器打开目录
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"无法打开目录: {ex.Message}");
                
                // 尝试使用最基本的方式打开目录
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

            // 检查 Frame 输入端是否已有连接
            if (this.Params.Input[9].Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找 TimeLine(Union) Slider
            var timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase));

            if (timelineSlider == null) return;
            
            // 获取 Frame 输入参数并添加数据连接
            var frameParam = Params.Input[9];
            frameParam.AddSource(timelineSlider);
            frameParam.WireDisplay = GH_ParamWireDisplay.faint;
            
            // 强制更新组件
            ExpireSolution(true);
        }

        // 格式化时间的辅助方法
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h{timeSpan.Minutes}m{timeSpan.Seconds}s";
            
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m{timeSpan.Seconds}s";
            
            return $"{timeSpan.Seconds}s";
        }
    }
}
