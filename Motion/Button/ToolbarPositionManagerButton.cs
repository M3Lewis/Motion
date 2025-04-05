using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using System.Xml.Serialization;

namespace Motion.Toolbar
{
    public class ToolbarPositionManagerButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 0; // 放在最前面
        private ToolStripButton button;
        private ContextMenuStrip buttonContextMenu;
        private ToolStrip grasshopperToolStrip;
        private ToolStripItemCollection grasshopperToolStripItems;

        // 文件路径常量，用于保存和加载位置设置
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Motion", "ToolbarSettings.xml");

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

            // 加载上次保存的位置设置并应用
            LoadAndApplyPositionSettings();
        }

        private void Instantiate()
        {
            button.Name = "Toolbar Position Manager";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            UpdateButtonImage(); // 替换原来的直接赋值

            button.ToolTipText = "管理工具栏位置";
            button.Click += Button_Click;

            // 创建右键菜单
            CreateContextMenu();
            button.MouseUp += Button_MouseUp;
        }

        // 添加新方法用于更新按钮图标
        private void UpdateButtonImage()
        {
            if (!CustomMotionToolbar.customMotionToolbar.Visible)
            {
                button.Image = Properties.Resources.ToolbarPositionManagerOnToolbar;
                return;
            }

            switch (CustomMotionToolbar.CurrentPosition)
            {
                case ToolbarPosition.Top:
                    button.Image = Properties.Resources.ToolbarPositionManagerTop;
                    break;
                case ToolbarPosition.Left:
                    button.Image = Properties.Resources.ToolbarPositionManagerLeft;
                    break;
                case ToolbarPosition.Right:
                    button.Image = Properties.Resources.ToolbarPositionManagerRight;
                    break;
                case ToolbarPosition.Bottom:
                    button.Image = Properties.Resources.ToolbarPositionManagerBottom;
                    break;
                default:
                    button.Image = Properties.Resources.ToolbarPositionManagerTop;
                    break;
            }
        }

        private void MoveToolbarItems(ToolbarPosition position)
        {
            // 收集需要移动的按钮
            List<ToolStripItem> itemsToMove = CollectItemsToMove();

            // 根据目标位置处理按钮
            if (position == ToolbarPosition.OnToolbar)
            {
                MoveItemsToGrasshopperToolbar(itemsToMove);
            }
            else
            {
                MoveItemsToCustomToolbar(itemsToMove, position);
            }

            // 更新按钮图标
            UpdateButtonImage();

            // 保存位置设置到文件
            SavePositionSettings(position);

            // 显示位置变更消息
            ShowTemporaryMessage(Instances.ActiveCanvas, $"工具栏已移动到: {position}");
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

            if (CustomMotionToolbar.customMotionToolbar.Visible)
            {
                switch (CustomMotionToolbar.CurrentPosition)
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


        private static ToolStrip GetGrasshopperToolbar()
        {
            try
            {
                var editor = Instances.DocumentEditor;
                if (editor == null) return null;

                Type typeFromHandle = typeof(GH_DocumentEditor);
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);

                if (field == null) return null;

                object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(editor));
                if (objectValue == null) return null;

                return (ToolStrip)objectValue;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error getting Grasshopper toolbar: {ex.Message}");
                return null;
            }
        }
        private static void MoveToGrasshopperToolbar()
        {
            CustomMotionToolbar.customMotionToolbar.Visible = false;

            ToolStrip grasshopperToolbar = GetGrasshopperToolbar();
            if (grasshopperToolbar != null)
            {
                // 创建分隔符
                ToolStripSeparator separator = new ToolStripSeparator();
                grasshopperToolbar.Items.Add(separator);

                // 移动项目到Grasshopper工具栏
                while (CustomMotionToolbar.customMotionToolbar.Items.Count > 0)
                {
                    ToolStripItem item = CustomMotionToolbar.customMotionToolbar.Items[0];
                    CustomMotionToolbar.customMotionToolbar.Items.RemoveAt(0);
                    grasshopperToolbar.Items.Add(item);
                }

                // 添加结束分隔符
                grasshopperToolbar.Items.Add(new ToolStripSeparator());
            }
        }


        private List<ToolStripItem> CollectItemsToMove()
        {
            List<ToolStripItem> itemsToMove = new List<ToolStripItem>();
            object objectValue = GetGrasshopperToolbar();

            if (objectValue is ToolStrip toolStrip)
            {
                grasshopperToolStrip = toolStrip;
                grasshopperToolStripItems = toolStrip.Items;
            }

            // 如果当前在Grasshopper工具栏上，则需要收集工具栏上的Motion按钮
            if (grasshopperToolStrip != null && !CustomMotionToolbar.customMotionToolbar.Visible)
            {
                CollectItemsFromGrasshopperToolbar(itemsToMove);
            }

            // 如果当前在CustomMotionToolbar上，收集所有按钮
            if (CustomMotionToolbar.customMotionToolbar.Visible)
            {
                CollectItemsFromCustomToolbar(itemsToMove);
            }

            return itemsToMove;
        }

        private void CollectItemsFromGrasshopperToolbar(List<ToolStripItem> itemsToMove)
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

        private void CollectItemsFromCustomToolbar(List<ToolStripItem> itemsToMove)
        {
            for (int i = CustomMotionToolbar.customMotionToolbar.Items.Count - 1; i >= 0; i--)
            {
                var item = CustomMotionToolbar.customMotionToolbar.Items[i];
                itemsToMove.Add(item);
                CustomMotionToolbar.customMotionToolbar.Items.RemoveAt(i);
            }
        }

        private void MoveItemsToGrasshopperToolbar(List<ToolStripItem> itemsToMove)
        {
            // 将所有按钮移到Grasshopper工具栏
            if (grasshopperToolStrip != null)
            {
                // 确保分隔符存在
                EnsureSeparatorsExist();

                // 按照ToolbarOrder排序
                SortItemsByToolbarOrder(itemsToMove);

                // 找到倒数第二个和最后一个分隔符
                int leftSeparatorIndex = FindLeftSeparatorIndex();

                // 在左分隔符后面插入所有按钮
                if (leftSeparatorIndex != -1)
                {
                    InsertItemsAfterSeparator(itemsToMove, leftSeparatorIndex);
                }
            }

            // 隐藏自定义工具栏
            CustomMotionToolbar.customMotionToolbar.Visible = false;
        }

        private void InsertItemsAfterSeparator(List<ToolStripItem> itemsToMove, int separatorIndex)
        {
            int insertIndex = separatorIndex + 1;
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

        private void MoveItemsToCustomToolbar(List<ToolStripItem> itemsToMove, ToolbarPosition position)
        {
            // 设置自定义工具栏位置
            CustomMotionToolbar.customMotionToolbar.Visible = true;
            CustomMotionToolbar.SetPosition(position);

            // 配置工具栏外观
            ConfigureCustomToolbarAppearance(position);

            // 按照ToolbarOrder排序
            SortItemsByToolbarOrder(itemsToMove);

            // 添加到自定义工具栏
            foreach (var item in itemsToMove)
            {
                CustomMotionToolbar.customMotionToolbar.Items.Add(item);
            }

            // 确保工具栏在Z顺序中是最前面的
            CustomMotionToolbar.customMotionToolbar.BringToFront();
        }

        private void ConfigureCustomToolbarAppearance(ToolbarPosition position)
        {
            var oldToolbar = GetGrasshopperToolbar();
            int size = 30;

            // 根据目标位置设置工具栏方向和样式
            switch (position)
            {
                case ToolbarPosition.Left:
                    SetToolbarOrientation(DockStyle.Left, size + 10, Instances.ActiveCanvas.Height, ToolStripLayoutStyle.VerticalStackWithOverflow);
                    break;
                case ToolbarPosition.Right:
                    SetToolbarOrientation(DockStyle.Right, size + 10, Instances.ActiveCanvas.Height, ToolStripLayoutStyle.VerticalStackWithOverflow);
                    break;
                case ToolbarPosition.Top:
                    SetToolbarOrientation(DockStyle.Top, oldToolbar.Width, size, ToolStripLayoutStyle.HorizontalStackWithOverflow);
                    break;
                case ToolbarPosition.Bottom:
                    SetToolbarOrientation(DockStyle.Bottom, oldToolbar.Width, size, ToolStripLayoutStyle.HorizontalStackWithOverflow);
                    break;
            }
        }

        private void SortItemsByToolbarOrder(List<ToolStripItem> items)
        {
            items.Sort((a, b) =>
            {
                int orderA = GetToolbarOrder(a.Tag as MotionToolbarButton);
                int orderB = GetToolbarOrder(b.Tag as MotionToolbarButton);
                return orderA.CompareTo(orderB);
            });
        }
        private void SetToolbarOrientation(DockStyle dockStyle, int width, int height, ToolStripLayoutStyle layoutStyle)
        {
            CustomMotionToolbar.customMotionToolbar.AutoSize = false;
            CustomMotionToolbar.customMotionToolbar.Dock = dockStyle;
            CustomMotionToolbar.customMotionToolbar.Width = width;
            CustomMotionToolbar.customMotionToolbar.Height = height;
            CustomMotionToolbar.customMotionToolbar.GripStyle = ToolStripGripStyle.Hidden;
            CustomMotionToolbar.customMotionToolbar.LayoutStyle = layoutStyle;
            CustomMotionToolbar.customMotionToolbar.ImageScalingSize = new Size(24, 24);
            if (CustomMotionToolbar.CurrentPosition == ToolbarPosition.OnToolbar)
            {
                MoveToGrasshopperToolbar();
            }
        }

        // 获取按钮的ToolbarOrder
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

        // 创建一个可序列化的设置类
        [Serializable]
        public class ToolbarSettings
        {
            public ToolbarPosition Position { get; set; }

            public ToolbarSettings()
            {
                Position = ToolbarPosition.OnToolbar; // 默认位置
            }

            public ToolbarSettings(ToolbarPosition position)
            {
                Position = position;
            }
        }

        // 保存位置设置到文件
        private void SavePositionSettings(ToolbarPosition position)
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建设置对象
                ToolbarSettings settings = new ToolbarSettings(position);

                // 序列化到XML
                XmlSerializer serializer = new XmlSerializer(typeof(ToolbarSettings));
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    serializer.Serialize(writer, settings);
                }

                Rhino.RhinoApp.WriteLine($"已保存工具栏位置设置: {position}");
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"保存工具栏位置设置失败: {ex.Message}");
            }
        }

        // 加载位置设置并应用
        private void LoadAndApplyPositionSettings()
        {
            try
            {
                // 检查设置文件是否存在
                if (!File.Exists(SettingsFilePath))
                {
                    return; // 如果不存在，使用默认设置
                }

                // 反序列化XML
                XmlSerializer serializer = new XmlSerializer(typeof(ToolbarSettings));
                ToolbarSettings settings;

                using (StreamReader reader = new StreamReader(SettingsFilePath))
                {
                    settings = (ToolbarSettings)serializer.Deserialize(reader);
                }

                // 应用加载的设置
                if (settings != null)
                {
                    // 使用延迟执行，确保UI已经完全加载
                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000; // 1秒后执行
                    timer.Tick += (sender, e) =>
                    {
                        timer.Stop();
                        MoveToolbarItems(settings.Position);
                        Rhino.RhinoApp.WriteLine($"已应用工具栏位置设置: {settings.Position}");
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"加载工具栏位置设置失败: {ex.Message}");
            }
        }
    }
}