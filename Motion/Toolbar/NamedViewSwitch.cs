using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Motion.Toolbar
{
    public class NamedViewSwitch : MotionToolbarButton
    {
        private ToolStripButton button;
        private List<string> namedViews = new List<string>();
        private int currentViewIndex = 0;
        private bool isActive = true;

        public NamedViewSwitch()
        {

        }
        private void AddNamedViewSwitchButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            LoadNamedViews();
            AddButtonToGroup(button); // 使用基类方法添加按钮
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

            //Check if DocumentEditor was instantiated
            if (editor == null) return;
            AddNamedViewSwitchButton();
        }

        private void Instantiate()
        {
            // 配置按钮
            button.Name = "View Switch";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.NamedViewSwitch2;
            button.ToolTipText = "Switch between named views using +/- keys";
            button.Click += ClickedButton;
        }

        private void LoadNamedViews()
        {
            namedViews.Clear();
            if (Instances.ActiveCanvas?.Document != null)
            {
                var viewList = Instances.ActiveCanvas.Document.Properties.ViewList;
                foreach (var view in viewList)
                {
                    namedViews.Add(view.Name);
                }
            }
        }

        private void ClickedButton(object sender, EventArgs e)
        {
            isActive = !isActive;

            if (isActive)
            {
                //button.Image = Properties.Resources.YourActiveIcon; // 激活状态图标
                button.BackColor = Color.FromArgb(41, 141, 173); // 激活状态背景色
                Instances.DocumentEditor.KeyDown += KeyDownEventHandler;
            }
            else
            {
                //button.Image = Properties.Resources.YourIcon; // 默认图标
                button.BackColor = Color.FromArgb(255, 255, 255); // 默认背景色
                Instances.DocumentEditor.KeyDown -= KeyDownEventHandler;
            }
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {
            LoadNamedViews();
            if (!isActive) return;
            if (namedViews.Count <= 0)
            {
                var doc = Grasshopper.Instances.ActiveCanvas.Document;
                var canvas = Grasshopper.Instances.ActiveCanvas;
                bool isPressed = e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Oemplus;
                if (canvas != null&& isPressed)
                {
                    ShowTemporaryMessage(canvas,
                        $"请创建一个Named View!");
                }
                return;
            }

            if (e.KeyCode == Keys.Oemplus)
            {
                currentViewIndex = (currentViewIndex + 1) % namedViews.Count;
                SwitchToView(currentViewIndex);
            }
            else if (e.KeyCode == Keys.OemMinus)
            {
                currentViewIndex = (currentViewIndex - 1 + namedViews.Count) % namedViews.Count;
                SwitchToView(currentViewIndex);
            }
        }

        private void SwitchToView(int index)
        {
            try
            {
                if (Instances.ActiveCanvas?.Document == null) return;

                var views = Instances.ActiveCanvas.Document.Properties.ViewList;
                if (index >= 0 && index < views.Count)
                {
                    // 直接使用视图列表中的 GH_NamedView 对象
                    GH_NamedView namedView = views[index];

                    // 使用带动画效果的切换(length代表切换时间)
                    namedView.SetToViewport(Instances.ActiveCanvas, 200);

                    // 或者使用即时切换
                    // namedView.SetToViewport(Instances.ActiveCanvas);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error switching view: {ex.Message}");
            }
        }

        private void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            GH_Canvas.CanvasPrePaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                // 保存当前的变换矩阵
                var originalTransform = g.Transform;
                
                // 重置变换，确保文字大小不受画布缩放影响
                g.ResetTransform();

                // 计算文本大小
                SizeF textSize = new SizeF(30, 30) ;
                
                // 设置消息位置在画布顶部居中
                float padding = 20; // 顶部边距
                float x = textSize.Width+300;
                float y = padding+30;

                RectangleF textBounds = new RectangleF(x, y, textSize.Width+300, textSize.Height+30);
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
            canvas.CanvasPrePaintObjects += canvasRepaint;

            // 设置定时器移除事件处理器
            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPrePaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }
    }
}
