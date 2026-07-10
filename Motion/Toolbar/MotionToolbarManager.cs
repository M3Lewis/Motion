using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Rhino;

namespace Motion.Toolbar
{
    public static class MotionToolbarManager
    {
        private static ToolStrip grasshopperToolStrip;
        private static ToolStripItemCollection grasshopperToolStripItems;
        private static bool separatorsAdded = false;

        public static ToolStrip GetGrasshopperToolbar()
        {
            var editor = Instances.DocumentEditor;
            if (editor == null) return null;

            Type typeFromHandle = typeof(GH_DocumentEditor);
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
            FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);
            object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));
            if (objectValue == null) return null;

            return (ToolStrip)objectValue;
        }

        public static void InitializeToolbarGroup(ToolbarType preferredToolbarType)
        {
            if (grasshopperToolStrip == null &&
                (preferredToolbarType == ToolbarType.GrasshopperDefault))
            {
                object objectValue = GetGrasshopperToolbar();

                if (objectValue is ToolStrip toolStrip)
                {
                    grasshopperToolStrip = toolStrip;
                    grasshopperToolStripItems = toolStrip.Items;
                    if (!separatorsAdded)
                    {
                        // 在倒数第二个位置添加左分隔符
                        grasshopperToolStripItems.Insert(grasshopperToolStripItems.Count - 1, new ToolStripSeparator());
                        // 在最后添加右分隔符
                        grasshopperToolStripItems.Add(new ToolStripSeparator());
                        separatorsAdded = true;
                    }
                }
                else
                {
                    RhinoApp.WriteLine($"Toolbar object is not a ToolStrip: {objectValue?.GetType().FullName ?? "null"}");
                }
            }

            // 确保自定义工具栏已初始化
            if (preferredToolbarType == ToolbarType.CustomMotion)
            {
                // 获取或创建自定义工具栏实例
                CustomMotionToolbar.customMotionToolbar.Visible = true;
            }
        }

        public static void AddButtonToToolbars(ToolStripItem originalButton, ToolbarType preferredToolbarType, int toolbarOrder, MotionToolbarButton buttonInstance)
        {
            // 根据设置添加到不同的工具栏
            switch (preferredToolbarType)
            {
                case ToolbarType.GrasshopperDefault:
                    AddButtonToGrasshopperToolbar(originalButton, toolbarOrder, buttonInstance);
                    break;

                case ToolbarType.CustomMotion:
                    AddButtonToCustomToolbar(originalButton, toolbarOrder, buttonInstance);
                    break;
            }
        }

        private static void AddButtonToGrasshopperToolbar(ToolStripItem button, int toolbarOrder, MotionToolbarButton buttonInstance)
        {
            if (grasshopperToolStripItems == null) return;

            // 找到倒数第二个和最后一个分隔符
            int leftSeparatorIndex = -1;
            int rightSeparatorIndex = -1;

            for (int i = grasshopperToolStripItems.Count - 1; i >= 0; i--)
            {
                if (grasshopperToolStripItems[i] is ToolStripSeparator)
                {
                    if (rightSeparatorIndex == -1)
                    {
                        rightSeparatorIndex = i;
                    }
                    else
                    {
                        leftSeparatorIndex = i;
                        break;
                    }
                }
            }

            if (leftSeparatorIndex == -1 || rightSeparatorIndex == -1) return;

            // 在两个分隔符之间找到合适的插入位置
            int insertIndex = leftSeparatorIndex + 1;  // 默认插在左分隔符后面
            for (int i = leftSeparatorIndex + 1; i < rightSeparatorIndex; i++)
            {
                var item = grasshopperToolStripItems[i];
                if (item.Tag is MotionToolbarButton existingButton
                    && existingButton.ToolbarOrder > toolbarOrder)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;  // 如果没找到更大的顺序，就放在当前按钮后面
            }

            // 设置按钮的Tag为当前实例，用于排序
            button.Tag = buttonInstance;

            // 在正确位置插入按钮
            grasshopperToolStripItems.Insert(insertIndex, button);
        }

        private static void AddButtonToCustomToolbar(ToolStripItem button, int toolbarOrder, MotionToolbarButton buttonInstance)
        {
            // 设置按钮的Tag为当前实例
            button.Tag = buttonInstance;

            // 按照顺序添加到自定义工具栏
            int insertIndex = 0;
            for (int i = 0; i < CustomMotionToolbar.customMotionToolbar.Items.Count; i++)
            {
                var item = CustomMotionToolbar.customMotionToolbar.Items[i];
                if (item.Tag is MotionToolbarButton existingButton
                    && existingButton.ToolbarOrder > toolbarOrder)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }

            CustomMotionToolbar.customMotionToolbar.Items.Insert(insertIndex, button);
        }

        public static void UpdateLanguageAll()
        {
            if (CustomMotionToolbar.customMotionToolbar != null)
            {
                foreach (ToolStripItem item in CustomMotionToolbar.customMotionToolbar.Items)
                {
                    if (item.Tag is MotionToolbarButton btn)
                    {
                        btn.UpdateLanguage();
                    }
                }
            }

            if (grasshopperToolStripItems != null)
            {
                foreach (ToolStripItem item in grasshopperToolStripItems)
                {
                    if (item.Tag is MotionToolbarButton btn)
                    {
                        btn.UpdateLanguage();
                    }
                }
            }
        }
    }
}
