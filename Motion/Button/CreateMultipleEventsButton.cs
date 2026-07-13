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
using Motion.General;

namespace Motion.Toolbar
{
    public class CreateMultipleEventsButton : MotionToolbarButton
    {
        private ToolStripButton button;
        public override int ToolbarOrder => 40;
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
            AddButtonToToolbars(button);
        }

        private void Instantiate()
        {
            button.Name = "Create Multiple Events";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.CreateMultipleEvents; // 需要添加对应的图标资源
            button.ToolTipText = General.LanguageManager.GetString("Button.CreateMultipleEvents.Tooltip", "为选定的多个Motion Sender创建Event");
            button.Click += CreateEventsForSelectedSenders;
        }

        public override void UpdateLanguage()
        {
            if (button != null)
            {
                button.ToolTipText = General.LanguageManager.GetString("Button.CreateMultipleEvents.Tooltip", "为选定的多个Motion Sender创建Event");
            }
        }

        private void CreateEventsForSelectedSenders(object sender, EventArgs e)
        {
            try
            {
                var canvas = Instances.ActiveCanvas;
                if (canvas?.Document == null) return;
                var ghDoc = canvas.Document;

                var selectedSenders = ghDoc.SelectedObjects()
                    .Where(obj => obj is MotionSender)
                    .Cast<MotionSender>()
                    .ToList();

                if (!selectedSenders.Any())
                {
                    ShowTemporaryMessage(canvas, "请至少选择一个Motion Sender");
                    return;
                }

                const float VERTICAL_SPACING = 120f;
                const float HORIZONTAL_OFFSET = 300f;

                var componentsToExpire = new List<(EventComponent eventComp, IGH_ActiveObject graphComp)>();

                for (int i = 0; i < selectedSenders.Count; i++)
                {
                    var senderParam = selectedSenders[i];
                    var senderPivot = senderParam.Attributes.Pivot;
                    var eventPivot = new PointF(
                        senderPivot.X + HORIZONTAL_OFFSET,
                        senderPivot.Y + i * VERTICAL_SPACING + 10f
                    );

                    var (eventComp, graphComp) = EventGraphFactory.CreateEventWithGraph(ghDoc, senderParam, eventPivot);

                    senderParam.Attributes.Selected = false;
                    eventComp.Attributes.Selected = false;
                    if (graphComp != null) graphComp.Attributes.Selected = false;

                    componentsToExpire.Add((eventComp, graphComp));
                }

                ghDoc.ScheduleSolution(10, doc =>
                {
                    foreach (var (eventComp, graphComp) in componentsToExpire)
                    {
                        eventComp.ExpireSolution(false);
                        graphComp?.ExpireSolution(false);
                    }
                });

                canvas.Refresh();
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }
    }
}