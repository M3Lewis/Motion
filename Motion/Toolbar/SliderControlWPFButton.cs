using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class SliderControlWPFButton : MotionToolbarButton
    {
        private ToolStripButton button;

        public SliderControlWPFButton()
        {
            
        }
        private void AddSliderControlWPFButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button); // 使用基类方法添加按钮
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;

            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;

            //Check if DocumentEditor was instantiated
            if (editor == null) return;
            AddSliderControlWPFButton();
        }

        private void Instantiate()
        {
            // 配置按钮
            button.Name = "Slider Control";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.SliderControlWPF;
            button.ToolTipText = "Add a topmost slider control WPF Window";
            button.Click += OpenControlWindow;
        }

        public void OpenControlWindow(object sender, EventArgs e)
        {
            try
            {
                // 使用 FirstOrDefault 替代 ToList()[0]
                var connectedSlider = Instances.ActiveCanvas.Document.Objects
                    .FirstOrDefault((IGH_DocumentObject o) => 
                        Grasshopper.Utility.LikeOperator(o.NickName, "TimeLine(Union)"));

                if (connectedSlider == null)
                {
                    MessageBox.Show("Please create a union slider first!");
                    return;
                }

                SliderControlWPF window = new SliderControlWPF(connectedSlider as GH_NumberSlider);
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
