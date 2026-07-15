using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Motion.General;
using Rhino;

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
            Menu_AppendItem(menu,
                LanguageManager.GetString("Menu.ShowHideButtons", "显示HIDE/LOCK按钮"),
                OnShowHideButtonsClicked,
                true,
                !IsCollapsed);

            Menu_AppendSeparator(menu);

            var invertItem = Menu_AppendItem(menu, 
                LanguageManager.GetString("Menu.InvertLogic", "反转隐藏/锁定逻辑"), 
                OnInvertToggle, 
                true, 
                InvertHideAndLock);
            invertItem.ToolTipText = LanguageManager.GetString("Menu.InvertLogicTooltip", "启用后：时间区间内隐藏/锁定组件。\n禁用后：时间区间外隐藏/锁定组件。");

            Menu_AppendSeparator(menu);

            // 添加跳转到 Motion Slider 的选项
            Menu_AppendItem(menu, 
                LanguageManager.GetString("Menu.JumpToSender", "跳转到 Motion Sender"), 
                OnJumpToMotionSender, 
                true);

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            ToolStripMenuItem recentKeyMenu = Menu_AppendItem(menu, 
                LanguageManager.GetString("Menu.SelectInterval", "选择区间"));

            // 获取所有区间并排序
            var sortedKeys = MotilityUtils.GetAllKeys(Instances.ActiveCanvas.Document)
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k =>
                {
                    if (MotilityUtils.TryParseNickNameInterval(k, out double start, out double end))
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

        private void OnInvertToggle(object sender, EventArgs e)
        {
            InvertHideAndLock = !InvertHideAndLock;
            ExpireSolution(true);
        }

        private bool isUpdating = false;

        public void UpdateGroupVisibilityAndLock(IEnumerable<IGH_DocumentObject> extraObjectsToUpdate = null)
        {
            if (isUpdating)
            {
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

                var doc = OnPingDocument();
                if (doc == null) return;

                var targets = new List<IGH_DocumentObject>();
                if (affectedObjects != null)
                {
                    targets.AddRange(affectedObjects);
                }
                if (extraObjectsToUpdate != null)
                {
                    targets.AddRange(extraObjectsToUpdate);
                }

                var uniqueTargets = targets.Where(obj => obj != null).Distinct().ToList();
                if (!uniqueTargets.Any()) return;

                MotilityUtils.UpdateObjectsVisibilityAndLock(doc, uniqueTargets);

                if (_timelineSlider != null && MotilityUtils.TryParseNickNameInterval(this.NickName, out double min, out double max))
                {
                    double currentValue = (double)_timelineSlider.CurrentValue;
                    _lastHideOrLockState = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void OnShowHideButtonsClicked(object sender, EventArgs e)
        {
            IsCollapsed = !IsCollapsed;
            ExpireSolution(true);
        }
    }
}