using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.Animation;
using Motion.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class RangeSelectorButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 91; // 设置在 ScribbleControl 按钮后面

        public RangeSelectorButton()
        {
        }

        private void AddRangeSelectorButton()
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
            AddRangeSelectorButton();
        }

        private void Instantiate()
        {
            button.Name = "Create Range";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.RangeSelector;
            button.ToolTipText = "根据当前画布上所有Motion Slider的区间值创建一个区间Param";
            button.Click += CreateRangeParameter;
        }

        private void CreateRangeParameter(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas.Document;
                if (doc == null) return;

                // 收集所有非Union的MotionSlider的区间值
                var values = new HashSet<string>();
                foreach (var obj in doc.Objects)
                {
                    if (obj is MotionSender motionSender)                   
                    {
                        string senderNickname = motionSender.NickName;
                        values.Add(senderNickname);
                    }
                }

                if (!values.Any())
                {
                    MessageBox.Show("No matching Motion Sender found in the document.",
                        "No Sender Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 显示对话框
                var dialog = new RangeSelectorDialog(values);
                dialog.ShowDialog();

                if (dialog.IsConfirmed)
                {
                    // 创建Interval参数
                    var timeInterval = new TimeInterval();

                    // 获取当前视图中心
                    var canvas = Instances.ActiveCanvas;
                    var viewport = canvas.Viewport;

                    // 将视图坐标转换为文档坐标
                    var pt = viewport.UnprojectPoint(
                        new PointF(canvas.Width / 2, canvas.Height / 2));

                    // 设置参数位置
                    timeInterval.Attributes.Pivot = pt;

                    var matchingSender = doc.Objects
                    .OfType<MotionSender>()
                    .Where(s => s.NickName == dialog.SelectedTimeIntervalStr)
                    .FirstOrDefault();

                    // 添加到文档并记录到撤销记录
                    doc.AddObject(timeInterval, true);
                    timeInterval.Params.Input[0].AddSource(matchingSender);
                    timeInterval.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

                    doc.NewSolution(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
} 