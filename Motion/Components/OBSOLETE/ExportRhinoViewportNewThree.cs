using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Motion.Components.OBSOLETE
{
    public class ExportRhinoViewportNewThree : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("a7286403-3481-4a1f-9712-c344361115ae");

        private bool isExporting = false;
        private Task exportTask = null;
        public ExportRhinoViewportNewThree()
          : base("ExportRhinoViewportNewThree", "ExportRhinoViewportNewThree",
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


            IGH_DocumentObject unionSliderObject = this.Params.Input[7].Sources[0];
            string unionSliderObjectTypeName = unionSliderObject.GetType().Name;
            GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;
            

            List<string> outputPath = new List<string>();
            double sliderValue = Convert.ToDouble(unionSlider.Slider.Value);
            int maxValue = (int)unionSlider.Slider.Maximum;
            
            
            if (iRun && !isExporting)
            {
                isExporting = true;
                if (unionSliderObjectTypeName == "pOd_TimeLineSlider")//如果是pod slider
                {
                    exportTask = Task.Run(() => ExportSliderViewports(iRun, unionSlider, iWidth, iHeight, sliderValue, iFullPath, out oPath));
                    DA.SetDataList(0, oPath);
                }
            }

            else if (!iRun && isExporting)
            {
                // 如果export变为false，尝试取消导出任务
                isExporting = false;
                if (exportTask != null && !exportTask.IsCompleted)
                {
                    // 这里可以添加取消任务的逻辑，如果GH_SliderAnimator支持的话
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Attempting to cancel export...");
                }
            }

            if (isExporting)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Exporting in progress...");
            }
        }
        private void ExportSliderViewports(bool run,GH_NumberSlider slider, int width, int height, double sliderValue, string fullPath, out List<string> outputPath)
        {
            GH_SliderAnimator animator = new GH_SliderAnimator(slider);
            outputPath = new List<string>();
            // 设置导出属性
            //animator.Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SliderExport");
            if (!run) return;
            animator.Folder = fullPath;
            animator.FileTemplate = "Frame_{0:0}.png";  // 确保使用.png扩展名
            animator.FrameCount = (int)slider.Slider.Maximum;
            animator.Width = width;
            animator.Height = height;
            animator.Viewport = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            animator.DrawTag = false;  // 可选：在图片上绘制帧信息

            string arg = string.Format(animator.FileTemplate, sliderValue);
            string text = string.Format("{0}{2}{1}", fullPath, arg, Path.DirectorySeparatorChar);
            outputPath.Add(text);
            // 开始导出
            try
            {
                int exportedFrames = animator.StartAnimation();
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Exported {exportedFrames} PNG frames to {animator.Folder}");
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Export failed: {ex.Message}");
            }
        }

       
    }
}