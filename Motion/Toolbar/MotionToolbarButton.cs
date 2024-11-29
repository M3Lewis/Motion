using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System.Linq;

namespace Motion.Toolbar
{
    public abstract class MotionToolbarButton : GH_AssemblyPriority
    {
        protected static ToolStrip toolStrip;
        protected static ToolStripItemCollection toolStripItems;
        private static bool separatorsAdded = false;

        protected virtual int ToolbarOrder => 0;

        protected void InitializeToolbarGroup()
        {
            if (toolStrip == null)
            {
                var editor = Instances.DocumentEditor;
                if (editor == null) return;

                // 获取指定的工具栏
                toolStrip = (ToolStrip)editor.Controls[0].Controls[1];
                if (toolStrip == null) return;

                toolStripItems = toolStrip.Items;

                if (!separatorsAdded)
                {
                    // 在倒数第二个位置添加左分隔符
                    toolStripItems.Insert(toolStripItems.Count - 1, new ToolStripSeparator());
                    // 在最后添加右分隔符
                    toolStripItems.Add(new ToolStripSeparator());
                    separatorsAdded = true;
                }
            }
        }

        protected void AddButtonToGroup(ToolStripItem button)
        {
            if (toolStripItems == null) return;

            // 找到倒数第二个和最后一个分隔符
            int leftSeparatorIndex = -1;
            int rightSeparatorIndex = -1;

            for (int i = toolStripItems.Count - 1; i >= 0; i--)
            {
                if (toolStripItems[i] is ToolStripSeparator)
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
                var item = toolStripItems[i];
                if (item.Tag is MotionToolbarButton existingButton 
                    && existingButton.ToolbarOrder > this.ToolbarOrder)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;  // 如果没找到更大的顺序，就放在当前按钮后面
            }

            // 设置按钮的Tag为当前实例，用于排序
            button.Tag = this;
            
            // 在正确位置插入按钮
            toolStripItems.Insert(insertIndex, button);
        }
    }
} 