using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
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
        // 添加静态事件来通知新滑块创建
        public static event EventHandler<MotionSlider> SliderCreated;

        // 添加静态字段来跟踪主控滑块
        private static MotionSlider _mainController;
        private bool _isPositionInitialized = false;
        public List<MotionSlider> _controlledSliders = new List<MotionSlider>();
        public List<Guid> _controlledSliderGuids = new List<Guid>();
        public bool _isControlled;
        public Guid _controllerGuid = Guid.Empty;
        private decimal _lastValue;
        public bool _isRefreshing = false;

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

            // 触发新建事件
            SliderCreated?.Invoke(this, this);

            // 添加值变化事件处理
            this.Slider.ValueChanged += Slider_ValueChanged;
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

            _lastValue = minimum;

            // 触发新建事件
            SliderCreated?.Invoke(this, this);

            // 添加值变化事件处理
            this.Slider.ValueChanged += Slider_ValueChanged;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            if (!_isPositionInitialized)
            {
                // 只有在位置未初始化时才使用鼠标位置
                if (this.Attributes.Bounds.IsEmpty)  // 检查是否已经设置了位置
                {
                    // 获取当前鼠标位置作为放置位置
                    PointF mouseLoc = Instances.ActiveCanvas.CursorCanvasPosition;
                    if (mouseLoc.X != 0 || mouseLoc.Y != 0)  // 确保有有效的鼠标位置
                    {
                        this.Attributes.Pivot = mouseLoc;

                        // 如果需要，也可以更新 Bounds
                        RectangleF bounds = this.Attributes.Bounds;
                        bounds.Location = mouseLoc;
                        this.Attributes.Bounds = bounds;
                    }
                }
                _isPositionInitialized = true;
            }

            // 如果有保存的控制关系，尝试立即恢复
            if (_isControlled || _controlledSliderGuids.Count > 0)
            {
                OnPingDocument().ScheduleSolution(50, doc =>
                {
                    RestoreControlRelationships();
                    //Rhino.RhinoApp.WriteLine("Restored Relationship"+DateTime.Now.ToString());
                });
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (document != null)
            {
                document.ObjectsDeleted -= Document_ObjectsDeleted;
            }

            if (IsMainController)
            {
                IsMainController = false;
            }

            // 清理事件订阅
            if (Slider != null)
            {
                Slider.ValueChanged -= Slider_ValueChanged;
            }

            base.RemovedFromDocument(document);
        }

        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this) && IsMainController)
            {
                IsMainController = false;
            }
        }

        // 添加属性来标识是否是主控滑块
        public bool IsMainController
        {
            get => this == _mainController;
            set
            {
                if (value)
                {
                    _mainController = this;
                    // 注册新滑块创建事件处理
                    SliderCreated += HandleNewSliderCreated;
                }
                else if (this == _mainController)
                {
                    _mainController = null;
                    // 取消注册事件处理
                    SliderCreated -= HandleNewSliderCreated;
                }
            }
        }

        // 处理新滑块创建的事件
        private void HandleNewSliderCreated(object sender, MotionSlider newSlider)
        {
            if (sender == this) return; // 忽略自己的创建事件

            bool needsUpdate = false;
            decimal currentMin = Slider.Minimum;
            decimal currentMax = Slider.Maximum;

            // 分别检查最小值和最大值
            decimal newMin = Math.Min(currentMin, newSlider.Slider.Minimum);
            if (newMin != currentMin)
            {
                Slider.Minimum = newMin;
                needsUpdate = true;
            }

            decimal newMax = Math.Max(currentMax, newSlider.Slider.Maximum);
            if (newMax != currentMax)
            {
                Slider.Maximum = newMax;
                needsUpdate = true;
            }

            // 只有在值真正发生变化时才更新解决方案
            if (needsUpdate && OnPingDocument() != null)
            {
                OnPingDocument().NewSolution(false);
            }
        }

        private void SetInitialValues()
        {
            Slider.Minimum = 0m;
            Slider.Maximum = 100m;
            Slider.Value = 0m;
            Slider.DecimalPlaces = 0;
            Slider.Type = GH_SliderAccuracy.Integer;
            _lastValue = CurrentValue;
        }

        public override void CreateAttributes()
        {
            m_attributes = new MotionSliderAttributes(this);
        }

        public override void ExpireSolution(bool recompute)
        {
            if (!_isRefreshing && CurrentValue != _lastValue)
            {
                try
                {
                    _isRefreshing = true;

                    if (_controlledSliders.Count > 0)
                    {
                        decimal currentValue = CurrentValue;
                        foreach (var slider in _controlledSliders)
                        {
                            if (currentValue >= slider.Slider.Minimum &&
                                currentValue <= slider.Slider.Maximum)
                            {
                                slider.UpdateValue(currentValue);
                            }
                        }
                    }

                    ValueChanged?.Invoke(this, CurrentValue);
                    _lastValue = CurrentValue;
                }
                finally
                {
                    _isRefreshing = false;
                }
            }
            base.ExpireSolution(recompute);
        }

        public bool IsControlled
        {
            get => _isControlled;
            set => _isControlled = value;
        }

        public void UpdateValue(decimal unionValue)
        {
            if (!_isRefreshing && _isControlled &&
                unionValue >= Slider.Minimum &&
                unionValue <= Slider.Maximum)
            {
                SetSliderValue(unionValue);
            }
        }

        /// <summary>
        /// 添加要控制的滑块
        /// </summary>
        public virtual void AddControlledSlider(MotionSlider slider)
        {
            if (!_controlledSliders.Contains(slider))
            {
                _controlledSliders.Add(slider);
                slider._isControlled = true;
                slider._controllerGuid = this.InstanceGuid;

                // 确保GUID也被添加到列表中
                if (!_controlledSliderGuids.Contains(slider.InstanceGuid))
                {
                    _controlledSliderGuids.Add(slider.InstanceGuid);
                }

                // 添加调试输出
                //Rhino.RhinoApp.WriteLine($"Added controlled slider: {slider.InstanceGuid}");
                //Rhino.RhinoApp.WriteLine($"Current controlled sliders count: {_controlledSliderGuids.Count}");
            }
        }

        /// <summary>
        /// 移除被控制的滑块
        /// </summary>
        public virtual void RemoveControlledSlider(MotionSlider slider)
        {
            _controlledSliders.Remove(slider);
            _controlledSliderGuids.Remove(slider.InstanceGuid);
            slider._isControlled = false;
            slider._controllerGuid = Guid.Empty;

            // 添加调试输出
            //Rhino.RhinoApp.WriteLine($"Removed controlled slider: {slider.InstanceGuid}");
            //Rhino.RhinoApp.WriteLine($"Current controlled sliders count: {_controlledSliderGuids.Count}");
        }

        /// <summary>
        /// 获取是否有被控制的滑块
        /// </summary>
        public bool HasControlledSliders => _controlledSliders.Count > 0;

        /// <summary>
        /// 获取所有被控制的滑块
        /// </summary>
        public List<MotionSlider> GetControlledSliders()
        {
            return new List<MotionSlider>(_controlledSliders);
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                // 添加调试输出
                //Rhino.RhinoApp.WriteLine($"Writing controlled sliders. Count: {_controlledSliderGuids.Count}");

                writer.SetDrawingPoint("CanvasPosition", new Point(
                    (int)Attributes.Pivot.X,
                    (int)Attributes.Pivot.Y
                ));

                writer.SetBoolean("IsControlled", _isControlled);
                writer.SetBoolean("IsMainController", IsMainController);
                writer.SetString("ControllerGuid", _controllerGuid.ToString());

                // 确保在写入之前同步两个列表
                _controlledSliderGuids = _controlledSliders.Select(s => s.InstanceGuid).ToList();

                writer.SetInt32("ControlledSlidersCount", _controlledSliderGuids.Count);
                for (int i = 0; i < _controlledSliderGuids.Count; i++)
                {
                    writer.SetString($"ControlledSlider_{i}", _controlledSliderGuids[i].ToString());
                    // 添加调试输出
                    //Rhino.RhinoApp.WriteLine($"Writing controlled slider {i}: {_controlledSliderGuids[i]}");
                }

                return true;
            }
            catch (Exception ex)
            {
                //Rhino.RhinoApp.WriteLine($"Error writing slider data: {ex.Message}");
                return false;
            }
        }

        public override bool Read(GH_IReader reader)
        {
            try
            {
                // 读取滑块在画布中的位置
                if (reader.ItemExists("CanvasPosition"))
                {
                    Point position = reader.GetDrawingPoint("CanvasPosition");
                    Attributes.Pivot = new PointF(position.X, position.Y);
                    _isPositionInitialized = true;
                }

                // 读取控制关系
                _isControlled = reader.GetBoolean("IsControlled");
                bool isMainController = reader.GetBoolean("IsMainController");
                string controllerGuidStr = reader.GetString("ControllerGuid");
                _controllerGuid = string.IsNullOrEmpty(controllerGuidStr) ?
                    Guid.Empty : new Guid(controllerGuidStr);

                // 读取受控滑块的GUID列表
                _controlledSliderGuids.Clear();
                _controlledSliders.Clear();

                int count = reader.GetInt32("ControlledSlidersCount");

                for (int i = 0; i < count; i++)
                {
                    string guidStr = reader.GetString($"ControlledSlider_{i}");
                    if (Guid.TryParse(guidStr, out Guid guid))
                    {
                        _controlledSliderGuids.Add(guid);
                    }
                }
                // 设置一个延迟执行的恢复
                GH_Document doc = this.OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(30, d => RestoreControlRelationships());
                }

                // 调用基类的 Read 方法，但不要立即返回
                base.Read(reader);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private void RestoreControlRelationships()
        {
            // 确保只执行一次
            var doc = OnPingDocument();
            if (doc == null)
            {
                return;
            }

            // 移除事件处理器
            doc.SolutionEnd -= (sender, e) => RestoreControlRelationships();

            // 如果这个滑块被控制，找到它的控制者并建立关系
            if (_isControlled && _controllerGuid != Guid.Empty)
            {
                var controller = doc.FindObject(_controllerGuid, true) as MotionUnionSlider;
                if (controller != null)
                {
                    controller.AddControlledSlider(this);
                    // 确保立即更新值
                    controller.ExpireSolution(true);
                }
            }

            // 如果这个滑块是控制者，重建对其他滑块的控制
            if (_controlledSliderGuids.Count > 0)
            {
                _controlledSliders.Clear(); // 清空现有列表
                foreach (var guid in _controlledSliderGuids.ToList())
                {
                    var slider = doc.FindObject(guid, true) as MotionSlider;
                    if (slider != null)
                    {
                        _controlledSliders.Add(slider); // 直接添加到控制列表
                        slider._isControlled = true;
                        slider._controllerGuid = this.InstanceGuid;
                    }
                }
                // 确保立即更新所有受控滑块的值
                ExpireSolution(true);
            }
        }

        private void Slider_ValueChanged(object sender, GH_SliderEventArgs e)
        {
            // 只更新受控滑块的值，不更新区间
            foreach (var slider in _controlledSliders)
            {
                if (slider != null)
                {
                    slider.SetSliderValue(Slider.Value);
                }
            }
        }

        // 添加一个只更新值的方法，不影响区间
        public virtual void SetSliderValue(decimal value)
        {
            if (!_isRefreshing && Slider.Value != value)
            {
                try
                {
                    _isRefreshing = true;

                    // 临时移除事件处理器以避免循环调用
                    this.Slider.ValueChanged -= Slider_ValueChanged;
                    Slider.Value = value;
                    ExpireSolution(true);
                }
                finally
                {
                    // 重新添加事件处理器
                    this.Slider.ValueChanged += Slider_ValueChanged;
                    _isRefreshing = false;
                }
            }
        }

        // 区间更新方法
        public virtual void SetRange(decimal minimum, decimal maximum)
        {
            bool rangeChanged = Slider.Minimum != minimum || Slider.Maximum != maximum;

            // 原有的 SetRange 逻辑
            Slider.Minimum = minimum;
            Slider.Maximum = maximum;

            // 保留 SetSliderValue 的逻辑
            decimal currentValue = Slider.Value;
            if (currentValue < minimum)
                SetSliderValue(minimum);
            else if (currentValue > maximum)
                SetSliderValue(maximum);

            // 如果范围发生变化，通知控制器更新范围
            if (rangeChanged && _isControlled && _controllerGuid != Guid.Empty)
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    var controller = doc.FindObject(_controllerGuid, true) as MotionUnionSlider;
                    controller?.UpdateUnionRange();
                }
            }
        }
    }

    public class MotionSliderAttributes : GH_ResizableAttributes<GH_NumberSlider>
    {
        private RectangleF _lastBounds;
        private readonly new MotionSlider Owner;
        private bool _initialized = false;
        private bool _isUpdating = false;
        private bool _isDraggingSlider = false;
        private int _dragMode = 0;
        protected override Size MinimumSize => new Size(
            (int)(TEXT_BOX_WIDTH + 100),  // 文本框宽度 + 最小滑块宽度
            20
        );
        protected override Size MaximumSize => new Size(5000, 20);
        protected override Padding SizingBorders => new Padding(6, 0, 6, 0);

        // 修改��本框相关的字段
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
            if (e.Button != System.Windows.Forms.MouseButtons.Left)
                return GH_ObjectResponse.Ignore;

            if (e.Button == MouseButtons.Left)
            {
                PointF pt = e.CanvasLocation;

                // 检查是否点击了区间文本框
                if (_rangeTextBox.Contains(pt))
                {
                    // 临时保存原始位置
                    var originalBounds = Owner.Slider.Bounds;

                    // 临时将滑块位置设置为文本框位置
                    Owner.Slider.Bounds = Rectangle.Round(_rangeTextBox);

                    // 根据模式准备不同的内容
                    string content;
                    if (MotionSliderSettings.IsSecondsInputMode())
                    {
                        double minSeconds = (double)Owner.Slider.Minimum / MotionSliderSettings.FramesPerSecond;
                        double maxSeconds = (double)Owner.Slider.Maximum / MotionSliderSettings.FramesPerSecond;
                        content = $"{minSeconds}-{maxSeconds}";
                    }
                    else
                    {
                        content = $"{Owner.Slider.Minimum}-{Owner.Slider.Maximum}";
                    }

                    Owner.Slider.ShowTextInputBox(
                        sender,
                        true,
                        sender.Viewport.XFormMatrix(GH_Viewport.GH_DisplayMatrix.CanvasToControl),
                        content
                    );
                    Owner.Slider.TextInputHandlerDelegate = TextInputHandler;

                    // 恢复原始位置
                    Owner.Slider.Bounds = originalBounds;
                    return GH_ObjectResponse.Handled;
                }
            }
            // 如果不是文本框，检查是否点击了滑块
            if ((double)sender.Viewport.Zoom >= 0.9 && Owner.Slider.Bounds.Contains(GH_Convert.ToPoint(e.CanvasLocation)))
            {
                string sliderContent = base.Owner.Slider.GripTextPure;
                base.Owner.Slider.TextInputHandlerDelegate = TextInputHandler;
                base.Owner.Slider.ShowTextInputBox(
                    sender,
                    true,
                    sender.Viewport.XFormMatrix(GH_Viewport.GH_DisplayMatrix.CanvasToControl),
                    sliderContent
                );
                return GH_ObjectResponse.Handled;
            }
            else
            {
                base.Owner.PopupEditor();
                return GH_ObjectResponse.Handled;
            }
        }

        // 添加或修改 TextInputHandler 方法来处理秒数转换
        private void TextInputHandler(GH_SliderBase slider, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string[] parts = text.Split('-');
            if (parts.Length == 2)
            {
                if (MotionSliderSettings.IsSecondsInputMode())
                {
                    // 秒数输入模式
                    if (double.TryParse(parts[0].Trim(), out double minSeconds) &&
                        double.TryParse(parts[1].Trim(), out double maxSeconds))
                    {
                        if (minSeconds < maxSeconds)
                        {
                            // 转换秒数为帧数
                            decimal minFrames = (decimal)MotionSliderSettings.ConvertSecondsToFrames(minSeconds);
                            decimal maxFrames = (decimal)MotionSliderSettings.ConvertSecondsToFrames(maxSeconds);

                            Owner.Slider.Minimum = minFrames;
                            Owner.Slider.Maximum = maxFrames;
                            Owner.Slider.Value = minFrames;

                            // 取消选中状态
                            Owner.OnPingDocument()?.DeselectAll();
                            Instances.ActiveCanvas?.Refresh();
                        }
                    }
                }
                else
                {
                    // 原有的帧数处理逻辑
                    if (decimal.TryParse(parts[0].Trim(), out decimal min) &&
                        decimal.TryParse(parts[1].Trim(), out decimal max))
                    {
                        if (min < max)
                        {
                            Owner.Slider.Minimum = min;
                            Owner.Slider.Maximum = max;
                            Owner.Slider.Value = min;
                            // 取消选中状态
                            Owner.OnPingDocument()?.DeselectAll();
                            Instances.ActiveCanvas?.Refresh();
                        }
                    }
                }
            }

            Owner.RecordUndoEvent("Changed Slider Range");
            Owner.ExpireSolution(true);
        }
    }
}