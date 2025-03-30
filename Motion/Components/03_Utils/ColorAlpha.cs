using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Drawing;
using System.Windows.Forms; // Required for ToolStripMenuItem

namespace Motion.Utils
{
    public class ColorAlpha : GH_Component
    {
        private bool _isAlphaMode0To1 = false; // false for 0-255, true for 0-1

        public ColorAlpha()
            : base("Color Alpha", "Alpha",
                "修改颜色的 alpha（透明度）值",
                "Motion", "03_Utils")
        {
            UpdateMessage(); // Initialize message on creation
        }

        public override Guid ComponentGuid => new Guid("47a71f9a-30c5-42ad-bd2e-1de680ca6b2f");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.ColorAlpha;
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "颜色", GH_ParamAccess.item);
            // Description updated dynamically based on mode
            pManager.AddNumberParameter("Alpha", "A", GetAlphaInputDescription(), GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "Color with modified alpha", GH_ParamAccess.item);
        }

        private string GetAlphaInputDescription()
        {
            return _isAlphaMode0To1 ? "透明度值 (0-1)" : "透明度值 (0-255)";
        }

        private void UpdateAlphaInputParamDescription()
        {
            if (Params.Input.Count > 1 && Params.Input[1] is Param_Number alphaParam)
            {
                alphaParam.NickName = "A"; // Keep NickName consistent
                alphaParam.Name = "Alpha";
                alphaParam.Description = GetAlphaInputDescription();
            }
        }

        private void UpdateMessage()
        {
            Message = _isAlphaMode0To1 ? "0-1" : "0-255";
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "切换 Alpha 模式", Menu_ToggleAlphaMode_Clicked);
        }

        private void Menu_ToggleAlphaMode_Clicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle Alpha Mode"); // Record undo event
            _isAlphaMode0To1 = !_isAlphaMode0To1;
            UpdateMessage();
            UpdateAlphaInputParamDescription();
            ExpireSolution(true); // Recompute the component
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Color inputColor = Color.Black;
            double alphaInput = 0; // Use a different variable for input

            if (!DA.GetData(0, ref inputColor)) return;
            if (!DA.GetData(1, ref alphaInput)) return;

            double alphaProcessed; // The final alpha value in 0-255 range

            if (_isAlphaMode0To1)
            {
                // Mode 0-1
                if (alphaInput <= 0)
                {
                    alphaProcessed = 1.0; // Map 0 or less to 1 (which is 1/255 * 255)
                }
                else
                {
                    // Clamp input between 1/255 and 1, then scale to 1-255
                    alphaProcessed = Math.Max(1.0 / 255.0, Math.Min(1.0, alphaInput)) * 255.0;
                }
                 // Ensure minimum is 1 after scaling
                alphaProcessed = Math.Max(1.0, alphaProcessed);
            }
            else
            {
                // Mode 0-255
                if (alphaInput <= 0)
                {
                    alphaProcessed = 1.0; // Map 0 or less to 1
                }
                else
                {
                    // Clamp input between 1 and 255
                    alphaProcessed = Math.Max(1.0, Math.Min(255.0, alphaInput));
                }
            }

            // Ensure the final value is an integer between 1 and 255
            int finalAlpha = (int)Math.Round(Math.Max(1.0, Math.Min(255.0, alphaProcessed)));


            // 创建新的颜色，保持RGB值不变，只修改alpha值
            Color resultColor = Color.FromArgb(
                finalAlpha,
                inputColor.R,
                inputColor.G,
                inputColor.B
            );

            DA.SetData(0, resultColor);
        }

        // Persist state
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("IsAlphaMode0To1", _isAlphaMode0To1);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            _isAlphaMode0To1 = reader.GetBoolean("IsAlphaMode0To1");
            UpdateMessage(); // Update message after reading
            UpdateAlphaInputParamDescription(); // Update input description after reading
            return base.Read(reader);
        }
    }
}