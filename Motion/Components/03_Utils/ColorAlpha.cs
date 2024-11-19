using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Motion.Utils
{
    public class ColorAlpha : GH_Component
    {
        public ColorAlpha()
            : base("Color Alpha", "Alpha", 
                "Modify the alpha (transparency) value of a color",
                "Motion", "03_Utils")
        {
        }

        public override Guid ComponentGuid => new Guid("47a71f9a-30c5-42ad-bd2e-1de680ca6b2f");
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "Input color", GH_ParamAccess.item);
            pManager.AddNumberParameter("Alpha", "A", "Alpha value between 0-255", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "Color with modified alpha", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Color inputColor = Color.Black;
            double alpha = 255;

            if (!DA.GetData(0, ref inputColor)) return;
            if (!DA.GetData(1, ref alpha)) return;

            // 确保alpha值在0-255范围内
            alpha = Math.Max(0, Math.Min(255, alpha));
            
            // 创建新的颜色，保持RGB值不变，只修改alpha值
            Color resultColor = Color.FromArgb(
                (int)alpha,
                inputColor.R,
                inputColor.G,
                inputColor.B
            );

            DA.SetData(0, resultColor);
        }
    }
} 