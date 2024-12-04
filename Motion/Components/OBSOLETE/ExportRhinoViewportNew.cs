using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Motion.Components.OBSOLETE
{
    public class ExportRhinoViewportNew : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("77B99E55-A02D-4B44-ACF1-F103E3344F7C");
        public ExportRhinoViewportNew()
          : base("ExportRhinoViewportNew", "ExportRhinoViewportNew",
            "导出Rhino视窗图片，支持透明度",
            "Motion", "02_Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "V", "视图名称", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Image Width", "W", "图片宽度", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Image Height", "H", "图片高度", GH_ParamAccess.item);
            pManager.AddTextParameter("Full Path", "P", "导出路径", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsTransparent", "T", "图片是否透明", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsSaveSeries", "S", "是否导出序列", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frame", "F", "当前帧", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
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
            bool iIsSaveSeries = false;
            bool iRun = false;
            double iFrame = 0;
            List<string> oPath = new List<string>();

            DA.GetData(0, ref iViewName);
            DA.GetData(1, ref iWidth);
            DA.GetData(2, ref iHeight);
            DA.GetData(3, ref iFullPath);
            DA.GetData(4, ref iIsTransparent);
            DA.GetData(5, ref iIsSaveSeries);
            DA.GetData(6, ref iRun);
            DA.GetData(7, ref iFrame);

            if (!iRun) return;

            var views = Rhino.RhinoDoc.ActiveDoc.Views;
            var myView = views.Find(iViewName, true);

            var viewCapture = new Rhino.Display.ViewCapture();
            viewCapture.Width = iWidth;
            viewCapture.Height = iHeight;
            viewCapture.ScaleScreenItems = false;
            viewCapture.DrawAxes = false;
            viewCapture.DrawGrid = false;
            viewCapture.DrawGridAxes = false;
            viewCapture.TransparentBackground = iIsTransparent;


            var bitmap = viewCapture.CaptureToBitmap(myView);

            double frame = 0;

            //if (!isSaveSeries)
            //{
            //    fileName = string.Format("{0}_{1}.png", iFullPath, "Capture");
            //    bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            //}

            //IGH_DocumentObject unionSliderObject = this.OnPingDocument().Objects.Where((IGH_DocumentObject o)
            //   => Grasshopper.Utility.LikeOperator(o.NickName, "TimeLine(Union)")).ToList()[0];

            IGH_DocumentObject unionSliderObject = this.Params.Input[7].Sources[0];
           string unionSliderObjectTypeName = unionSliderObject.GetType().Name;

            if (unionSliderObjectTypeName == "pOd_TimeLineSlider")//如果是pod slider
            {
                GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;

                frame = iFrame;
                List<string> outputPath = new List<string>();

                double sliderValue = Convert.ToDouble(unionSlider.Slider.Value);
                MotionSliderAnimatorOne sliderAnimator = new MotionSliderAnimatorOne(unionSlider, iViewName, iWidth, iHeight, iIsTransparent);
                sliderAnimator.Width = iWidth;
                sliderAnimator.Height = iHeight;
                sliderAnimator.Folder = iFullPath;
                sliderAnimator.FrameCount = (int)unionSlider.Slider.Maximum;
                sliderAnimator.MotionStartAnimation(ref unionSlider, sliderValue, out outputPath);

                //StartAsyncAnimation(sliderAnimator, unionSlider, sliderValue);

                oPath = outputPath;

            }
            DA.SetDataList(0, oPath);
        }

        //private void StartAsyncAnimation(MotionSliderAnimator sliderAnimator, GH_NumberSlider slider, double startValue)
        //{
        //    GH_Document doc = this.OnPingDocument();
        //    if (doc == null) return;

        //    double currentValue = startValue;
        //    double maximum = Convert.ToDouble(slider.Slider.Maximum);

        //    GH_Document.GH_ScheduleDelegate updateAction = null;
        //    updateAction = (GH_Document document) =>
        //    {
        //        if (currentValue <= maximum)
        //        {
        //            slider.Slider.Value = Convert.ToDecimal(currentValue);
        //            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

        //            Bitmap bitmap = sliderAnimator.MotionCreateFrame(sliderAnimator.ViewName, sliderAnimator.Width, sliderAnimator.Height, sliderAnimator.IsTransparent);
        //            if (bitmap != null)
        //            {
        //                string fileName = string.Format("{0}\\frame_{1}.png", sliderAnimator.Folder, currentValue);
        //                bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
        //                bitmap.Dispose();
        //            }

        //            currentValue++;
        //            doc.ScheduleSolution(5, updateAction);
        //        }
        //        else
        //        {
        //            // 动画结束，更新输出
        //            List<string> outputPath = Directory.GetFiles(sliderAnimator.Folder, "frame_*.png").ToList();
        //            this.Params.Output[0].ClearData();
        //            this.Params.Output[0].AddVolatileDataList(new Grasshopper.Kernel.Data.GH_Path(0), outputPath);
        //            doc.NewSolution(false);
        //        }
        //    };

        //    doc.ScheduleSolution(5, updateAction);
        //}

        
    }
}