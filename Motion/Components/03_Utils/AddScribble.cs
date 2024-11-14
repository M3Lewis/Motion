using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Drawing;
using FontStyle = System.Drawing.FontStyle;

namespace Motion.Utils
{
    public class AddScribble : GH_Component
    {
        protected override Bitmap Icon => Properties.Resources.AddScribble;
        public override Guid ComponentGuid => new Guid("2F58BB1F-78B1-45EB-9F93-1E5037527233");

        public override GH_Exposure Exposure => GH_Exposure.primary;
        public AddScribble()
          : base("Add Scribble", "Add Scribble",
              "Free to add a scribble.",
              "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Add Time?", "T?", "Add time scribble?", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Add Text?", "Te?", "Add text scribble?", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Text Content", "T", "Scribble text content", GH_ParamAccess.item, "Hello!");
            pManager.AddIntegerParameter("Text Size", "S", "Scribble text size", GH_ParamAccess.item, 100);
            pManager.AddTextParameter("Font", "F", "Font type", GH_ParamAccess.item,"Arial");
            pManager.AddIntegerParameter("Font Style", "FS", "Font Style.\n 0=Regular \n 1=Bold \n 2=Italic \n 3=Underline \n 4=Strikeout", GH_ParamAccess.item,0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iIsAddTime = false;
            bool iIsAddText = false;
            string iText = "";
            int iSize = 0;
            string iFont = "";
            int iFontStyleNum = 0;

            if (!DA.GetData(0, ref iIsAddTime)) return;
            if (!DA.GetData(1, ref iIsAddText)) return;
            if (!DA.GetData(2, ref iText)) return;
            if (!DA.GetData(3, ref iSize)) return;
            if (!DA.GetData(4, ref iFont)) return;
            if (!DA.GetData(5, ref iFontStyleNum)) return;
            
            GH_Document ghDoc = Instances.ActiveCanvas.Document;

            if (iIsAddTime == true || iIsAddText == true)
            {
                PointF comPivot = this.Attributes.Pivot;//本电池的中心点
                GH_Scribble scribble = CreateScribble(iIsAddTime, iIsAddText, iText, iSize, iFont, (FontStyle)iFontStyleNum);
                ghDoc.AddObject(scribble, false);//添加新物件
                scribble.Attributes.Pivot = new PointF(comPivot.X, (comPivot.Y - 200));//设置字体位置
                scribble.Attributes.ExpireLayout();
            }
        }
        private GH_Scribble CreateScribble(bool isAddTime, bool isAddText, string freeText, int size, string font, FontStyle fontStyle)
        {
            GH_Scribble scribble = new GH_Scribble();
            
            if (!isAddTime && !isAddText) return null;
            if (isAddText && !isAddTime)
            {
                FontStyle style = (FontStyle)fontStyle;
                scribble.Text = freeText;
                scribble.Font = new Font(font, size, style);
            }
            else if (isAddTime && !isAddText)
            {
                FontStyle style = (FontStyle)fontStyle;
                scribble.Text = DateTime.Now.ToString();
                scribble.Font = new Font(font, size, style);
            }
            return scribble;
        }


    }
}