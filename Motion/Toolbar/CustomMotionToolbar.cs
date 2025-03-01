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

        public CustomMotionToolbar()
        {
            // 初始化工具栏属性
            customMotionToolbar.ImageScalingSize = new Size(24, 24);
            customMotionToolbar.Padding = new Padding(1);
            customMotionToolbar.GripStyle = ToolStripGripStyle.Hidden;
            customMotionToolbar.BackColor = Color.FromArgb(128, 240, 240, 240);
            customMotionToolbar.ShowItemToolTips = true;

            // 设置初始位置和大小
            customMotionToolbar.AutoSize = false;
            customMotionToolbar.Dock = DockStyle.None;
            customMotionToolbar.Width = 40;
            customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
            customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
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
                    break;
                case ToolbarPosition.Left:
                    customMotionToolbar.Dock = DockStyle.Left;
                    customMotionToolbar.Width = toolbarSize;
                    customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
                    break;
                case ToolbarPosition.Right:
                    customMotionToolbar.Dock = DockStyle.Right;
                    customMotionToolbar.Width = toolbarSize;
                    customMotionToolbar.Height = Instances.ActiveCanvas?.Height ?? 600;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
                    break;
                case ToolbarPosition.Bottom:
                    customMotionToolbar.Dock = DockStyle.Bottom;
                    customMotionToolbar.Height = toolbarSize;
                    customMotionToolbar.Width = Instances.ActiveCanvas?.Width ?? 800;
                    customMotionToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
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
    }
}
