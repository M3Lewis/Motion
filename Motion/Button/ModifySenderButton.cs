using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Motion.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using Motion.Animation;
using System.Collections.Generic;

namespace Motion.Toolbar
{
    public class ModifySenderButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 1;
        private ToolStripButton button;

        public ModifySenderButton()
        {
        }

        private void AddModifySliderButton()
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
            AddModifySliderButton();
        }

        private void Instantiate()
        {
            button.Name = "Modify Slider";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.ModifySliderButton; // 需要添加对应的图标
            button.ToolTipText = "修改Motion Sender";
            button.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            OnButtonClicked(sender, e);
        }

        // Add this method to your ModifySliderButton class
        protected override void OnButtonClicked(object sender, EventArgs e)
        {
            var window = new ModifySliderWindow();

            // 获取选中的Motion Slider组件
            List<MotionSender> selectedSenders = Instances.ActiveCanvas.Document.SelectedObjects()
                .OfType<MotionSender>()
                .ToList();

            window.Initialize(selectedSenders);

            // 设置窗口位置
            // For cloned buttons we need to get position differently
            Point screenPoint;
            if (sender is ToolStripItem item && item.Owner != null)
            {
                screenPoint = item.Owner.PointToScreen(item.Bounds.Location);
            }
            else
            {
                screenPoint = button.Owner.PointToScreen(button.Bounds.Location);
            }

            window.Left = screenPoint.X;
            window.Top = screenPoint.Y + (sender is ToolStripItem ? ((ToolStripItem)sender).Height : button.Height);

            window.ShowDialog();
        }
    }
} 