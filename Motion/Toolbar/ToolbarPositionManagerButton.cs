using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using System.Collections.Generic;
using System.Linq;

namespace Motion.Toolbar
{
    public class ToolbarPositionManagerButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 0; // 放在最前面
        private ToolStripButton button;
        private ContextMenuStrip buttonContextMenu;

        public ToolbarPositionManagerButton()
        {
        }

        private void AddToolbarPositionManagerButton()
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
            AddToolbarPositionManagerButton();
        }

        private void Instantiate()
        {
            button.Name = "Toolbar Position Manager";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = /*Properties.Resources.ToolbarPositionManager*/ null; // 需要添加对应的图标
            button.ToolTipText = "管理工具栏位置";
            button.Click += Button_Click;

            // 创建右键菜单
            CreateContextMenu();
            button.MouseUp += Button_MouseUp;
        }

        private void CreateContextMenu()
        {
            buttonContextMenu = new ContextMenuStrip();

            // 添加位置选项
            buttonContextMenu.Items.Add("Top", null, (s, e) => MoveToolbarItems(ToolbarPosition.Top));
            buttonContextMenu.Items.Add("Left", null, (s, e) => MoveToolbarItems(ToolbarPosition.Left));
            buttonContextMenu.Items.Add("Right", null, (s, e) => MoveToolbarItems(ToolbarPosition.Right));
            buttonContextMenu.Items.Add("Bottom", null, (s, e) => MoveToolbarItems(ToolbarPosition.Bottom));
            buttonContextMenu.Items.Add("On Toolbar", null, (s, e) => MoveToolbarItems(ToolbarPosition.OnToolbar));
        }

        private void Button_Click(object sender, EventArgs e)
        {
            OnButtonClicked(sender, e);
        }

        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point screenPoint;
                if (sender is ToolStripItem item && item.Owner != null)
                {
                    screenPoint = item.Owner.PointToScreen(new Point(e.X, e.Y));
                }
                else
                {
                    screenPoint = button.Owner.PointToScreen(new Point(e.X, e.Y));
                }

                buttonContextMenu.Show(screenPoint);
            }
        }

        protected override void OnButtonClicked(object sender, EventArgs e)
        {
            // 显示当前工具栏位置信息
            ShowPositionInfo();
        }

        private void ShowPositionInfo()
        {
            // 获取当前工具栏位置
            string positionName = "Unknown";

            if (CustomMotionToolbar.Instance.Visible)
            {
                switch (CustomMotionToolbar.Instance.CurrentPosition)
                {
                    case ToolbarPosition.Top:
                        positionName = "Top";
                        break;
                    case ToolbarPosition.Left:
                        positionName = "Left";
                        break;
                    case ToolbarPosition.Right:
                        positionName = "Right";
                        break;
                    case ToolbarPosition.Bottom:
                        positionName = "Bottom";
                        break;
                }
            }
            else
            {
                positionName = "On Toolbar";
            }

            // 在画布上显示一个临时消息
            ShowTemporaryMessage(Instances.ActiveCanvas, $"当前工具栏位置: {positionName}");
        }

        private void MoveToolbarItems(ToolbarPosition position)
        {
            // 获取当前所有的工具栏按钮
            List<ToolStripItem> itemsToMove = new List<ToolStripItem>();
            object objectValue = GetGrasshopperToolbar();

            if (objectValue is ToolStrip toolStrip)
            {
                grasshopperToolStrip = toolStrip;
                grasshopperToolStripItems = toolStrip.Items;
            }

            // 如果当前在Grasshopper工具栏上，则需要收集工具栏上的Motion按钮
            if (grasshopperToolStrip != null&& !CustomMotionToolbar.Instance.Visible)
            {
                // 查找所有标记有MotionToolbarButton的项目
                for (int i = grasshopperToolStripItems.Count - 1; i >= 0; i--)
                {
                    var item = grasshopperToolStripItems[i];
                    if (item.Tag is MotionToolbarButton)
                    {
                        itemsToMove.Add(item);
                        grasshopperToolStripItems.RemoveAt(i);
                    }
                }
            }

            // 如果当前在CustomMotionToolbar上，收集所有按钮
            if (CustomMotionToolbar.Instance.Visible)
            {
                for (int i = CustomMotionToolbar.Instance.Items.Count - 1; i >= 0; i--)
                {
                    var item = CustomMotionToolbar.Instance.Items[i];
                    itemsToMove.Add(item);
                    CustomMotionToolbar.Instance.Items.RemoveAt(i);
                }
            }

            // 根据选择的位置添加按钮
            if (position == ToolbarPosition.OnToolbar)
            {
                // 将所有按钮移到Grasshopper工具栏
                if (grasshopperToolStrip != null)
                {
                    // 确保分隔符存在
                    EnsureSeparatorsExist();

                    // 按照ToolbarOrder排序（使用GetToolbarOrder方法）
                    itemsToMove.Sort((a, b) =>
                    {
                        int orderA = GetToolbarOrder(a.Tag as MotionToolbarButton);
                        int orderB = GetToolbarOrder(b.Tag as MotionToolbarButton);
                        return orderA.CompareTo(orderB);
                    });

                    // 找到倒数第二个和最后一个分隔符
                    int leftSeparatorIndex = FindLeftSeparatorIndex();

                    // 在左分隔符后面插入所有按钮
                    if (leftSeparatorIndex != -1)
                    {
                        int insertIndex = leftSeparatorIndex + 1;
                        foreach (var item in itemsToMove)
                        {
                            grasshopperToolStripItems.Insert(insertIndex++, item);

                            // Verify insertion worked
                            if (!grasshopperToolStripItems.Contains(item))
                            {
                                // Insertion failed, handle error
                                ShowTemporaryMessage(Instances.ActiveCanvas, "Error moving toolbar items to Grasshopper toolbar");
                                return;
                            }
                        }
                    }
                }

                // 隐藏自定义工具栏
                CustomMotionToolbar.Instance.Visible = false;
            }
            else
            {
                // 设置自定义工具栏位置
                CustomMotionToolbar.Instance.Visible = true;
                CustomMotionToolbar.Instance.SetPosition(position);

                // 按照ToolbarOrder排序（使用GetToolbarOrder方法）
                itemsToMove.Sort((a, b) =>
                {
                    int orderA = GetToolbarOrder(a.Tag as MotionToolbarButton);
                    int orderB = GetToolbarOrder(b.Tag as MotionToolbarButton);
                    return orderA.CompareTo(orderB);
                });

                // 添加到自定义工具栏
                foreach (var item in itemsToMove)
                {
                    CustomMotionToolbar.Instance.Items.Add(item);
                }

                // 确保工具栏在Z顺序中是最前面的
                CustomMotionToolbar.Instance.BringToFront();
            }

            // 显示位置变更消息
            ShowTemporaryMessage(Instances.ActiveCanvas, $"工具栏已移动到: {position}");
        }

        // 新方法：获取按钮的ToolbarOrder
        private int GetToolbarOrder(MotionToolbarButton button)
        {
            // 如果按钮为null，返回默认值0
            if (button == null)
                return 0;

            // 使用反射获取ToolbarOrder属性值
            // 这样可以避免直接访问受保护成员的问题
            var type = button.GetType();
            var property = type.GetProperty("ToolbarOrder",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.GetProperty);

            if (property != null)
            {
                return (int)property.GetValue(button);
            }

            // 如果无法获取属性值，返回默认值0
            return 0;
        }

        private void EnsureSeparatorsExist()
        {
            // 检查分隔符是否存在，不存在则添加
            bool leftSeparatorFound = false;
            bool rightSeparatorFound = false;

            for (int i = 0; i < grasshopperToolStripItems.Count; i++)
            {
                if (grasshopperToolStripItems[i] is ToolStripSeparator)
                {
                    if (!leftSeparatorFound)
                    {
                        leftSeparatorFound = true;
                    }
                    else
                    {
                        rightSeparatorFound = true;
                        break;
                    }
                }
            }

            if (!leftSeparatorFound)
            {
                grasshopperToolStripItems.Insert(grasshopperToolStripItems.Count - 1, new ToolStripSeparator());
            }

            if (!rightSeparatorFound)
            {
                grasshopperToolStripItems.Add(new ToolStripSeparator());
            }
        }

        private int FindLeftSeparatorIndex()
        {
            int separatorsFound = 0;

            for (int i = grasshopperToolStripItems.Count - 1; i >= 0; i--)
            {
                if (grasshopperToolStripItems[i] is ToolStripSeparator)
                {
                    separatorsFound++;
                    if (separatorsFound == 2)
                    {
                        return i; // 返回倒数第二个分隔符的索引
                    }
                }
            }

            return -1; // 如果没找到则返回-1
        }
    }
}
