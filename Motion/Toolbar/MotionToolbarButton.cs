using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public abstract class MotionToolbarButton : GH_AssemblyPriority
    {
        protected static bool isToolbarInitialized = false;
        protected static ToolStripSeparator leftSeparator;
        protected static ToolStripSeparator rightSeparator;

        protected void InitializeToolbarGroup()
        {
            if (isToolbarInitialized) return;

            ToolStrip toolbar = (ToolStrip)Instances.DocumentEditor.Controls[0].Controls[1];
            
            // 添加左分隔线
            leftSeparator = new ToolStripSeparator();
            toolbar.Items.Add(leftSeparator);

            // 添加右分隔线
            rightSeparator = new ToolStripSeparator();
            toolbar.Items.Add(rightSeparator);

            isToolbarInitialized = true;
        }

        protected void AddButtonToGroup(ToolStripButton button)
        {
            ToolStrip toolbar = (ToolStrip)Instances.DocumentEditor.Controls[0].Controls[1];
            
            // 在右分隔线之前插入按钮
            int insertIndex = toolbar.Items.IndexOf(rightSeparator);
            toolbar.Items.Remove(button); // 确保按钮不会重复添加
            toolbar.Items.Insert(insertIndex, button);
        }
    }
} 