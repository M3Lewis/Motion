using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Motion.Properties;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class MotionSliderSettings : MotionToolbarButton
    {
        protected override int ToolbarOrder => 101;
        private ToolStripButton button;
        private bool isActive = false;
        public static int FramesPerSecond { get; private set; } = 60; // 默认每秒60帧

        public MotionSliderSettings()
        {
        }

        private void AddMotionSliderSettingsButton()
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
            AddMotionSliderSettingsButton();
        }

        private void Instantiate()
        {
            button.Name = "Motion Slider Settings";
            button.Size = new System.Drawing.Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Resources.MotionSliderSettingsButton; // 需要添加对应的图标
            button.ToolTipText = "鼠标左键：显示Slider帧数对应的时间\n鼠标右键：设置每秒帧数";
            button.Click += LeftClickButton;
            button.MouseDown += RightClickButton;
        }

        private void LeftClickButton(object sender, EventArgs e)
        {
            isActive = !isActive;
            button.BackColor = isActive ? Color.Orange : Color.FromArgb(255, 255, 255);
        }

        private void RightClickButton(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var settingsWindow = new MotionSliderSettingsWindow();
                settingsWindow.CurrentFPS = FramesPerSecond;
                settingsWindow.FPSChanged += (fps) =>
                {
                    FramesPerSecond = fps;
                };

                // 将窗口位置设置在按钮附近
                var screenPoint = button.Owner.PointToScreen(button.Bounds.Location);
                settingsWindow.Left = screenPoint.X;
                settingsWindow.Top = screenPoint.Y + button.Height;

                settingsWindow.ShowDialog();
            }
        }

        public static double ConvertSecondsToFrames(double seconds)
        {
            return seconds * FramesPerSecond;
        }

        public static bool IsSecondsInputMode()
        {
            ToolStrip customToolbar = CustomMotionToolbar.customMotionToolbar;
            ToolStrip targetToolbar;

            if (customToolbar.Items.Count == 0)
            {
                // 如果位置是 OnToolbar，检查 Grasshopper 的工具栏
                Type typeFromHandle = typeof(GH_DocumentEditor);
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);
                object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));
                targetToolbar = objectValue as ToolStrip;
            }
            else
            {
                // 否则，检查 CustomMotionToolbar
                targetToolbar = customToolbar;
            }

            if (targetToolbar != null)
            {
                foreach (ToolStripItem item in targetToolbar.Items)
                {
                    if (item.Name == "Motion Slider Settings" && item is ToolStripButton button)
                    {
                        var settings = button.Tag as MotionSliderSettings;
                        return settings?.isActive ?? false;
                    }
                }
            }

            return false;
        }
    }
}