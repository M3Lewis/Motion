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
        public override int ToolbarOrder => 11;

        public ScribbleControlButton()
        {
        }

        private void AddScribbleButton()
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
            AddScribbleButton();
        }

        private void Instantiate()
        {
            button.Name = "Add Scribble";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.AddScribble;
            button.ToolTipText = General.LanguageManager.GetString("Button.ScribbleControl.Tooltip", "Scribble缩放大小整理");
            button.Click += OpenScribbleDialog;
        }

        public override void UpdateLanguage()
        {
            if (button != null)
            {
                button.ToolTipText = General.LanguageManager.GetString("Button.ScribbleControl.Tooltip", "Scribble缩放大小整理");
            }
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
                MessageBox.Show(string.Format(General.LanguageManager.GetString("Msg.ErrorOccurred", "An error occurred: {0}"), ex.Message));
            }
        }
    }
}