using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System.Linq;
using Grasshopper.GUI.Canvas;
using System.Drawing;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using Rhino;

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

                //toolStrip = (ToolStrip)editor.Controls[0].Controls[1];

                Type typeFromHandle = typeof(GH_DocumentEditor);
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);
                object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));

                if (objectValue is ToolStrip toolStrip)
                {
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
                else
                {
                    RhinoApp.WriteLine($"Toolbar object is not a ToolStrip: {objectValue?.GetType().FullName ?? "null"}");
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

        protected void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                // 保存当前的变换矩阵
                var originalTransform = g.Transform;
                
                // 重置变换，确保文字大小不受画布缩放影响
                g.ResetTransform();

                // 计算文本大小
                SizeF textSize = new SizeF(30, 30);
                
                // 设置消息位置在画布顶部居中
                float padding = 20;
                float x = textSize.Width + 300;
                float y = padding + 30;

                RectangleF textBounds = new RectangleF(x, y, textSize.Width + 300, textSize.Height + 30);
                textBounds.Inflate(6, 3);  // 添加一些内边距

                // 绘制消息
                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();

                // 恢复原始变换
                g.Transform = originalTransform;
            };

            // 添加临时事件处理器
            canvas.CanvasPostPaintObjects += canvasRepaint;
            
            // 立即刷新画布以显示消息
            canvas.Refresh();

            // 设置定时器移除事件处理器
            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPostPaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }
    }
} 