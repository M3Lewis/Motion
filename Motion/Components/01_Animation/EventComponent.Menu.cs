using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Motion.General;

namespace Motion.Animation
{
    public partial class EventComponent
    {
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            // 添加显示/隐藏按钮的菜单项
            var showHideItem = Menu_AppendItem(menu,
                IsCollapsed ? "显示HIDE/LOCK按钮" : "显示HIDE/LOCK按钮",
                OnShowHideButtonsClicked,
                true,
                !IsCollapsed);

            Menu_AppendSeparator(menu);

            // 添加模式切换选项
            var modeItem = Menu_AppendItem(menu, "空值模式", OnModeToggle, true, UseEmptyValueMode);
            modeItem.ToolTipText = "切换是否使用空值模式进行Hide/Lock控制";

            if (UseEmptyValueMode)
            {
                // 只在空值模式下显示这些选项
                Menu_AppendItem(menu, "Hide When Empty", OnHideToggle, true, HideWhenEmpty);
                Menu_AppendItem(menu, "Lock When Empty", OnLockToggle, true, LockWhenEmpty);
            }

            Menu_AppendSeparator(menu);

            // 添加跳转到 Motion Slider 的选项
            Menu_AppendItem(menu, "跳转到 Motion Sender", OnJumpToMotionSender, true);

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            ToolStripMenuItem recentKeyMenu = Menu_AppendItem(menu, "选择区间");

