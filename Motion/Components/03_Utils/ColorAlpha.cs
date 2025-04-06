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
                "修改颜色的 alpha（透明度）值，当alpha值为0时，将锁定下游组件。",
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
            if (!TryGetInputData(DA, out Color inputColor, out double alphaInput))
                return;

            int finalAlpha = CalculateFinalAlpha(alphaInput, _isAlphaMode0To1);
            UpdateActiveObjectLockState(alphaInput, _isAlphaMode0To1);

            Color resultColor = CreateResultColor(inputColor, finalAlpha);
            DA.SetData(0, resultColor);
        }

        private bool TryGetInputData(IGH_DataAccess DA, out Color inputColor, out double alphaInput)
        {
            inputColor = Color.Black;
            alphaInput = 0;
            return DA.GetData(0, ref inputColor) && DA.GetData(1, ref alphaInput);
        }

        private int CalculateFinalAlpha(double alphaInput, bool isAlphaMode0To1)
        {
            double alphaProcessed = isAlphaMode0To1
                ? ProcessAlphaIn0To1Mode(alphaInput)
                : ProcessAlphaIn0To255Mode(alphaInput);

            return (int)Math.Round(Math.Max(0.0, Math.Min(255.0, alphaProcessed)));
        }

        private double ProcessAlphaIn0To1Mode(double alphaInput)
        {
            if (alphaInput <= 0)
                return 0.0;

            return Math.Max(0.0, Math.Min(1.0, alphaInput)) * 255.0;
        }

        private double ProcessAlphaIn0To255Mode(double alphaInput)
        {
            if (alphaInput <= 0)
                return 0.0;

            return Math.Max(0.0, Math.Min(255.0, alphaInput));
        }

        private void UpdateActiveObjectLockState(double originalAlpha, bool isAlphaMode0To1)
        {
            bool shouldLock = originalAlpha <= 0;


            bool anyLockStateChanged = false;

            for (int i = 0; i < Params.Output[0].Recipients.Count; i++)
            {
                GH_ActiveObject activeObj = Params.Output[0].Recipients[i].Attributes.GetTopLevel.DocObject as GH_ActiveObject;
                if (activeObj != null)
                {
                    if (activeObj.Locked != shouldLock) // Only track changes if lock state changed
                    {
                        activeObj.Locked = shouldLock;
                        anyLockStateChanged = true;
                    }
                }
            }

            // Only expire solution once after all lock states have been updated
            if (anyLockStateChanged)
            {
                // This will trigger a solution for the entire document, but only once
                Grasshopper.Instances.ActiveCanvas?.Document?.ExpireSolution();
            }
        }

        private Color CreateResultColor(Color inputColor, int alpha)
        {
            return Color.FromArgb(alpha, inputColor.R, inputColor.G, inputColor.B);
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