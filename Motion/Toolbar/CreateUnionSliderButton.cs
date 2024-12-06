using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using Motion.Animation;
using System.Linq;
using System.Collections.Generic;

namespace Motion.Toolbar
{
    public class CreateUnionSliderButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 20;

        public CreateUnionSliderButton()
        {
        }

        private void AddControlButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button);
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
            AddControlButton();
        }

        private void Instantiate()
        {
            button.Name = "Create Union Slider";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.CreateUnionSlider;
            button.ToolTipText = "LMB：创建/更新Motion Union Slider，以控制选定的Motion Slider \nRMB：移除对Motion Slider的控制";
            button.MouseDown += Button_MouseDown;
            button.CheckOnClick = false;
        }

        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            var canvas = Instances.ActiveCanvas;
            if (canvas == null) return;

            var selectedSliders = new List<MotionSlider>();

            // 获取所有选中的 MotionSlider
            foreach (IGH_DocumentObject obj in canvas.Document.SelectedObjects())
            {
                if (obj is MotionSlider slider && !(obj is MotionUnionSlider))  // 确保不选中 MotionUnionSlider
                {
                    selectedSliders.Add(slider);
                }
            }

            if (selectedSliders.Count == 0)
            {
                ShowMessage("Please select the sliders to be controlled first");
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // 获取所有文档中的 MotionSlider（不包括 MotionUnionSlider）
                var allSliders = canvas.Document.Objects
                    .OfType<MotionSlider>()
                    .Where(s => !(s is MotionUnionSlider))
                    .ToList();

                // 计算全局范围（考虑所有滑块，而不仅仅是选中的）
                decimal globalMin = allSliders.Min(s => s.Slider.Minimum);
                decimal globalMax = allSliders.Max(s => s.Slider.Maximum);

                var existingController = FindExistingController(selectedSliders);

                if (existingController != null)
                {
                    // 更新现有主控滑块的范围
                    existingController.SetRange(globalMin, globalMax);
                    existingController.IsMainController = true;

                    // 更新控制关系
                    foreach (var slider in selectedSliders)
                    {
                        if (slider != existingController)
                        {
                            existingController.AddControlledSlider(slider);
                        }
                    }

                    // 强制更新
                    existingController.ExpireSolution(true);
                    canvas.Refresh();

                    ShowMessage($"Already updated union slider ({globalMin}-{globalMax})，controlled {selectedSliders.Count - 1} slider(s)");
                }
                else
                {
                    // 创建新的联合滑块
                    var controller = new MotionUnionSlider(globalMin, globalMax);

                    // 先创建属性
                    controller.CreateAttributes();
                    controller.IsMainController = true;

                    // 计算新滑块位置和大小
                    float minX = selectedSliders.Min(s => s.Attributes.Bounds.Left);
                    float maxX = selectedSliders.Max(s => s.Attributes.Bounds.Right);
                    float minY = selectedSliders.Min(s => s.Attributes.Bounds.Top);
                    float width = maxX - minX;  // 使用实际的宽度范围

                    // 设置滑块的位置和大小
                    RectangleF bounds = new RectangleF(
                        minX,  // 使用最左侧的位置
                        minY -50,  // 在最高的滑块上方50个单位
                        width,  // 使用实际的宽度
                        controller.Attributes.Bounds.Height
                    );
                    controller.Attributes.Bounds = bounds;
                    controller.Attributes.Pivot = new PointF(bounds.X, bounds.Y);

                    // 添加到文档
                    canvas.Document.AddObject(controller, false);

                    // 建立控制关系
                    foreach (var slider in selectedSliders)
                    {
                        controller.AddControlledSlider(slider);
                    }

                    // 强制更新
                    controller.ExpireSolution(true);
                    canvas.Refresh();

                    ShowMessage($"Already created union slider ({globalMin}-{globalMax})，controlled {selectedSliders.Count} slider(s)");
                }

                canvas.Document.NewSolution(true);
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 移除所有选中滑块的控制关系
                foreach (var slider in selectedSliders)
                {
                    if (slider.IsControlled)
                    {
                        // 找到控制这个滑块的主控滑块
                        foreach (IGH_DocumentObject obj in canvas.Document.Objects)
                        {
                            if (obj is MotionSlider controller && controller.HasControlledSliders)
                            {
                                controller.RemoveControlledSlider(slider);
                            }
                        }
                        if (slider.IsMainController)
                        {
                            slider.IsMainController = false;
                        }
                    }
                }
                ShowMessage($"Removed {selectedSliders.Count} slider control relationship");
            }

            canvas.Document.NewSolution(true);
        }

        private void ShowMessage(string message)
        {
            var canvas = Instances.ActiveCanvas;
            GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                // 保存当前变换矩阵
                var originalTransform = g.Transform;
                g.ResetTransform();

                // 计算文本大小
                SizeF textSize = new SizeF(30, 30);

                // 设置消息位置
                float padding = 20;
                float x = textSize.Width + 300;
                float y = padding + 30;

                RectangleF textBounds = new RectangleF(x, y, textSize.Width + 300, textSize.Height + 30);
                textBounds.Inflate(6, 3);

                // 绘制消息
                using (var capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Blue,
                    message))
                {
                    capsule.Render(g, Color.LightSkyBlue);
                }

                // 恢复变换
                g.Transform = originalTransform;
            };

            // 添加临时事件处理器
            canvas.CanvasPostPaintObjects += canvasRepaint;
            canvas.Refresh();

            // 设置定时器移除事件处理器
            Timer messageTimer = new Timer();
            messageTimer.Interval = 1500;
            messageTimer.Tick += (sender, e) =>
            {
                canvas.CanvasPostPaintObjects -= canvasRepaint;
                canvas.Refresh();
                messageTimer.Stop();
                messageTimer.Dispose();
            };
            messageTimer.Start();
        }

        private MotionSlider FindExistingController(List<MotionSlider> selectedSliders)
        {
            if (selectedSliders.Count == 0) return null;

            // 遍历所有选中的滑块
            foreach (var slider in selectedSliders)
            {
                // 如果这个滑块是被控制的，找到它的主控滑块
                if (slider.IsControlled)
                {
                    foreach (IGH_DocumentObject obj in Instances.ActiveCanvas.Document.Objects)
                    {
                        if (obj is MotionSlider controller &&
                            controller.IsMainController &&
                            controller.GetControlledSliders().Contains(slider))
                        {
                            return controller;
                        }
                    }
                }
            }

            return null;
        }
    }
}