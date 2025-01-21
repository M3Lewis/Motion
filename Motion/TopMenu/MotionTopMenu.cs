using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Motion.Settings;
using Motion.Widget;
using System;
using System.Drawing;
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
            try
            {
                GH_DocumentEditor editor = Instances.DocumentEditor;
                Rhino.RhinoApp.WriteLine($"ToggleTimelineWidget called with activate={activate}");

                if (activate)
                {
                    var existingWidget = editor.Controls.OfType<TimelineWidget>().FirstOrDefault();
                    if (existingWidget == null)
                    {
                        Rhino.RhinoApp.WriteLine("Creating new TimelineWidget");
                        TimelineWidget widget = new TimelineWidget();
                        
                        // 确保控件位置和尺寸正确
                        widget.Size = new Size(800, 100);
                        widget.Location = new Point(10, 10);
                        widget.Visible = true;
                        widget.Enabled = true;
                        
                        editor.Controls.Add(widget);
                        widget.BringToFront();
                        
                        // 强制布局更新
                        editor.PerformLayout();
                        widget.Invalidate(true);
                        widget.Update();
                        
                        Rhino.RhinoApp.WriteLine("TimelineWidget created and added to controls");
                    }
                    else
                    {
                        Rhino.RhinoApp.WriteLine("Using existing TimelineWidget");
                        existingWidget.Visible = true;
                        existingWidget.BringToFront();
                        existingWidget.Invalidate(true);
                        existingWidget.Update();
                    }
                }
                else
                {
                    foreach (var widget in editor.Controls.OfType<TimelineWidget>())
                    {
                        widget.Visible = false;
                    }
                }
                
                // 刷新整个界面
                editor.PerformLayout();
                editor.Refresh();
                Instances.ActiveCanvas?.Refresh();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"ToggleTimelineWidget error: {ex.Message}");
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