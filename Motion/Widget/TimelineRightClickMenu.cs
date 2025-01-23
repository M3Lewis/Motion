﻿using System.Drawing;
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

            // 获取点击的组信息
            var (clickedGroup, _,_) = GetClickedGroupWithButtons(location);

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
    
                // 添加组管理选项
                menu.Items.Add(new ToolStripSeparator());
                var addGroupItem = menu.Items.Add("添加新组");
                addGroupItem.Click += (s, e) => AddGroup("新组 " + (_keyframeGroups.Count + 1));
    
                // 如果有选中的组，添加重命名和删除选项
                if (clickedGroup != null)
                {
                    var renameGroupItem = menu.Items.Add("重命名组");
                    renameGroupItem.Click += (s, e) => RenameGroup(clickedGroup);
    
                    var removeGroupItem = menu.Items.Add("删除组");
                    removeGroupItem.Click += (s, e) => RemoveGroup(clickedGroup);
                }
    
                // 添加Motion Value参数选项
                if (_keyframeGroups.ContainsKey(_activeGroup) && _keyframeGroups[_activeGroup].Count > 0)
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

        private void RenameGroup(string oldName)
        {
            using (var dialog = new InputDialog("重命名组", "输入新组名:", oldName))
            {
                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.InputText))
                {
                    if (_keyframeGroups.ContainsKey(oldName))
                    {
                        var frames = _keyframeGroups[oldName];
                        _keyframeGroups.Remove(oldName);
                        _keyframeGroups[dialog.InputText] = frames;
                        _groupVisibility[dialog.InputText] = _groupVisibility[oldName];
                        _groupCollapsed[dialog.InputText] = _groupCollapsed[oldName];
                        _groupVisibility.Remove(oldName);
                        _groupCollapsed.Remove(oldName);
                        Invalidate();
                    }
                }
            }
        }
    }
}
