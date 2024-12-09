using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Motion.Animation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class CreateMultipleEventsButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 40;
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

            AddCreateEventsButton();
        }

        private void AddCreateEventsButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button);
        }

        private void Instantiate()
        {
            button.Name = "Create Multiple Events";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.CreateMultipleEvents; // 需要添加对应的图标资源
            button.ToolTipText = "为选定的多个Motion Sender创建Event";
            button.Click += CreateEventsForSelectedSenders;
        }

        private void CreateEventsForSelectedSenders(object sender, EventArgs e)
        {
            try
            {
                var canvas = Instances.ActiveCanvas;
                if (canvas?.Document == null) return;

                var selectedSenders = canvas.Document.SelectedObjects()
                    .Where(obj => obj is MotionSender)
                    .Cast<MotionSender>()
                    .ToList();

                if (!selectedSenders.Any())
                {
                    ShowTemporaryMessage(canvas, "请至少选择一个Motion Sender");
                    return;
                }

                const float EVENT_WIDTH = 150f;
                const float VERTICAL_SPACING = 120f;
                const float HORIZONTAL_OFFSET = 300f;

                Dictionary<Guid, PointF> targetPositions = new Dictionary<Guid, PointF>();

                for (int i = 0; i < selectedSenders.Count; i++)
                {
                    var senderParam = selectedSenders[i];
                    var attributes = senderParam.Attributes as RemoteParamAttributes;
                    if (attributes == null) continue;

                    var senderPivot = attributes.Pivot;
                    var eventPivot = new PointF(
                        senderPivot.X + HORIZONTAL_OFFSET,
                        senderPivot.Y + i * VERTICAL_SPACING
                    );

                    targetPositions[senderParam.InstanceGuid] = eventPivot;

                    var controlPoint = new Point(
                        (int)attributes.Bounds.Left + (int)attributes.Bounds.Width / 2,
                        (int)attributes.Bounds.Top + (int)attributes.Bounds.Height / 2
                    );

                    var mouseEvent = new GH_CanvasMouseEvent(
                        controlPoint,
                        canvas.Viewport.UnprojectPoint(controlPoint),
                        MouseButtons.Left,
                        2
                    );

                    attributes.RespondToMouseDoubleClick(canvas, mouseEvent);

                    canvas.Document.ScheduleSolution(5, doc =>
                    {
                        var newComponents = doc.Objects
                            .Where(obj => obj.Attributes.Selected)
                            .OrderByDescending(obj => obj.InstanceGuid)
                            .ToList();

                        foreach (var comp in newComponents)
                        {
                            if (targetPositions.TryGetValue(senderParam.InstanceGuid, out PointF targetPos))
                            {
                                if (comp is EventComponent eventComp)
                                {
                                    eventComp.Attributes.Pivot = new PointF(targetPos.X,targetPos.Y+10f);
                                    eventComp.Attributes.Selected = false;
                                }
                                else if (comp is GH_GraphMapper mapper)
                                {
                                    mapper.Attributes.Pivot = new PointF(
                                        targetPos.X + EVENT_WIDTH + 50f,
                                        targetPos.Y - 65f
                                    );
                                    mapper.Attributes.Selected = false;
                                }
                            }
                        }
                        
                        Grasshopper.Instances.ActiveCanvas.Invalidate();
                    });
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }
    }
}