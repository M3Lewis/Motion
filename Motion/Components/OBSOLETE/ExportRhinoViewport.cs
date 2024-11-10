using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Motion.Components.OBSOLETE
{
    public class ExportRhinoViewport : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("77B99E66-A02D-4B44-ACF1-F103E0293F7C");
        public ExportRhinoViewport()
          : base("ExportRhinoViewport", "ExportRhinoViewport",
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

            string fileName;
            double frame = 0;

            //if (!isSaveSeries)
            //{
            //    fileName = string.Format("{0}_{1}.png", iFullPath, "Capture");
            //    bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            //}

            IGH_DocumentObject unionSliderObject = this.Params.Input[7].Sources[0];
            string unionSliderObjectTypeName = unionSliderObject.GetType().Name;

            var views = Rhino.RhinoDoc.ActiveDoc.Views;
            views.RedrawEnabled = true;
            var myView = views.Find(iViewName, true);

            var viewCapture = new Rhino.Display.ViewCapture();
            viewCapture.Width = iWidth;
            viewCapture.Height = iHeight;
            viewCapture.ScaleScreenItems = false;
            viewCapture.DrawAxes = false;
            viewCapture.DrawGrid = false;
            viewCapture.DrawGridAxes = false;
            viewCapture.TransparentBackground = iIsTransparent;
            

            if (unionSliderObjectTypeName == "pOd_TimeLineSlider")//如果是pod slider
            {
                GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;
                //double sliderValue = Convert.ToDouble(unionSlider.Slider.Value);
                //MotionSliderAnimator sliderAnimator = new MotionSliderAnimator(unionSlider,iViewName,iWidth,iHeight,iIsTransparent);
                //sliderAnimator.Width = iWidth;
                //sliderAnimator.Height = iHeight;
                //sliderAnimator.Folder = iFullPath;
                //sliderAnimator.FrameCount = (int)unionSlider.Slider.Maximum;
                //sliderAnimator.MotionStartAnimation(ref unionSlider,sliderValue);

                frame = iFrame;
                Bitmap bitmap = new Bitmap(1,1) ;

                while (Convert.ToDecimal(frame) <= unionSlider.Slider.Maximum)
                {
                    
                    unionSlider.Slider.Value = Convert.ToDecimal(frame);
                    bitmap = viewCapture.CaptureToBitmap(myView);
                    fileName = string.Format("{0}{1}{2}{3}", iFullPath,Path.DirectorySeparatorChar, frame.ToString(),".png");
                    //fileName = string.Concat(iFullPath, "Frame_", frame.ToString() + ".png");

                    //if (System.IO.File.Exists(fileName))
                    //{
                    //    System.IO.File.Delete(fileName);
                    //}
                    oPath.Add(fileName);
                    if (bitmap == null)
                    {
                        RhinoApp.WriteLine($"Frame {frame} failed to render to DIB");
                    }

                    views.Redraw();
                    bitmap.Save(fileName, ImageFormat.Png);
                    bitmap.Dispose();
                    frame++;
                    
                }
                //bitmap.Dispose();
                DA.SetDataList(0, oPath);
            }

        }


       
    }
}