using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Motion.UI;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Motion
{

    public class ExportSliderAnimation : MotionButtonComponent
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("7b8d5ff6-c766-4ae3-a832-95861edb9fde");

        private string currentPath = "";  // 添加字段保存当前路径
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
            Attributes = new MotionButton(this, "Open", (o, e) =>
            {
                // 在按钮点击时直接打开文件夹
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    OpenDirectoryWithDirectoryOpus(currentPath);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"找不到路径文件夹。");
                }
            });
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item,"Perspective");
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item,1920);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item,1080);
            pManager.AddParameter(new Param_FilePath(), "Full Path", "P", "输出路径", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsTransparent", "T", "图片是否透明", GH_ParamAccess.item,true);
            pManager.AddBooleanParameter("IsCycles", "C", "是否使用Cycles渲染", GH_ParamAccess.item,false);
            pManager.AddIntegerParameter("Passes", "Pa", "Cycles渲染次数", GH_ParamAccess.item,500);
            pManager.AddIntervalParameter("Range", "Ra", "导出范围（可选）", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frame", "F", "当前帧", GH_ParamAccess.item);

            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output Path", "Op", "导出路径", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        protected override async void SolveInstance(IGH_DataAccess DA)
        {
            string iViewName = "";
            int iWidth = 1920;
            int iHeight = 1080;
            string iFullPath = "";
            bool iIsTransparent = false;
            bool iIsCycles = false;
            int iRealtimeRenderPasses = 1;
            Interval iRange = new Interval(0, 0);
            bool iRun = false;
            double iFrame = 0;

            bool iIsCustomRange = false;
            List<string> oPath = new List<string>();

            if (!DA.GetData(0, ref iViewName))
                return;
            if (!DA.GetData(1, ref iWidth))
                return;
            if (!DA.GetData(2, ref iHeight))
                return;
            if (!DA.GetData(3, ref iFullPath))
                return;
            if (!DA.GetData(4, ref iIsTransparent))
                return;
            if (!DA.GetData(5, ref iIsCycles))
                return;
            if (!DA.GetData(6, ref iRealtimeRenderPasses))
                return;

            iIsCustomRange = DA.GetData(7, ref iRange);

            if (!DA.GetData(8, ref iRun))
                return;
            if (!DA.GetData(9, ref iFrame))
                return;

            if (!iRun)
                return;

            currentPath = iFullPath;

            if (this.Params.Input[9].Sources.Count == 0)
                return;
            IGH_DocumentObject unionSliderObject = this.Params.Input[9].Sources[0];
            if (!(unionSliderObject is GH_NumberSlider))
                return;

            var views = RhinoDoc.ActiveDoc.Views;
            views.RedrawEnabled = true;

            GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;

            await Task.Run(() =>
           {
               int currentFrame = 0;
               int total =  (int)unionSlider.Slider.Maximum;
               // 创建进度更新的委托
               Action<int, int> updateProgress = (frame, total) =>
               {
                   this.Message = $"Rendering Frame.. {frame + 1}/{total}";

                   // 强制组件重绘以更新显示
                   this.OnDisplayExpired(true);
               };

               Rhino.RhinoApp.InvokeOnUiThread(
                   new Action(() =>
                   {
                       MotionSliderAnimator sliderAnimator =
                           new MotionSliderAnimator(unionSlider);

                       sliderAnimator.Width = iWidth;
                       sliderAnimator.Height = iHeight;
                       sliderAnimator.Folder = iFullPath;

                       if (iIsCustomRange)
                       {
                           sliderAnimator.CustomRange = iRange;
                           sliderAnimator.FrameCount = (int)(iRange.Max - iRange.Min + 1);
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
                           iIsTransparent,
                           iViewName,
                           iIsCycles,
                           iRealtimeRenderPasses,
                           out oPath,
                           (frame, total) => { updateProgress(frame, total); }// 传入进度更新回调
                       );

                       DA.SetDataList(0, oPath);
                   })
               );
           });
        }
        static void OpenDirectoryWithDirectoryOpus(string path)
        {
            try
            {
                // 使用 Process.Start 启动 Directory Opus 并传递路径参数
                Process.Start("dopus.exe", $"/cmd \"{path}\"");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"无法打开目录: {ex.Message}");
            }
        }
    }
}
