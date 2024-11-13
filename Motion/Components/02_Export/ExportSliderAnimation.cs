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
using System.Threading.Tasks;

namespace Motion
{
    public class ExportSliderAnimation : MotionButtonComponent
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("7b8d5ff6-c766-4ae3-a832-95861edb9fde");

        

        public ExportSliderAnimation()
            : base(
                "ExportSliderAnimation",
                "ExportSliderAnimation",
                "Slider自动导出Rhino视窗图片，支持透明背景和Cycles模式，可指定slider范围",
                "Motion",
                "02_Export"
            )
        { }

        public override void CreateAttributes()
        {
            Attributes = new MotionButton(this, "Open", "Export", async (sender, e, isExport) =>
            {
                if (isExport)
                {
                    // 直接执行渲染，不通过 Run 输入端
                    await ExecuteRenderingAsync();
                }
                else
                {
                    // 从输入端获取路径
                    string pathString = "";
                    if (this.Params.Input[3].SourceCount > 0)
                    {
                        var pathData = this.Params.Input[3].VolatileData.AllData(true).FirstOrDefault();
                        if (pathData != null)
                        {
                            pathString = pathData.ToString();
                        }
                    }
                    else
                    {
                        // 如果没有连接源，使用默认值
                        pathString = this.Params.Input[3].VolatileData.AllData(true).FirstOrDefault()?.ToString();
                    }

                    // 执行打开文件夹功能
                    if (!string.IsNullOrEmpty(pathString) && Directory.Exists(pathString))
                    {
                        OpenDirectoryWithDirectoryOpus(pathString);
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"找不到路径文件夹。");
                    }
                }
            });
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item, "Perspective");
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item, 1920);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item, 1080);

            // 添加文件路径参数
            Param_FilePath pathParam = new Param_FilePath();
            pathParam.SetPersistentData(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\Motion");  // 设置默认为桌面路径
            pManager.AddParameter(pathParam, "Full Path", "P", "输出路径", GH_ParamAccess.item);
            
            pManager.AddBooleanParameter("IsTransparent", "T", "图片是否透明", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("IsCycles", "C", "是否使用Cycles渲染", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Passes", "Pa", "Cycles渲染次数", GH_ParamAccess.item, 500);
            pManager.AddIntervalParameter("Range", "Ra", "导出范围（可选）", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frame", "F", "当前帧", GH_ParamAccess.item);

            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output Path", "Op", "·", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        protected override async void SolveInstance(IGH_DataAccess DA)
        {
            // 获取所有输入参数
            if (!GetInputParams(DA, out RenderParameters parameters))
                return;

            //currentPath = parameters.FullPath;

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
                // 获取并转换每个输入参数
                var viewNameGoo = this.Params.Input[0].VolatileData.AllData(true).First();
                parameters.ViewName = viewNameGoo.ToString();

                var widthGoo = this.Params.Input[1].VolatileData.AllData(true).First();
                parameters.Width = Convert.ToInt32(widthGoo.ToString());

                var heightGoo = this.Params.Input[2].VolatileData.AllData(true).First();
                parameters.Height = Convert.ToInt32(heightGoo.ToString());

                var pathGoo = this.Params.Input[3].VolatileData.AllData(true).First();
                parameters.FullPath = pathGoo.ToString();

                var transparentGoo = this.Params.Input[4].VolatileData.AllData(true).First();
                parameters.IsTransparent = Convert.ToBoolean(transparentGoo.ToString());

                var cyclesGoo = this.Params.Input[5].VolatileData.AllData(true).First();
                parameters.IsCycles = Convert.ToBoolean(cyclesGoo.ToString());

                var passesGoo = this.Params.Input[6].VolatileData.AllData(true).First();
                parameters.RealtimeRenderPasses = Convert.ToInt32(passesGoo.ToString());

                // 处理可选的区间参数
                var rangeGoo = this.Params.Input[7].VolatileData.AllData(true).FirstOrDefault();
                if (rangeGoo != null)
                {
                    if (rangeGoo is GH_Interval ghInterval)
                    {
                        parameters.Range = ghInterval.Value;
                        parameters.IsCustomRange = true;
                    }
                }

                parameters.Run = true;  // 按钮点击时总是为 true

                var frameGoo = this.Params.Input[9].VolatileData.AllData(true).First();
                parameters.Frame = Convert.ToDouble(frameGoo.ToString());

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
            // 检查 Frame 输入源
            if (this.Params.Input[9].Sources.Count == 0)
                return;
            
            if (!(this.Params.Input[9].Sources[0] is GH_NumberSlider unionSlider))
                return;

            var views = RhinoDoc.ActiveDoc.Views;
            views.RedrawEnabled = true;

            await Task.Run(() =>
            {
                int currentFrame = 0;
                int total = (int)unionSlider.Slider.Maximum;
                
                Action<int, int> updateProgress = (frame, total) =>
                {
                    this.Message = $"Rendering Frame.. {frame + 1}/{total}";
                    this.OnDisplayExpired(true);
                };

                List<string> outputPaths = new List<string>();

                RhinoApp.InvokeOnUiThread(new Action(() =>
                {
                    var sliderAnimator = new MotionSliderAnimator(unionSlider)
                    {
                        Width = parameters.Width,
                        Height = parameters.Height,
                        Folder = parameters.FullPath
                    };

                    if (parameters.IsCustomRange)
                    {
                        sliderAnimator.CustomRange = parameters.Range;
                        sliderAnimator.FrameCount = (int)(parameters.Range.Max - parameters.Range.Min + 1);
                        sliderAnimator.UseCustomRange = true;
                    }
                    else
                    {
                        sliderAnimator.FrameCount = (int)unionSlider.Slider.Maximum;
                        sliderAnimator.UseCustomRange = false;
                    }

                    if (!sliderAnimator.SetupAnimationProperties())
                        return;

                    currentFrame = sliderAnimator.MotionStartAnimation(
                        parameters.IsTransparent,
                        parameters.ViewName,
                        parameters.IsCycles,
                        parameters.RealtimeRenderPasses,
                        out outputPaths,
                        updateProgress
                    );
                }));

                // 在主线程中更新输出
                if (DA != null)
                {
                    RhinoApp.InvokeOnUiThread(new Action(() =>
                    {
                        DA.SetDataList(0, outputPaths);
                    }));
                }
            });
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
                // ʹ Process.Start  Directory Opus ·
                Process.Start("dopus.exe", $"/cmd \"{path}\"");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"无法打开目录: {ex.Message}");
            }
        }
    }
}