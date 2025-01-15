using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class TimelineToolbarButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 200;  // 设置工具栏顺序
        private static ToolStripButton timelineButton;
        private static TimeScrubRegion scrubRegion;
        private static bool isTimelineVisible = false;

        public TimelineToolbarButton()
        {
        }

        private void AddTimelineButton()
        {
            InitializeToolbarGroup();
            timelineButton = new ToolStripButton("Timeline");
            Instantiate();
            AddButtonToGroup(timelineButton);
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
            AddTimelineButton();
        }

        private void Instantiate()
        {
            // 配置按钮
            timelineButton.Name = "Timeline";
            timelineButton.Size = new Size(24, 24);
            timelineButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            timelineButton.Image = null;
            timelineButton.ToolTipText = "时间轴";
            timelineButton.Click += TimelineButton_Click;

            // 初始化时间轴区域
            scrubRegion = new TimeScrubRegion();

            // 订阅 Canvas 事件
            Instances.ActiveCanvas.Paint += Canvas_Paint;
            Instances.ActiveCanvas.MouseDown += Canvas_MouseDown;
            Instances.ActiveCanvas.MouseMove += Canvas_MouseMove;
            Instances.ActiveCanvas.MouseUp += Canvas_MouseUp;
            Instances.ActiveCanvas.MouseWheel += Canvas_MouseWheel;
        }

        private void TimelineButton_Click(object sender, EventArgs e)
        {
            isTimelineVisible = !isTimelineVisible;
            if (isTimelineVisible)
            {
                timelineButton.BackColor = Color.Orange;
            }
            else
            {
                timelineButton.BackColor = Color.FromArgb(255, 255, 255);
            }
            Instances.ActiveCanvas.Refresh();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (!isTimelineVisible || scrubRegion == null) return;

            GH_Canvas canvas = Instances.ActiveCanvas;
            if (canvas != null)
            {
                scrubRegion.UpdateRegion(canvas);
                scrubRegion.Render(e.Graphics);
            }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isTimelineVisible || scrubRegion == null) return;

            if (scrubRegion.HandleMouseDown(e.Location))
            {
                Instances.ActiveCanvas.Refresh();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isTimelineVisible || scrubRegion == null) return;

            if (scrubRegion.HandleMouseMove(e.Location))
            {
                Instances.ActiveCanvas.Refresh();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isTimelineVisible || scrubRegion == null) return;

            if (scrubRegion.HandleMouseUp(e.Location))
            {
                Instances.ActiveCanvas.Refresh();
            }
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!isTimelineVisible || scrubRegion == null) return;

            scrubRegion.HandleMouseWheel(e.Location, e.Delta);
            Instances.ActiveCanvas.Refresh();
        }
    }
} 