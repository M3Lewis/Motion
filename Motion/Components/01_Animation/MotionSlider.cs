using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using Motion.General;
using Motion.Toolbar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class MotionSlider : GH_NumberSlider
    {
        private bool _isPositionInitialized = false;

        public event EventHandler<decimal> ValueChanged;

        public override Guid ComponentGuid => new Guid("A6704806-4EE3-42AF-B742-3C348C5F7F38");

        protected override Bitmap Icon => Properties.Resources.MotionSlider;
        public MotionSlider()
        {
            NickName = "Slider";
            Name = "Motion Slider";
            Description = "基础Slider，可被Union Slider控制。请先选择该Slider，然后使用工具栏按钮创建一个Union Slider。";
            Category = "Motion";
            SubCategory = "01_Animation";

            SetInitialValues();
        }

        public MotionSlider(decimal minimum, decimal maximum) : base()
        {
            NickName = "MotionSlider";
            Name = "Motion Slider";
            Description = "基础Slider，可被Union Slider控制。请先选择该Slider，然后使用工具栏按钮创建一个Union Slider。";
            Category = "Motion";
            SubCategory = "01_Animation";

            // 设置滑块属性
            Slider.Type = GH_SliderAccuracy.Integer;
            Slider.TickDisplay = GH_SliderTickDisplay.None;
            Slider.RailDisplay = GH_SliderRailDisplay.Filled;

            // 设置区间范围
            Slider.Minimum = minimum;
            Slider.Maximum = maximum;
            Slider.Value = minimum;
        }

        public override void AddedToDocument(GH_Document document)
        {
            // 检查文档中是否已经存在 MotionSlider
            var existingSliders = document.Objects
                .OfType<MotionSlider>()
                .Where(s => s != this)
                .ToList();

            if (existingSliders.Any())
            {
                // 如果已经存在 MotionSlider，显示消息并取消添加
                var canvas = Grasshopper.Instances.ActiveCanvas;
                if (canvas != null)
                {
                    ShowTemporaryMessage(canvas, "每个文件只能放置一个 Motion Slider!");
                }

                // 延迟执行移除操作，确保消息能够显示
                document.ScheduleSolution(5, doc =>
                {
                    doc.RemoveObject(this, false);
                });

                return;
            }

            base.AddedToDocument(document);

            if (_isPositionInitialized) return;

            // 只有在位置未初始化时才使用鼠标位置
            if (!this.Attributes.Bounds.IsEmpty) return;// 检查是否已经设置了位置

            // 获取当前鼠标位置作为放置位置
            PointF mouseLoc = Instances.ActiveCanvas.CursorCanvasPosition;
            if (mouseLoc.X == 0 || mouseLoc.Y == 0) return; // 确保有有效的鼠标位置

            this.Attributes.Pivot = mouseLoc;

            // 如果需要，也可以更新 Bounds
            RectangleF bounds = this.Attributes.Bounds;
            bounds.Location = mouseLoc;
            this.Attributes.Bounds = bounds;
            _isPositionInitialized = true;
        }

        // 添加显示临时消息的方法
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
        public void UpdateRangeBasedOnSenders()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 获取所有连接到这个 Slider 的 MotionSender
            var connectedSenders = doc.Objects
                .OfType<MotionSender>()
                .Where(sender => sender.Sources.Any(source => source.InstanceGuid == this.InstanceGuid))
                .ToList();

            if (!connectedSenders.Any()) return;

            // 计算所有连接的 Sender 的最小和最大区间
            decimal minValue = decimal.MaxValue;
            decimal maxValue = decimal.MinValue;

            foreach (var sender in connectedSenders)
            {
                if (TryParseRange(sender.NickName, out decimal min, out decimal max))
                {
                    minValue = Math.Min(minValue, min);
                    maxValue = Math.Max(maxValue, max);
                }
            }

            // 更新 Slider 的区间
            if (minValue != decimal.MaxValue && maxValue != decimal.MinValue)
            {
                Slider.Minimum = minValue;
                Slider.Maximum = maxValue;
                ExpireSolution(true);
            }
        }

        private bool TryParseRange(string nickname, out decimal min, out decimal max)
        {
            min = 0;
            max = 0;

            if (string.IsNullOrEmpty(nickname))
                return false;

            var parts = nickname.Split('-');
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0], out min) &&
                decimal.TryParse(parts[1], out max))
            {
                return true;
            }

            return false;
        }
        // 处理新滑块创建的事件

        private void SetInitialValues()
        {
            Slider.Minimum = 0m;
            Slider.Maximum = 100m;
            Slider.Value = 0m;
            Slider.DecimalPlaces = 0;
            Slider.Type = GH_SliderAccuracy.Integer;
        }

        public override void CreateAttributes()
        {
            m_attributes = new MotionSliderAttributes(this);
        }
        // 新增方法：同步 MotionSender 的区间
        public void SynchronizeSenderIntervals()
        {
            var doc = Instances.ActiveCanvas.Document;
            if (doc == null) return;

            // 找到所有连接到当前 Slider 的 MotionSender
            var connectedSenders = doc.Objects
                .OfType<MotionSender>()
                .Where(sender => sender.Sources.Any(source => source.InstanceGuid == this.InstanceGuid))
                .ToList();

            if (!connectedSenders.Any()) return;

            // 计算所有连接的 Sender 的最小和最大区间
            decimal minInterval = connectedSenders.Min(sender =>
                decimal.Parse(sender.NickName.Split('-')[0]));
            decimal maxInterval = connectedSenders.Max(sender =>
                decimal.Parse(sender.NickName.Split('-')[1]));

            // 更新 Slider 的区间
            if (minInterval < maxInterval)
            {
                Slider.Minimum = minInterval;
                Slider.Maximum = maxInterval;

                // 触发解决方案更新
                ExpireSolution(true);
            }
        }
        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            return true;
        }

        protected override void ValuesChanged()
        {
            base.ValuesChanged();
            SynchronizeSenderIntervals();
        }
    }

    public class MotionSliderAttributes : GH_ResizableAttributes<GH_NumberSlider>
    {
        private readonly new MotionSlider Owner;
        private bool _isDraggingSlider = false;
        private int _dragMode = 0;
        protected override Size MinimumSize => new Size(
            (int)(TEXT_BOX_WIDTH + 100),  // 文本框宽度 + 最小滑块宽度
            20
        );
        protected override Size MaximumSize => new Size(5000, 20);
        protected override Padding SizingBorders => new Padding(6, 0, 6, 0);

        // 修改文本框相关的字段
        private RectangleF _rangeTextBox;  // 只保留一个文本框
        public const float TEXT_BOX_WIDTH = 80;  // 增加宽度以容纳更多文本
        public const float TEXT_BOX_HEIGHT = 20;

        public MotionSliderAttributes(MotionSlider owner) : base(owner)
        {
            Owner = owner;
        }

        public override bool HasInputGrip
        {
            get { return false; }
        }

        protected override void Layout()
        {
            // 计算名称区域大小
            SizeF sizeF = new SizeF(2, 20);

            // 设置整体边界，确保最小宽度
            float minWidth = TEXT_BOX_WIDTH + 100;  // 文本框宽度 + 最小滑块宽度
            Bounds = new RectangleF(
                Pivot.X,
                Pivot.Y,
                Math.Max(Math.Max(Bounds.Width, sizeF.Width), minWidth),
                MinimumSize.Height
            );
            Bounds = GH_Convert.ToRectangle(Bounds);

            // 设置文本框区域 - 放在最左侧
            _rangeTextBox = new RectangleF(
                Pivot.X,
                Pivot.Y,
                TEXT_BOX_WIDTH,
                MinimumSize.Height
            );

            // 设置名称区域 - 在文本框右侧
            Rectangle boundsName = GH_Convert.ToRectangle(new RectangleF(
                _rangeTextBox.Right + 5,
                Pivot.Y,
                sizeF.Width,
                MinimumSize.Height
            ));

            // 设置滑块区域 - 在名称区域右侧，确保最小宽度
            Rectangle boundsSlider = Rectangle.FromLTRB(
                boundsName.Right,
                boundsName.Top,
                Math.Max(Convert.ToInt32(Bounds.Right), boundsName.Right + 50),  // 确保滑块最小宽度为50
                boundsName.Bottom
            );

            // 设置滑块属性
            base.Owner.Slider.Font = GH_FontServer.StandardAdjusted;
            base.Owner.Slider.DrawControlBorder = false;
            base.Owner.Slider.DrawControlShadows = false;
            base.Owner.Slider.DrawControlBackground = false;
            base.Owner.Slider.TickCount = 11;
            base.Owner.Slider.TickFrequency = 5;
            base.Owner.Slider.RailDarkColour = Color.FromArgb(255, Color.LightSkyBlue);
            base.Owner.Slider.RailEmptyColour = Color.FromArgb(255, Color.LightSkyBlue);
            base.Owner.Slider.RailBrightColour = Color.FromArgb(255, Color.LightSkyBlue);
            base.Owner.Slider.RailFullColour = Color.FromArgb(255, Color.White);
            base.Owner.Slider.TickDisplay = GH_SliderTickDisplay.None;
            base.Owner.Slider.RailDisplay = GH_SliderRailDisplay.Etched;
            base.Owner.Slider.GripDisplay = GH_SliderGripDisplay.ShapeAndText;
            base.Owner.Slider.TextColour = Color.FromArgb(255, 40, 40, 40);
            base.Owner.Slider.Padding = new Padding(6, 2, 6, 1);
            base.Owner.Slider.Bounds = boundsSlider;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel != GH_CanvasChannel.Objects)
                return;

            // 检查名称是否变化
            string impliedNickName = Owner.ImpliedNickName;
            if (impliedNickName != Owner.NickName)
            {
                ExpireLayout();
                Layout();
            }

            // 检查可见性
            GH_Viewport viewport = canvas.Viewport;
            RectangleF rec = Bounds;
            if (!viewport.IsVisible(ref rec, 10f))
                return;

            // 绘制滑块区域
            int[] radii2 = new int[4] { 3, 3, 3, 3 };

            using (GH_Capsule sliderCapsule = GH_Capsule.CreateCapsule(
                Owner.Slider.Bounds,
                GH_Palette.Normal,
                radii2,
                5))
            {
                sliderCapsule.AddOutputGrip(OutputGrip.Y);
                sliderCapsule.Render(graphics, Selected, Owner.Locked, false);
            }

            // 绘制滑块
            Owner.Slider.Render(graphics);
            // 如果处于秒数输入模式，绘制秒数文本
            if (MotionSliderSettings.IsSecondsInputMode())
            {
                // 计算秒数文本的位置（在区间文本框左侧）
                var secondsTextBounds = new RectangleF(
                    _rangeTextBox.X - 100,  // 在区间文本框左侧50像素
                    _rangeTextBox.Y,
                    95,  // 文本宽度
                    _rangeTextBox.Height
                );

                // 计算秒数
                double minSeconds = (double)Owner.Slider.Minimum / MotionSliderSettings.FramesPerSecond;
                double maxSeconds = (double)Owner.Slider.Maximum / MotionSliderSettings.FramesPerSecond;
                string secondsText = $"{minSeconds:F1}s-{maxSeconds:F1}s";

                // 直接绘制秒数文本
                graphics.DrawString(
                    secondsText,
                    GH_FontServer.Standard,
                    Brushes.White,
                    secondsTextBounds,
                    new StringFormat()
                    {
                        Alignment = StringAlignment.Far,  // 右对齐，这样文本会靠近区间文本框
                        LineAlignment = StringAlignment.Center
                    }
                );
            }
            // 渲染区间文本框
            using (var capsule = GH_Capsule.CreateTextCapsule(
                Rectangle.Round(_rangeTextBox),
                Rectangle.Round(_rangeTextBox),
                GH_Palette.Black,
                $"{Owner.Slider.Minimum}-{Owner.Slider.Maximum}",  // 显示当前区间
                6,  // 边框粗细
                2))  // 文本对齐方式
            {
                capsule.Render(graphics, Selected, Owner.Locked, true);
            }
        }

        public override void ExpireLayout()
        {
            base.ExpireLayout();
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            switch (_dragMode)
            {
                case 1: // 滑块拖动开始
                    Owner.RecordUndoEvent("Slider change");
                    _dragMode = 2;
                    goto case 2;

                case 2: // 滑块正在拖动
                    Owner.Slider.MouseMove(e.WinFormsEventArgs, e.CanvasLocation);
                    using (var region = new Region(Owner.Slider.Bounds))
                    {
                        sender.Invalidate(region);
                    }
                    return GH_ObjectResponse.Handled;
            }

            // 只有在非拖动状态下才调用基类方法
            if (_dragMode == 0)
            {
                return base.RespondToMouseMove(sender, e);
            }

            return GH_ObjectResponse.Ignore;
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner.Slider.MouseDown(e.WinFormsEventArgs, e.CanvasLocation))
            {
                _dragMode = 1;
                _isDraggingSlider = true;
                sender.Invalidate();

                // 计算滑块的捕捉距离
                var rail = Owner.Slider.Rail;
                decimal railWidth = new decimal(rail.Right - rail.Left);
                decimal zoomFactor = Convert.ToDecimal(sender.Viewport.Zoom);
                decimal range = Owner.Slider.Maximum - Owner.Slider.Minimum;
                decimal snapDistance = (range / (railWidth * zoomFactor)) * 10m;
                Owner.Slider.SnapDistance = snapDistance;

                return GH_ObjectResponse.Capture;
            }

            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (_dragMode > 0)
            {
                if (_dragMode == 2)
                {
                    Owner.Slider.MouseUp(e.WinFormsEventArgs, e.CanvasLocation);
                }
                _dragMode = 0;
                _isDraggingSlider = false;
                sender.Invalidate();
                return GH_ObjectResponse.Release;
            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF pt = e.CanvasLocation;

                // 检查是否点击了区间文本框
                if (_rangeTextBox.Contains(pt))
                {
                    string content = $"{Owner.Slider.Minimum}-{Owner.Slider.Maximum}";
                    Owner.Slider.TextInputHandlerDelegate = (slider, text) =>
                    {
                        // 解析输入的区间文本
                        string[] parts = text.Split('-');
                        if (parts.Length == 2 &&
                            decimal.TryParse(parts[0], out decimal min) &&
                            decimal.TryParse(parts[1], out decimal max))
                        {
                            if (min < max)
                            {
                                Owner.Slider.Minimum = min;
                                Owner.Slider.Maximum = max;
                                Owner.ExpireSolution(true);

                                // 取消选中状态
                                Owner.OnPingDocument()?.DeselectAll();
                                Instances.ActiveCanvas?.Refresh();
                            }
                        }
                    };

                    // 临时保存原始位置
                    var originalBounds = Owner.Slider.Bounds;

                    // 临时将滑块位置设置为文本框位置
                    Owner.Slider.Bounds = Rectangle.Round(_rangeTextBox);

                    Owner.Slider.ShowTextInputBox(
                        sender,
                        true,
                        sender.Viewport.XFormMatrix(GH_Viewport.GH_DisplayMatrix.CanvasToControl),
                        content
                    );

                    // 恢复原始位置
                    Owner.Slider.Bounds = originalBounds;

                    return GH_ObjectResponse.Handled;
                }

                // 如果不是文本框，检查是否点击了滑块
                if ((double)sender.Viewport.Zoom >= 0.9 && Owner.Slider.Bounds.Contains(GH_Convert.ToPoint(e.CanvasLocation)))
                {
                    string content = base.Owner.Slider.GripTextPure;
                    base.Owner.Slider.TextInputHandlerDelegate = TextInputHandler;
                    base.Owner.Slider.ShowTextInputBox(
                        sender,
                        true,
                        sender.Viewport.XFormMatrix(GH_Viewport.GH_DisplayMatrix.CanvasToControl),
                        content
                    );
                    return GH_ObjectResponse.Handled;
                }
                else
                {
                    base.Owner.PopupEditor();
                    return GH_ObjectResponse.Handled;
                }
            }

            return GH_ObjectResponse.Ignore;
        }

        private void TextInputHandler(GH_SliderBase slider, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                if (GH_Convert.ToDouble(text, out var destination, GH_Conversion.Secondary))
                {
                    Owner.RecordUndoEvent("Slider Value Change");
                    Owner.TrySetSliderValue(Convert.ToDecimal(destination));
                    Owner.SynchronizeSenderIntervals(); // 确保这里调用了 SynchronizeSenderIntervals
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}