using Grasshopper;
using Grasshopper.GUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class CustomMotionToolbar : GH_Toolstrip
    {
        public static ToolStrip customMotionToolbar = new GH_Toolstrip();

        private static ToolbarPosition _currentPosition = ToolbarPosition.OnToolbar;
        public static ToolbarPosition CurrentPosition => _currentPosition;

        // Add transparency property
        private static int _backgroundAlpha = 128;
        public static int BackgroundAlpha
        {
            get => _backgroundAlpha;
            set
            {
                _backgroundAlpha = Math.Max(0, Math.Min(255, value)); // Ensure value is between 0-255
                UpdateBackgroundColor();
            }
        }

        public CustomMotionToolbar()
        {
            // 初始化工具栏属性
            customMotionToolbar.ImageScalingSize = new Size(24, 24);
            customMotionToolbar.Padding = new Padding(1);
            customMotionToolbar.GripStyle = ToolStripGripStyle.Hidden;
            UpdateBackgroundColor(); // Use our method instead of direct assignment
            customMotionToolbar.ShowItemToolTips = true;

            // 设置初始位置和大小
            customMotionToolbar.AutoSize = false;
            customMotionToolbar.Dock = DockStyle.None;
            customMotionToolbar.Width = 40;
            customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
            customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
        }

        // Add method to update background color with current alpha
        private static void UpdateBackgroundColor()
        {
            customMotionToolbar.BackColor = Color.FromArgb(_backgroundAlpha, 240, 240, 240);
        }

        public static void SetPosition(ToolbarPosition position)
        {
            _currentPosition = position;

            // Get the icon size with appropriate padding
            int iconSize = customMotionToolbar.ImageScalingSize.Height;
            int toolbarSize = iconSize + 16; // Add padding for the toolbar

            switch (position)
            {
                case ToolbarPosition.Top:
                    customMotionToolbar.Dock = DockStyle.Top;
                    customMotionToolbar.Height = toolbarSize;
                    customMotionToolbar.Width = Instances.ActiveCanvas?.Width ?? 800;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                    // Preserve icon sizing
                    customMotionToolbar.ImageScalingSize = new Size(24, 24);
                    break;
                case ToolbarPosition.Left:
                    customMotionToolbar.Dock = DockStyle.Left;
                    customMotionToolbar.Width = toolbarSize;
                    customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
                    customMotionToolbar.ImageScalingSize = new Size(24, 24);
                    break;
                case ToolbarPosition.Right:
                    customMotionToolbar.Dock = DockStyle.Right;
                    customMotionToolbar.Width = toolbarSize;
                    customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
                    customMotionToolbar.ImageScalingSize = new Size(24, 24);
                    break;
                case ToolbarPosition.Bottom:
                    customMotionToolbar.Dock = DockStyle.Bottom;
                    customMotionToolbar.Height = toolbarSize;
                    customMotionToolbar.Width = Instances.ActiveCanvas?.Width ?? 800;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                    // Preserve icon sizing
                    customMotionToolbar.ImageScalingSize = new Size(24, 24);
                    break;
                case ToolbarPosition.OnToolbar:
                    customMotionToolbar.Visible = false;
                    break;
            }

            // Ensure the toolbar is visible (unless it's OnToolbar position)
            if (position != ToolbarPosition.OnToolbar)
            {
                customMotionToolbar.Visible = true;
                customMotionToolbar.BringToFront();
            }
        }

        // Add a resizing handler for when the canvas size changes
        public static void UpdateToolbarSize()
        {
            if (!customMotionToolbar.Visible) return;

            switch (_currentPosition)
            {
                case ToolbarPosition.Top:
                case ToolbarPosition.Bottom:
                    customMotionToolbar.Width = Instances.ActiveCanvas?.Width ?? 800;
                    break;
                case ToolbarPosition.Left:
                case ToolbarPosition.Right:
                    customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
                    break;
            }
        }
    }
}