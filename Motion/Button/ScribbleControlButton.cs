using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Motion.Windows;

namespace Motion.Toolbar
{
    public class ScribbleControlButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 90;
        public ScribbleControlButton()
        {
        }

        private void AddScribbleControlButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(button);
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
            if (editor == null) return;
            AddScribbleControlButton();
        }

        private void Instantiate()
        {
            button.Name = "Add Scribble";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.AddScribble;
            button.ToolTipText = "添加Scribble（超越字体大小限制，多行输入）";
            button.Click += OpenScribbleDialog;
        }

        private void OpenScribbleDialog(object sender, EventArgs e)
        {
            try
            {
                var window = new ScribbleControlWPF(Instances.ActiveCanvas.Document);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
} 