using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System;

namespace Motion.Components
{
    public class SliderControlWPFComponent : GH_Component
    {
        private GH_NumberSlider connectedSlider;

        public override Guid ComponentGuid => new Guid("9a8572e5-5d62-436e-bcbf-0f31daf09978");

        public SliderControlWPFComponent()
            : base("Slider Control WPF", "SldCtrlWPF",
                "Control a slider through a WPF window",
                "Motion", "01_Animation")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Slider", "S", "Connect a slider here", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // No outputs needed
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (Params.Input[0].Sources.Count > 0)
            {
                connectedSlider = Params.Input[0].Sources[0] as GH_NumberSlider;
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Open Control Window", OpenControlWindow);
        }

        public override void CreateAttributes()
        {
            m_attributes = new CustomWPFComponentAttributes(this);
        }

        public void OpenControlWindow(object sender, EventArgs e)
        {
            if (connectedSlider == null)
            {
                MessageBox.Show("Please connect a slider first!");
                return;
            }

            var window = new SliderControlWPF(connectedSlider);
            window.Show();
        }
    }

    public class CustomWPFComponentAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
    {
        public CustomWPFComponentAttributes(IGH_Component component)
            : base(component)
        {
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            SliderControlWPFComponent comp = Owner as SliderControlWPFComponent;
            if (comp != null)
            {
                comp.OpenControlWindow(null, null);
            }
            return GH_ObjectResponse.Handled;
        }
    }
}