            // 获取所有区间并排序
            var sortedKeys = MotilityUtils.GetAllKeys(Instances.ActiveCanvas.Document)
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k =>
                {
                    var parts = k.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double start) &&
                        double.TryParse(parts[1], out double end))
                    {
                        return new { Key = k, Start = start, End = end };
                    }
                    return new { Key = k, Start = double.MaxValue, End = double.MaxValue };
                })
                .OrderBy(x => x.Start)
                .ToList();

            // 创建多列布局
            const int maxItemsPerColumn = 20;
            int totalItems = sortedKeys.Count;
            int columnsNeeded = (int)Math.Ceiling((double)totalItems / maxItemsPerColumn);

            // 创建列菜单
            for (int col = 0; col < columnsNeeded; col++)
            {
                int startIdx = col * maxItemsPerColumn;
                int endIdx = Math.Min(startIdx + maxItemsPerColumn, totalItems);

                if (endIdx <= startIdx) break;

                var columnItems = sortedKeys.GetRange(startIdx, endIdx - startIdx);
                var firstItem = columnItems.First();
                var lastItem = columnItems.Last();

                // 创建列标题
                string columnTitle = $"{firstItem.Start:0}-{lastItem.End:0}";
                var columnMenu = new ToolStripMenuItem(columnTitle);

                // 添加区间项
                foreach (var item in columnItems)
                {
                    var menuItem = new ToolStripMenuItem(item.Key);
                    menuItem.Click += Menu_KeyClicked;
                    columnMenu.DropDownItems.Add(menuItem);
                }

                // 将列添加到主菜单
                recentKeyMenu.DropDownItems.Add(columnMenu);
            }
        }

        private void OnJumpToMotionSender(object sender, EventArgs e)
        {
            if (this.Params.Input[0].SourceCount == 0) return;

            var source = this.Params.Input[0].Sources[0];
            if (source is MotionSender motionSender)
            {
                // 跳转到 Motion Sender
                MotilityUtils.GoComponent(motionSender);
            }
        }

        protected void Menu_KeyClicked(object sender, EventArgs e)
        {
            ToolStripMenuItem keyItem = (ToolStripMenuItem)sender;
            this.NickName = keyItem.Text;
            this.Attributes.ExpireLayout();
        }

        private void UpdateMessage()
        {
            // 解析NickName中的区间
            string[] parts = this.NickName?.Split('-');
            if (parts != null && parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                this.Message = $"[{min}-{max}]";
            }
            else
            {
                this.Message = "Invalid Interval";
            }
        }

        private void OnModeToggle(object sender, EventArgs e)
        {
            UseEmptyValueMode = !UseEmptyValueMode;
            // 切换模式时重置状态
            _lastHasData = true;
            _lastInInterval = true;
            ExpireSolution(true);
        }

        private void OnHideToggle(object sender, EventArgs e)
        {
            if (UseEmptyValueMode)
            {
                _hideWhenEmpty = !_hideWhenEmpty;
                UpdateGroupVisibilityAndLock();
                ExpireSolution(true);
            }
        }

        private void OnLockToggle(object sender, EventArgs e)
        {
            if (UseEmptyValueMode)
            {
                _lockWhenEmpty = !_lockWhenEmpty;
                UpdateGroupVisibilityAndLock();
                ExpireSolution(true);
            }
        }

        private bool isUpdating = false;
        private bool? lastEmptyState = null;

        public void UpdateGroupVisibilityAndLock()
        {
            // 添加超时机制
            if (isUpdating)
            {
                // 如果上次更新时间超过1秒，强制重置状态
                var doc = OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(1000, d => isUpdating = false);
                }
                return;
            }

            try
            {
                isUpdating = true;

                if (affectedObjects == null || !affectedObjects.Any())
                {
                    isUpdating = false;
                    return;
                }

                var doc = OnPingDocument();
                if (doc == null)
                {
                    isUpdating = false;
                    return;
                }

                // 使用 ScheduleSolution 来延迟更新状态
                doc.ScheduleSolution(5, d =>
                {
                    try
                    {
                        // 处理两种模式
                        if (UseEmptyValueMode)
                        {
                            bool isEmpty = this.Params.Input[0].VolatileData.IsEmpty;
                            if (lastEmptyState.HasValue && lastEmptyState.Value == isEmpty)
                            {
                                isUpdating = false;
                                return;
                            }

                            lastEmptyState = isEmpty;

                            // 创建受影响对象的副本以避免集合修改问题
                            var objectsToUpdate = new List<IGH_DocumentObject>(affectedObjects);

                            foreach (var obj in objectsToUpdate)
                            {
                                if (obj == null) continue;

                                try
                                {
                                    if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                                    {
                                        d.ScheduleSolution(1, doc =>
                                        {
                                            previewObj.Hidden = isEmpty;
                                            doc.ExpireSolution();
                                        });
                                    }
                                    if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                                    {
                                        d.ScheduleSolution(1, doc =>
                                        {
                                            activeObj.Locked = isEmpty;
                                            if (isEmpty)
                                            {
                                                activeObj.Phase = GH_SolutionPhase.Blank;
                                            }
                                            doc.ExpireSolution();
                                        });
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (_timelineSlider != null)
                        {
                            double currentValue = (double)_timelineSlider.CurrentValue;
                            string[] parts = this.NickName.Split('-');
                            if (parts.Length == 2 &&
                                double.TryParse(parts[0], out double min) &&
                                double.TryParse(parts[1], out double max))
                            {
                                bool shouldHideOrLock = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);

                                if (_lastHideOrLockState != shouldHideOrLock)
                                {
                                    var objectsToUpdate = new List<IGH_DocumentObject>(affectedObjects);

                                    foreach (var obj in objectsToUpdate)
                                    {
                                        if (obj == null) continue;

                                        try
                                        {
                                            if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                                            {
                                                d.ScheduleSolution(1, doc =>
                                                {
                                                    previewObj.Hidden = shouldHideOrLock;
                                                    doc.ExpireSolution();
                                                });
                                            }
                                            if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                                            {
                                                d.ScheduleSolution(1, doc =>
                                                {
                                                    activeObj.Locked = shouldHideOrLock;
                                                    if (shouldHideOrLock)
                                                    {
                                                        activeObj.Phase = GH_SolutionPhase.Blank;
                                                    }
                                                    doc.ExpireSolution();
                                                });
                                            }
                                        }
                                        catch { }
                                    }

                                    _lastHideOrLockState = shouldHideOrLock;
                                }
                            }
                        }
                    }
                    finally
                    {
                        // 确保在所有操作完成后重置状态
                        d.ScheduleSolution(10, doc => isUpdating = false);
                    }
                });
            }
            catch
            {
                isUpdating = false;
            }
        }

        private void EmptyModeMenuItem_Clicked(object sender, EventArgs e)
        {
            UseEmptyValueMode = !UseEmptyValueMode;
            ExpireSolution(true);
        }

        private void OnShowHideButtonsClicked(object sender, EventArgs e)
        {
            IsCollapsed = !IsCollapsed;
            ExpireSolution(true);
        }
    }
}
