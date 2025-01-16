using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Motion.Settings;
using Motion.Widget;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Motion.TopMenu
{
    public class MotionTopMenu : GH_AssemblyPriority
    {
        private ToolStripMenuItem motionTimelineWidgetToggleMenuItem;
        private static bool Activated
        {
            get
            {
                if (MotionSettings.Instance.TimelineWidgetToggle)
                {
                    return true;
                }
                return false;
            }
        }
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += CanvasCreatedEventHandler;
            return GH_LoadingInstruction.Proceed;
        }

        public static void ToggleTimelineWidget(bool activate)
        {
            foreach (Control item in Instances.DocumentEditor?.Controls)
            {
                if (!(item is Panel panel))
                {
                    continue;
                }
                foreach (Control control in panel.Controls)
                {
                    if (control is GH_Canvas canvas)
                    {
                        if (activate)
                        {
                            var existingWidget = canvas.Widgets.OfType<TimelineWidget>().FirstOrDefault();
                            if (existingWidget == null)
                            {
                                TimelineWidget widget = new TimelineWidget();
                                canvas.Widgets.Add(widget);
                                widget.Owner = canvas;
                            }
                            foreach (var widget in canvas.Widgets.OfType<TimelineWidget>())
                            {
                                widget.Visible = true;
                            }
                        }
                        else
                        {
                            foreach (var widget in canvas.Widgets.OfType<TimelineWidget>())
                            {
                                widget.Visible = false;
                            }
                        }
                        canvas.Invalidate();
                    }
                }
            }
        }

        private void CanvasCreatedEventHandler(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= CanvasCreatedEventHandler;
            GH_DocumentEditor documentEditor = Instances.DocumentEditor;
            if (documentEditor != null)
            {
                SetupMenu(documentEditor);
            }
            ToggleTimelineWidget(Activated);
        }

        private void SetupMenu(GH_DocumentEditor docEdit)
        {
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem();
            docEdit.MainMenuStrip.SuspendLayout();
            docEdit.MainMenuStrip.Items.AddRange(new ToolStripItem[1] { toolStripMenuItem });
            toolStripMenuItem.Name = "MotionTimelineWidgetToolStripMenuItem";
            toolStripMenuItem.Text = "Motion";
            motionTimelineWidgetToggleMenuItem = new ToolStripMenuItem
            {
                Name = "MotionTimelineWidgetToolStripMenuItem",
                Text = "Open Timeline Widget",
                CheckOnClick = true,
                Checked = MotionSettings.Instance.TimelineWidgetToggle
            };
            motionTimelineWidgetToggleMenuItem.Click += MotionTimelineWidgetToggleMenuItem_Click;
            toolStripMenuItem.DropDownItems.Add(motionTimelineWidgetToggleMenuItem);
           
            toolStripMenuItem.DropDownOpened += SgMenu_DropDownOpened;
            docEdit.MainMenuStrip.ResumeLayout(performLayout: false);
            docEdit.MainMenuStrip.PerformLayout();
            GH_DocumentEditor.AggregateShortcutMenuItems += GH_DocumentEditor_AggregateShortcutMenuItems;
        }

        private void SgMenu_DropDownOpened(object sender, EventArgs e)
        {
            motionTimelineWidgetToggleMenuItem.Checked = MotionSettings.Instance.TimelineWidgetToggle;
        }

        private void MotionTimelineWidgetToggleMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem toolStripMenuItem)
            {
                MotionSettings.Instance.TimelineWidgetToggle = toolStripMenuItem.Checked;
                ToggleTimelineWidget(toolStripMenuItem.Checked);
            }
        }

        private void GH_DocumentEditor_AggregateShortcutMenuItems(object sender, GH_MenuShortcutEventArgs e)
        {
            e.AppendItem(motionTimelineWidgetToggleMenuItem);
        }
    }

}