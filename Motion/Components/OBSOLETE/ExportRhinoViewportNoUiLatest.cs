using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Motion.Components.OBSOLETE
{
    public class ExportRhinoViewportNoUiLatest : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("B9987272-3140-47BF-AA55-9502EF782553");
        public ExportRhinoViewportNoUiLatest()
          : base("ExportRhinoViewportNoUiLatest", "ExportRhinoViewportNoUiLatest",
            "自动导出Rhino视窗图片，支持透明度，可指定slider范围",
            "Motion", "02_Export")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item);
            pManager.AddParameter(new Param_FilePath(), "Full Path", "P", "输出路径", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsTransparent", "T", "图片是否透明", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsCycles", "C", "是否使用Cycles渲染", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Passes", "P", "Cycles渲染次数", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frame", "F", "当前帧", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Range", "R", "导出范围（可选）", GH_ParamAccess.item);
            pManager[9].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "路径", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string iViewName = "";
            int iWidth = 1920;
            int iHeight = 1080;
            string iFullPath = "";
            bool iIsTransparent = false;
            bool iIsCycles = false;
            int iRealtimeRenderPasses = 1;
            bool iRun = false;
            double iFrame = 0;
            Interval iRange = new Interval(0, 0);
            bool iIsCustomRange = false;
            List<string> oPath = new List<string>();

            if (!DA.GetData(0, ref iViewName)) return;
            if (!DA.GetData(1, ref iWidth)) return;
            if (!DA.GetData(2, ref iHeight)) return;
            if (!DA.GetData(3, ref iFullPath)) return;
            if (!DA.GetData(4, ref iIsTransparent)) return;
            if (!DA.GetData(5, ref iIsCycles)) return;
            if (!DA.GetData(6, ref iRealtimeRenderPasses)) return;
            if (!DA.GetData(7, ref iRun)) return;
            if (!DA.GetData(8, ref iFrame)) return;
            iIsCustomRange = DA.GetData(9, ref iRange);

            if (!iRun) return;

            if (Params.Input[8].Sources.Count == 0) return;
            IGH_DocumentObject unionSliderObject = Params.Input[8].Sources[0];
            if (!(unionSliderObject is GH_NumberSlider)) return;

            var views = RhinoDoc.ActiveDoc.Views;
            views.RedrawEnabled = true;

            GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;
            MotionSliderAnimatorNoUiLatest sliderAnimator = new MotionSliderAnimatorNoUiLatest(unionSlider);

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

            if (!sliderAnimator.SetupAnimationProperties()) return;
            sliderAnimator.MotionStartAnimation(iIsTransparent, iViewName, iIsCycles, iRealtimeRenderPasses, out oPath);

            DA.SetDataList(0, oPath);
        }

        
    }
}