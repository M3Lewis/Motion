using System.Drawing;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private void ShowContextMenu(Point location)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            // 检查是否点击到关键帧
            _selectedKeyframe = GetKeyframeAtPoint(location);

            // 如果有选中的关键帧，添加删除选项
            if (_selectedKeyframe != null)
            {
                menu.Items.Add(new ToolStripSeparator());
                var deleteItem = menu.Items.Add("Delete Keyframe");
                deleteItem.Click += Menu_DeleteKeyframe;
            }
            else
            {
                // 基础菜单项
                menu.Items.Add(new ToolStripSeparator());

                var topItem = menu.Items.Add("Top");
                topItem.Click += Menu_DockTop;

                var bottomItem = menu.Items.Add("Bottom");
                bottomItem.Click += Menu_DockBottom;

                // 添加Motion Value参数选项
                if (_keyframes.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                    var addMotionValueItem = menu.Items.Add("Add Motion Value Parameter");
                    addMotionValueItem.Click += Menu_AddMotionValue;

                    // 添加清除所有关键帧的选项
                    menu.Items.Add(new ToolStripSeparator());
                    var clearKeyframesItem = menu.Items.Add("Clear All Keyframes");
                    clearKeyframesItem.Click += Menu_ClearKeyframes;
                }
            }

            if (menu.Items.Count > 0)
            {
                menu.Show(Control.MousePosition);
            }
        }
    }
}
