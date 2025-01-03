using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class SliderControlWPFButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 5;
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
            button.Image = Properties.Resources.SliderControlWPF2;
            button.ToolTipText = "创建一个控制Union Slider的置顶窗口";
            button.Click += OpenControlWindow;
        }

        public void OpenControlWindow(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    MessageBox.Show("无法访问当前文档！");
                    return;
                }

                var connectedSlider = doc.Objects
                    .OfType<GH_NumberSlider>()
                    .FirstOrDefault(o => Grasshopper.Utility.LikeOperator(o.NickName, "TimeLine(Union)"));

                if (connectedSlider == null)
                {
                    MessageBox.Show("请先创建一个Union滑块！");
                    return;
                }

                SliderControlWPF.ShowWindow(connectedSlider);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误：{ex.Message}");
            }
        }
    }
}
