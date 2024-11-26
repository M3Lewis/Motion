using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GH_IO.Serialization;

namespace Motion.Animation
{
    public class MotionSlider : GH_NumberSlider
    {
        // 添加静态事件来通知新滑块创建
        public static event EventHandler<MotionSlider> SliderCreated;
        
        // 添加静态字段来跟踪主控滑块
        private static MotionSlider _mainController;
        
        private List<MotionSlider> _controlledSliders = new List<MotionSlider>();
        private bool _isControlled;
        private decimal _lastValue;
        private bool _isDragging = false;
        private bool _isDraggingLeft = false;
        private PointF _lastMouseLocation;
        private bool _isRefreshing = false;

        public event EventHandler<decimal> ValueChanged;

        public override Guid ComponentGuid => new Guid("A6704806-4EE3-42AF-B742-3C348C5F7F38");
        public MotionSlider()
        {
            NickName = "MotionSlider";
            Name = "Motion Slider";
            Description = "可调节区间的动画滑块";
            Category = "Motion";
            SubCategory = "01_Animation";

            SetInitialValues();
            
            // 触发新建事件
            SliderCreated?.Invoke(this, this);
        }

        public MotionSlider(decimal minimum, decimal maximum) : base()
        {
            NickName = "MotionSlider";
            Name = "Motion Slider";
            Description = "可调节区间的动画滑块";
            Category = "Motion";
            SubCategory = "01_Animation";

            // 设置滑块属性
            Slider.Type = GH_SliderAccuracy.Float;
            Slider.DecimalPlaces = 2;
            Slider.TickDisplay = GH_SliderTickDisplay.None;
            Slider.RailDisplay = GH_SliderRailDisplay.Filled;
            
            // 设置区间范围
            Slider.Minimum = minimum;
            Slider.Maximum = maximum;
            Slider.Value = minimum;

            _lastValue = minimum;
            
            // 触发新建事件
            SliderCreated?.Invoke(this, this);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsDeleted += Document_ObjectsDeleted;
            
            // 如果是新创建的滑块，触发事件
            if (_mainController != null && _mainController != this)
            {
                SliderCreated?.Invoke(this, this);
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
            
            // 更新范围
            decimal newMin = Math.Min(Slider.Minimum, newSlider.Slider.Minimum);
            decimal newMax = Math.Max(Slider.Maximum, newSlider.Slider.Maximum);
            
            // 如果范围有变化，更新主控滑块
            if (newMin != Slider.Minimum || newMax != Slider.Maximum)
            {
                SetRange(newMin, newMax);
                
                // 请求重新计算解决方案
                if (OnPingDocument() != null)
                {
                    OnPingDocument().NewSolution(false);
                }
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
            if (CurrentValue != _lastValue)
            {
                ValueChanged?.Invoke(this, CurrentValue);
                
                // 更新所有被控制的滑块
                foreach (var slider in _controlledSliders)
                {
                    if (CurrentValue >= slider.Slider.Minimum && 
                        CurrentValue <= slider.Slider.Maximum)
                    {
                        slider.UpdateValue(CurrentValue);
                    }
                }
                
                _lastValue = CurrentValue;
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
            if (_isControlled && unionValue >= Slider.Minimum && unionValue <= Slider.Maximum)
            {
                SetSliderValue(unionValue);
            }
        }

        /// <summary>
        /// 添加要控制的滑块
        /// </summary>
        public void AddControlledSlider(MotionSlider slider)
        {
            if (slider != null && !_controlledSliders.Contains(slider))
            {
                _controlledSliders.Add(slider);
                slider.IsControlled = true;
            }
        }

        /// <summary>
        /// 移除被控制的滑块
        /// </summary>
        public void RemoveControlledSlider(MotionSlider slider)
        {
            if (slider != null && _controlledSliders.Contains(slider))
            {
                _controlledSliders.Remove(slider);
                slider.IsControlled = false;
            }
        }

        /// <summary>
        /// 获取是否有被控制的滑块
        /// </summary>
        public bool HasControlledSliders => _controlledSliders.Count > 0;

        /// <summary>
        /// 获取所有被控制的滑块
        /// </summary>
        public IEnumerable<MotionSlider> GetControlledSliders()
        {
            return _controlledSliders.AsReadOnly();
        }

        /// <summary>
        /// 清除所有被控制的滑块
        /// </summary>
        public void ClearControlledSliders()
        {
            foreach (var slider in _controlledSliders)
            {
                slider.IsControlled = false;
            }
            _controlledSliders.Clear();
        }

        // 添加一个新的方法来更新区间范围
        public void SetRange(decimal minimum, decimal maximum)
        {
            Slider.Minimum = minimum;
            Slider.Maximum = maximum;
            Slider.Value = Math.Max(minimum, Math.Min(maximum, Slider.Value));
            ExpireSolution(true);
        }
    }

    public class MotionSliderAttributes : GH_NumberSliderAttributes
    {
        private RectangleF _lastBounds;
        private readonly new MotionSlider Owner;
        private bool _initialized = false;
        private bool _isUpdating = false;

        public MotionSliderAttributes(MotionSlider owner) : base(owner)
        {
            Owner = owner;
        }

        protected override void Layout()
        {
            // 计算名称区域大小
            SizeF sizeF = GH_FontServer.MeasureString(base.Owner.ImpliedNickName, GH_FontServer.StandardAdjusted);
            sizeF.Width = Math.Max(sizeF.Width, MinimumSize.Height);

            // 设置整体边界
            Bounds = new RectangleF(Pivot.X, Pivot.Y, Math.Max(Bounds.Width, sizeF.Width + 1), MinimumSize.Height);
            Bounds = GH_Convert.ToRectangle(Bounds);

            // 设置名称区域
            Rectangle boundsName = GH_Convert.ToRectangle(new RectangleF(Pivot.X, Pivot.Y, sizeF.Width, MinimumSize.Height));

            // 设置滑块区域
            Rectangle boundsSlider = Rectangle.FromLTRB(
                boundsName.Right,
                boundsName.Top,
                Convert.ToInt32(Bounds.Right),
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
            base.Owner.Slider.RailDisplay = GH_SliderRailDisplay.Filled;
            base.Owner.Slider.GripDisplay = GH_SliderGripDisplay.Shape;
            base.Owner.Slider.TextColour = Color.FromArgb(255, 43, 141, 174);
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

            // 绘制名称区域
            int[] radii = new int[4] { 3, 0, 0, 3 };
            Rectangle textbox = Owner.Slider.Bounds;
            GH_Capsule nameCapsule = GH_Capsule.CreateTextCapsule(
                textbox, textbox, GH_Palette.Hidden,
                Owner.ImpliedNickName, radii, 5
            );
            nameCapsule.Render(graphics, Selected, Owner.Locked, true);
            nameCapsule.Dispose();

            // 绘制滑块区域
            int[] radii2 = new int[4] { 0, 3, 3, 0 };
            GH_Capsule sliderCapsule = GH_Capsule.CreateCapsule(
                Owner.Slider.Bounds,
                GH_Palette.Normal,
                radii2,
                5
            );
            sliderCapsule.Render(graphics, Selected, Owner.Locked, false);
            sliderCapsule.Dispose();

            // 绘制滑块
            Owner.Slider.Render(graphics);

            // 绘制区间范围文本
            string rangeText = $"{Owner.Slider.Minimum}-{Owner.Slider.Maximum}";
            var font = GH_FontServer.Standard;
            float fontHeight = GH_FontServer.StandardAdjusted.Height;

            using (var brush = new SolidBrush(Color.LightSkyBlue))
            {
                float textX = Owner.Slider.Bounds.Left - GH_FontServer.StringWidth(rangeText, font) - 5;
                float textY = Owner.Slider.Bounds.Top + (Owner.Slider.Bounds.Height - fontHeight) / 2;
                graphics.DrawString(rangeText, font, brush, new PointF(textX, textY));
            }
        }

        public override void ExpireLayout()
        {
            base.ExpireLayout();

            if (!_isUpdating && Owner?.Slider != null && Bounds.Width > 0)
            {
                UpdateSliderRange();
            }
        }

        private void UpdateSliderRange()
        {
            if (_isUpdating) return;

            var currentBounds = Bounds;
            if (!currentBounds.Equals(_lastBounds))
            {
                try
                {
                    _isUpdating = true;

                    // 使用 Owner.Slider.Bounds 获取实际滑块区域
                    var sliderBounds = Owner.Slider.Bounds;
                    decimal newMin = Math.Max(0, (decimal)sliderBounds.Left);
                    decimal newMax = (decimal)sliderBounds.Right;

                    // 保存当前值在区间中的比例
                    decimal oldMin = Owner.Slider.Minimum;
                    decimal oldMax = Owner.Slider.Maximum;
                    decimal oldValue = Owner.Slider.Value;
                    decimal proportion = 0;

                    if (oldMax != oldMin)
                    {
                        proportion = (oldValue - oldMin) / (oldMax - oldMin);
                    }

                    // 确保最小值小于最大值
                    if (newMin < newMax)
                    {
                        Owner.Slider.Minimum = newMin;
                        Owner.Slider.Maximum = newMax;

                        // 根据比例计算新值
                        decimal newValue = Owner.Slider.Minimum +
                            (Owner.Slider.Maximum - Owner.Slider.Minimum) * proportion;

                        // 更新值
                        Owner.SetSliderValue(newValue);
                    }

                    _lastBounds = currentBounds;
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }
    }
}