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
        private bool _isPlaying = false;
        private bool _isLooping = false;
        private System.Windows.Forms.Timer _playbackTimer;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    if (_isPlaying)
                    {
                        StartPlaybackTimer();
                    }
                    else
                    {
                        StopPlaybackTimer();
                    }
                }
            }
        }

        public bool IsLooping
        {
            get => _isLooping;
            set => _isLooping = value;
        }

        public override Guid ComponentGuid => new Guid("A6704806-4EE3-42AF-B742-3C348C5F7F38");

        protected override Bitmap Icon => Properties.Resources.MotionSlider;
        public MotionSlider()
        {
            // NickName will be set by UpdateNickNameBasedOnRange
            Name = "Motion Slider";
            Description = "基础Slider，可被Union Slider控制。请先选择该Slider，然后使用工具栏按钮创建一个Union Slider。";
            Category = "Motion";
            SubCategory = "01_Animation";

            SetInitialValues();
            // UpdateNickNameBasedOnRange(); // Called within SetInitialValues
        }

        public MotionSlider(decimal minimum, decimal maximum) : base()
        {
            // NickName will be set by UpdateNickNameBasedOnRange
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

            UpdateNickNameBasedOnRange(); // Set initial NickName based on range
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
                    MotilityUtils.ShowTemporaryMessageAtLocation(canvas, General.LanguageManager.GetString("Msg.SingleSliderOnly", "每个文件只能放置一个 Motion Slider!"),
                        new PointF(this.Attributes.Bounds.Right + 20, this.Attributes.Bounds.Top + 4));
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

            // 自动连接到所有时间输入端
            ConnectToTimeInputs();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            // 添加分隔线
            menu.Items.Add(new ToolStripSeparator());

            // 添加自动连接选项
            ToolStripMenuItem connectItem = new ToolStripMenuItem(
                LanguageManager.GetString("Menu.ConnectToAllTimeInputs", "连接到所有时间输入端"),
                null,
                (sender, e) => ConnectToTimeInputs()
            );
            menu.Items.Add(connectItem);
        }

        public void ConnectToTimeInputs()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            int connectionCount = 0;

            // 连接到所有 EventOperation 的 Time 输入端
            foreach (var eventOp in doc.Objects.OfType<EventOperation>())
            {
                var timeParam = eventOp.Params.Input.FirstOrDefault(p => p.Name == "Time");
                if (timeParam != null && timeParam.SourceCount == 0)
                {
                    timeParam.AddSource(this);
                    timeParam.WireDisplay = GH_ParamWireDisplay.hidden;  // 设置连线为隐藏
                    connectionCount++;
                }
            }

            // 连接到所有 IntervalLock 的第一个输入端
            foreach (var intervalLock in doc.Objects.OfType<IntervalLock>())
            {
                var firstInput = intervalLock.Params.Input.FirstOrDefault();
                if (firstInput != null && firstInput.SourceCount == 0)
                {
                    firstInput.AddSource(this);
                    firstInput.WireDisplay = GH_ParamWireDisplay.hidden;  // 设置连线为隐藏
                    connectionCount++;
                }
            }

            if (connectionCount > 0)
            {
                doc.NewSolution(true);
            }
        }

        

        internal void UpdateRangeFromConnectedSenders()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var connectedSenders = doc.Objects
                .OfType<MotionSender>()
                .Where(sender => sender.Sources.Any(source => source.InstanceGuid == this.InstanceGuid))
                .ToList();

            if (!connectedSenders.Any()) return;

            decimal minValue = decimal.MaxValue;
            decimal maxValue = decimal.MinValue;
            bool hasValidInterval = false;

            foreach (var sender in connectedSenders)
            {
                if (MotilityUtils.TryParseNickNameInterval(sender.NickName, out double min, out double max))
                {
                    minValue = Math.Min(minValue, (decimal)min);
                    maxValue = Math.Max(maxValue, (decimal)max);
                    hasValidInterval = true;
                }
            }

            if (hasValidInterval && minValue < maxValue)
            {
                Slider.Minimum = minValue;
                Slider.Maximum = maxValue;
                UpdateNickNameBasedOnRange();
                ExpireSolution(true);
            }
        }
        
        // 处理新滑块创建的事件

        private void SetInitialValues()
        {
            Slider.Minimum = 0m;
            Slider.Maximum = 100m;
            Slider.Value = 0m;
            Slider.DecimalPlaces = 0;
            Slider.Type = GH_SliderAccuracy.Integer;
            UpdateNickNameBasedOnRange(); // Update NickName after setting initial values
        }

        public override void CreateAttributes()
        {
            m_attributes = new MotionSliderAttributes(this);
        }
        // 新增方法：同步 MotionSender 的区间
        
        private void StartPlaybackTimer()
        {
            if (_playbackTimer == null)
            {
                _playbackTimer = new System.Windows.Forms.Timer();
                _playbackTimer.Tick += PlaybackTimer_Tick;
            }
            int fps = MotionSenderSettings.FramesPerSecond;
            if (fps <= 0) fps = 60;
            _playbackTimer.Interval = Math.Max(1, 1000 / fps);
            _playbackTimer.Start();
        }

        private void StopPlaybackTimer()
        {
            if (_playbackTimer != null)
            {
                _playbackTimer.Stop();
                _playbackTimer.Tick -= PlaybackTimer_Tick;
                _playbackTimer.Dispose();
                _playbackTimer = null;
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            var doc = OnPingDocument();
            if (doc == null || doc.SolutionState == GH_ProcessStep.Process)
            {
                return;
            }

            if (Locked)
            {
                IsPlaying = false;
                Instances.ActiveCanvas?.Invalidate();
                return;
            }

            decimal current = Slider.Value;
            decimal min = Slider.Minimum;
            decimal max = Slider.Maximum;

            decimal nextValue = current + 1;
            if (current >= max)
            {
                if (!IsLooping)
                {
                    IsPlaying = false;
                    Instances.ActiveCanvas?.Invalidate();
                    return;
                }
                nextValue = min;
            }

            doc.ScheduleSolution(1, d =>
            {
                if (!IsPlaying) return;
                TrySetSliderValue(nextValue);
                ExpireSolution(false);
            });
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            IsPlaying = false;
            base.RemovedFromDocument(document);
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;
            writer.SetBoolean("IsLooping", IsLooping);
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;
            if (reader.ItemExists("IsLooping"))
            {
                IsLooping = reader.GetBoolean("IsLooping");
            }
            return true;
        }

        protected override void ValuesChanged()
        {
            base.ValuesChanged();
            UpdateRangeFromConnectedSenders();
        }

        internal void UpdateNickNameBasedOnRange()
        {
            NickName = $"{Slider.Minimum}-{Slider.Maximum}";
            // Optionally trigger a redraw or layout update if needed visually
            // Attributes?.ExpireLayout(); // Might be needed if NickName affects layout significantly
        }
    }

    public class MotionSliderAttributes : GH_ResizableAttributes<GH_NumberSlider>
    {
        private readonly new MotionSlider Owner;
        private int _dragMode = 0;
        protected override Size MinimumSize => new Size(
            (int)(TEXT_BOX_WIDTH + 100),  // 文本框宽度 + 最小滑块宽度
            42
        );
        protected override Size MaximumSize => new Size(5000, 42);
        protected override Padding SizingBorders => new Padding(6, 0, 6, 0);

        // 修改文本框相关的字段
        private RectangleF _rangeTextBox;  // 只保留一个文本框
        private RectangleF _playButtonRect;
        private RectangleF _pauseButtonRect;
        private RectangleF _loopButtonRect;
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

        public override PointF OutputGrip
        {
            get
            {
                return new PointF(Bounds.Right, Pivot.Y + 10f);
            }
        }

        private void UpdateButtonRects()
        {
            RectangleF sliderBounds = Owner.Slider.Bounds;
            float totalWidth = sliderBounds.Width;
            float gap = 4f; 
            float btnWidth = (totalWidth - 2 * gap) / 3f;
            float btnHeight = 16f;
            float btnY = sliderBounds.Bottom + 5f; // boundsSlider.Bottom is Pivot.Y + 20

            _playButtonRect = new RectangleF(sliderBounds.Left, btnY, btnWidth, btnHeight);
            _pauseButtonRect = new RectangleF(sliderBounds.Left + btnWidth + gap, btnY, btnWidth, btnHeight);
            _loopButtonRect = new RectangleF(sliderBounds.Left + 2 * (btnWidth + gap), btnY, btnWidth, btnHeight);
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
                20
            );

            // 设置名称区域 - 在文本框右侧
            Rectangle boundsName = GH_Convert.ToRectangle(new RectangleF(
                _rangeTextBox.Right + 5,
                Pivot.Y,
                sizeF.Width,
                20
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

            UpdateButtonRects();
        }

        private void DrawRoundedRect(Graphics g, RectangleF rect, Color fillColor, Color strokeColor, float strokeWidth, int radius)
        {
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float r = radius * 2f;
                if (r > rect.Width) r = rect.Width;
                if (r > rect.Height) r = rect.Height;

                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();

                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    g.FillPath(brush, path);
                }

                if (strokeWidth > 0)
                {
                    using (Pen pen = new Pen(strokeColor, strokeWidth))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
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

            UpdateButtonRects();

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
                sliderCapsule.AddOutputGrip(Pivot.Y + 10f);
                sliderCapsule.Render(graphics, Selected, Owner.Locked, false);
            }

            // 绘制滑块
            Owner.Slider.Render(graphics);

            // 绘制 Play 按钮
            Color playBg = Owner.IsPlaying ? Color.FromArgb(255, 46, 204, 113) : Color.FromArgb(255, 45, 45, 45);
            DrawRoundedRect(graphics, _playButtonRect, playBg, Color.FromArgb(255, 80, 80, 80), 1f, 3);
            
            float p_cx = _playButtonRect.X + _playButtonRect.Width / 2f;
            float p_cy = _playButtonRect.Y + _playButtonRect.Height / 2f;
            PointF[] p_pts = new PointF[]
            {
                new PointF(p_cx - 3, p_cy - 5),
                new PointF(p_cx - 3, p_cy + 5),
                new PointF(p_cx + 5, p_cy)
            };
            graphics.FillPolygon(Brushes.White, p_pts);

            // 绘制 Pause 按钮
            Color pauseBg = !Owner.IsPlaying ? Color.FromArgb(255, 231, 76, 60) : Color.FromArgb(255, 45, 45, 45);
            DrawRoundedRect(graphics, _pauseButtonRect, pauseBg, Color.FromArgb(255, 80, 80, 80), 1f, 3);
            
            float pa_cx = _pauseButtonRect.X + _pauseButtonRect.Width / 2f;
            float pa_cy = _pauseButtonRect.Y + _pauseButtonRect.Height / 2f;
            graphics.FillRectangle(Brushes.White, pa_cx - 3, pa_cy - 5, 2, 10);
            graphics.FillRectangle(Brushes.White, pa_cx + 1, pa_cy - 5, 2, 10);

            // 绘制 Loop 按钮 (醒目颜色 43, 214, 255)
            Color loopBg = Owner.IsLooping ? Color.FromArgb(255, 43, 214, 255) : Color.FromArgb(255, 45, 45, 45);
            DrawRoundedRect(graphics, _loopButtonRect, loopBg, Color.FromArgb(255, 80, 80, 80), 1f, 3);
            
            float l_cx = _loopButtonRect.X + _loopButtonRect.Width / 2f;
            float l_cy = _loopButtonRect.Y + _loopButtonRect.Height / 2f;
            Color loopIconColor = Owner.IsLooping ? Color.Black : Color.FromArgb(200, 200, 200);
            using (Pen pen = new Pen(loopIconColor, 1.5f))
            {
                graphics.DrawLine(pen, l_cx - 5, l_cy - 2, l_cx + 5, l_cy - 2);
                graphics.DrawLine(pen, l_cx + 5, l_cy - 2, l_cx + 2, l_cy - 5);

                graphics.DrawLine(pen, l_cx + 5, l_cy + 2, l_cx - 5, l_cy + 2);
                graphics.DrawLine(pen, l_cx - 5, l_cy + 2, l_cx - 2, l_cy + 5);
            }

            // 如果处于秒数输入模式，绘制秒数文本
            if (MotionSenderSettings.IsSecondsInputMode())
            {
                // 计算秒数文本的位置（在区间文本框左侧）
                var secondsTextBounds = new RectangleF(
                    _rangeTextBox.X - 100,  // 在区间文本框左侧
                    _rangeTextBox.Y,
                    95,  // 文本宽度
                    _rangeTextBox.Height
                );

                // 计算秒数
                double minSeconds = (double)Owner.Slider.Minimum / MotionSenderSettings.FramesPerSecond;
                double maxSeconds = (double)Owner.Slider.Maximum / MotionSenderSettings.FramesPerSecond;
                string secondsText = $"{minSeconds:F1}s-{maxSeconds:F1}s";

                // 直接绘制秒数文本
                graphics.DrawString(
                    secondsText,
                    GH_FontServer.Standard,
                    Brushes.White,
                    secondsTextBounds,
                    new StringFormat()
                    {
                        Alignment = StringAlignment.Far,  // 右对齐
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
            UpdateButtonRects();
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
            UpdateButtonRects();
            if (e.Button == MouseButtons.Left)
            {
                if (_playButtonRect.Contains(e.CanvasLocation))
                {
                    Owner.IsPlaying = true;
                    sender.Invalidate();
                    return GH_ObjectResponse.Handled;
                }
                if (_pauseButtonRect.Contains(e.CanvasLocation))
                {
                    Owner.IsPlaying = false;
                    sender.Invalidate();
                    return GH_ObjectResponse.Handled;
                }
                if (_loopButtonRect.Contains(e.CanvasLocation))
                {
                    Owner.IsLooping = !Owner.IsLooping;
                    sender.Invalidate();
                    return GH_ObjectResponse.Handled;
                }
            }

            if (Owner.Slider.MouseDown(e.WinFormsEventArgs, e.CanvasLocation))
            {
                Owner.IsPlaying = false; // Pause playback if user manually drags the slider
                _dragMode = 1;
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
            UpdateButtonRects();
            if (_dragMode > 0)
            {
                if (_dragMode == 2)
                {
                    Owner.Slider.MouseUp(e.WinFormsEventArgs, e.CanvasLocation);
                }
                _dragMode = 0;
                sender.Invalidate();
                return GH_ObjectResponse.Release;
            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            UpdateButtonRects();
            if (e.Button == MouseButtons.Left)
            {
                PointF pt = e.CanvasLocation;

                // 检查是否点击了播放、暂停、循环按钮，若是，则不响应双击或拦截为单击行为
                if (_playButtonRect.Contains(pt) || _pauseButtonRect.Contains(pt) || _loopButtonRect.Contains(pt))
                {
                    return RespondToMouseDown(sender, e);
                }

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
                    Owner.UpdateRangeFromConnectedSenders(); // 确保这里调用了 SynchronizeSenderIntervals
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error processing text input for slider: {ex.Message}");
            }
        }
    }
}