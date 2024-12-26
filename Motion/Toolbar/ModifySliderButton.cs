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
    public class ModifySliderButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 1;
        private ToolStripButton button;

        public ModifySliderButton()
        {
        }

        private void AddModifySliderButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button);
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
            button.ToolTipText = "修改Motion Slider";
            button.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var window = new ModifySliderWindow();
            
            // 获取选中的Motion Slider组件
            List<MotionSlider> selectedSliders = Instances.ActiveCanvas.Document.SelectedObjects()
                .OfType<MotionSlider>()
                .ToList();

            window.Initialize(selectedSliders);

            // 设置窗口位置
            var screenPoint = button.Owner.PointToScreen(button.Bounds.Location);
            window.Left = screenPoint.X;
            window.Top = screenPoint.Y + button.Height;

            window.ShowDialog();
        }
    }
} 