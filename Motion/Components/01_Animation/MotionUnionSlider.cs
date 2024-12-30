using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class MotionUnionSlider : MotionSlider
    {
        private bool _isRestoringState = false;
        private static MotionUnionSlider _instance;
        private bool _isUpdatingRange = false;
        private readonly System.Threading.SemaphoreSlim _updateLock = new System.Threading.SemaphoreSlim(1, 1);
        private CancellationTokenSource _updateCancellation;
        private bool _isUpdating = false;
        private decimal _pendingValue;
        private const int UPDATE_DELAY = 16; // 约60fps的更新频率
        public override Guid ComponentGuid => new Guid("1a76afcb-c799-42d1-9a52-5f09ba073362");
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public static MotionUnionSlider Instance
        {
            get => _instance;
            set
            {
                if (_instance != value)
                {
                    if (_instance != null)
                    {
                        // 清理旧实例的事件订阅
                        _instance.CleanupEvents();
                    }
                    _instance = value;
                }
            }
        }

        public MotionUnionSlider() : base()
        {
            NickName = "TimeLine(Union)";
            Name = "Motion Union Slider";
            Description = "统一控制多个Motion Slider的主控滑块。\n" +
                         "可以自动连接到所有Event Operation的时间输入端。";
            Category = "Motion";
            SubCategory = "01_Animation";

            Instance = this;
            SetupEvents();
            this.Slider.ValueChanged += UnionSlider_ValueChanged;
            _updateCancellation = new CancellationTokenSource();
        }

        public MotionUnionSlider(decimal minimum, decimal maximum) : base(minimum, maximum)
        {
            NickName = "TimeLine(Union)";
            Name = "Motion Union Slider";
            Description = "统一控制多个Motion Slider的主控滑块。\n" +
                         "可以自动连接到所有Event Operation的时间输入端。";
            Category = "Motion";
            SubCategory = "01_Animation";

            Instance = this;
            SetupEvents();
            this.Slider.ValueChanged += UnionSlider_ValueChanged;
            _updateCancellation = new CancellationTokenSource();
        }

        private void SetupEvents()
        {
            // 监听文档事件
            GH_Document doc = this.OnPingDocument();
            if (doc != null)
            {
                doc.ObjectsAdded += OnObjectsAdded;
                doc.ObjectsDeleted += OnObjectsDeleted;
                doc.SolutionEnd += Doc_SolutionEnd;
            }
        }

        private void CleanupEvents()
        {
            GH_Document doc = this.OnPingDocument();
            if (doc != null)
            {
                doc.ObjectsAdded -= OnObjectsAdded;
                doc.ObjectsDeleted -= OnObjectsDeleted;
                doc.SolutionEnd -= Doc_SolutionEnd;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (Slider != null)
            {
                Slider.ValueChanged -= UnionSlider_ValueChanged;
            }
            _updateCancellation?.Cancel();
            _updateCancellation?.Dispose();
            _updateLock?.Dispose();
            CleanupEvents();
            base.RemovedFromDocument(document);
        }

        private void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            if (_isUpdatingRange) return;
            
            bool hasNewSlider = e.Objects.Any(obj => obj is MotionSlider && !(obj is MotionUnionSlider));
            if (hasNewSlider)
            {
                // 检查新添加的对象中是否有复制出来的被控制滑块
                foreach (IGH_DocumentObject obj in e.Objects)
                {
                    if (obj is MotionSlider slider && !_controlledSliders.Contains(slider))
                    {
                        if (slider._isControlled && slider._controllerGuid == this.InstanceGuid)
                        {
                            AddControlledSlider(slider);
                        }
                    }
                }
                
                GH_Document doc = this.OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(5, (d) => UpdateUnionRange());
                }
            }
        }

        private void OnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (_isUpdatingRange) return;

            // 检查是否有受控的滑块被删除
            bool hasDeletedSlider = e.Objects.Any(obj => 
                obj is MotionSlider && _controlledSliders.Contains(obj as MotionSlider));

            if (hasDeletedSlider)
            {
                // 清理已删除的滑块
                _controlledSliders.RemoveAll(slider => 
                    e.Objects.Contains(slider));
                _controlledSliderGuids.RemoveAll(guid => 
                    e.Objects.Any(obj => obj.InstanceGuid == guid));

                // 如果还有受控滑块，立即更新区间
                if (_controlledSliders.Count > 0)
                {
                    // 在主线程上执行更新
                    Instances.ActiveCanvas?.BeginInvoke((MethodInvoker)delegate
                    {
                        UpdateUnionRange();
                        ExpireSolution(false);
                    });
                }
            }
        }

        private void Doc_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            // 在解决方案结束时检查并更新范围
            if (!_isUpdatingRange)
            {
                UpdateUnionRange();
            }
        }

        public async void UpdateUnionRange()
        {
            GH_Document doc = this.OnPingDocument();
            if (_isUpdatingRange || doc == null || _controlledSliders.Count == 0) return;

            try
            {
                _isUpdatingRange = true;

                await Task.Run(() =>
                {
                    var sliders = _controlledSliders
                        .Where(s => s != null && s.Slider != null)
                        .ToList();

                    if (!sliders.Any()) return;

                    decimal globalMin = sliders.Min(s => s.Slider.Minimum);
                    decimal globalMax = sliders.Max(s => s.Slider.Maximum);

                    if (globalMin != Slider.Minimum || globalMax != Slider.Maximum)
                    {
                        Grasshopper.Instances.ActiveCanvas?.BeginInvoke((MethodInvoker)delegate
                        {
                            SetRange(globalMin, globalMax);

                            decimal currentValue = Slider.Value;
                            if (currentValue < globalMin)
                                SetSliderValue(globalMin);
                            else if (currentValue > globalMax)
                                SetSliderValue(globalMax);

                            doc.ScheduleSolution(1, d => ExpireSolution(true));
                        });
                    }
                });
            }
            finally
            {
                _isUpdatingRange = false;
            }
        }

        private async Task UpdateControlledSlidersAsync(decimal value, CancellationToken cancellationToken)
        {
            if (_isRestoringState || _isUpdatingRange || _isRefreshing) return;

            try
            {
                // 尝试获取锁，如果无法立即获取则保存待更新值并返回
                if (!await _updateLock.WaitAsync(0, cancellationToken))
                {
                    _pendingValue = value;
                    return;
                }

                _isRefreshing = true;
                _isUpdating = true;

                // 使用最新的值
                decimal updateValue = value;

                var slidersToUpdate = _controlledSliders
                    .Where(s => s?.Slider != null)
                    .ToList();

                // 批量更新所有滑块
                await Task.Run(() =>
                {
                    Grasshopper.Instances.ActiveCanvas?.BeginInvoke((MethodInvoker)delegate
                    {
                        foreach (var slider in slidersToUpdate)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            slider.SetSliderValue(updateValue);
                        }

                        // 一次性触发所有滑块的更新
                        foreach (var slider in slidersToUpdate)
                        {
                            slider.ExpireSolution(false);
                        }
                        this.ExpireSolution(true);
                    });
                }, cancellationToken);
            }
            finally
            {
                _isRefreshing = false;
                _isUpdating = false;
                _updateLock.Release();
            }
        }

        public async void UnionSlider_ValueChanged(object sender, GH_SliderEventArgs e)
        {
            if (_isUpdatingRange || _isRestoringState) return;

            try
            {
                // 取消之前的更新操作
                _updateCancellation.Cancel();
                _updateCancellation.Dispose();
                _updateCancellation = new CancellationTokenSource();

                // 移除延迟，直接执行更新
                await UpdateControlledSlidersAsync(Slider.Value, _updateCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // 忽略取消的操作
            }
            catch (Exception)
            {
                // 忽略其他异常
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new MotionUnionSliderAttributes(this);
        }

        public override void SetSliderValue(decimal value)
        {
            if (_isRestoringState)
            {
                // 在恢复状态下，直接设置值而不触发事件
                if (Slider != null)
                {
                    Slider.ValueChanged -= UnionSlider_ValueChanged;  // 临时移除事件处理
                    base.SetSliderValue(value);
                    Slider.ValueChanged += UnionSlider_ValueChanged;  // 恢复事件处理
                }
            }
            else
            {
                base.SetSliderValue(value);
            }
        }
        public override void AddControlledSlider(MotionSlider slider)
        {
            if (!_controlledSliders.Contains(slider))
            {
                //Rhino.RhinoApp.WriteLine($"MotionUnionSlider {InstanceGuid} adding control over {slider.InstanceGuid}");
                
                base.AddControlledSlider(slider);
                slider._isControlled = true;
                slider._controllerGuid = this.InstanceGuid;
                
                if (this.Slider != null)
                {
                    this.Slider.ValueChanged -= UnionSlider_ValueChanged;
                    this.Slider.ValueChanged += UnionSlider_ValueChanged;
                    //Rhino.RhinoApp.WriteLine("Value change event handler set");
                }

                if (!_isUpdatingRange)
                {
                    UpdateUnionRange();
                }

                //DebugControlRelationships();
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                //Rhino.RhinoApp.WriteLine($"Writing MotionUnionSlider {InstanceGuid}:");
                //Rhino.RhinoApp.WriteLine($"IsInstance: {this == Instance}");
                //Rhino.RhinoApp.WriteLine($"IsUpdatingRange: {_isUpdatingRange}");

                writer.SetBoolean("IsInstance", this == Instance);
                writer.SetBoolean("IsUpdatingRange", _isUpdatingRange);

                return true;
            }
            catch (Exception ex)
            {
                //Rhino.RhinoApp.WriteLine($"Error in MotionUnionSlider Write: {ex.Message}");
                return false;
            }
        }


        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            try
            {
                //Rhino.RhinoApp.WriteLine($"Reading MotionUnionSlider {InstanceGuid}:");
                _isUpdatingRange = reader.GetBoolean("IsUpdatingRange");
                bool isInstance = reader.GetBoolean("IsInstance");
                //Rhino.RhinoApp.WriteLine($"IsInstance: {isInstance}");

                if (isInstance)
                {
                    GH_Document doc = this.OnPingDocument();
                    if (doc != null)
                    {
                        //Rhino.RhinoApp.WriteLine("Setting up instance restoration");
                        doc.SolutionEnd += (s, e) =>
                        {
                            Instance = this;
                            //Rhino.RhinoApp.WriteLine($"Restored instance: {InstanceGuid}");

                            if (this.Slider != null)
                            {
                                this.Slider.ValueChanged -= UnionSlider_ValueChanged;
                                this.Slider.ValueChanged += UnionSlider_ValueChanged;
                                //Rhino.RhinoApp.WriteLine("Value change event handler set");
                            }

                            UpdateUnionRange();

                            doc.SolutionEnd -= (sender, args) => Instance = this;
                        };
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                //Rhino.RhinoApp.WriteLine($"Error in MotionUnionSlider Read: {ex.Message}");
                return false;
            }
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            SetupEvents();
            
            if (!_isUpdatingRange)
            {
                UpdateUnionRange();
            }

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
                "连接到所有时间输入端",
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
    }

    public class MotionUnionSliderAttributes : MotionSliderAttributes
    {
        public MotionUnionSliderAttributes(MotionUnionSlider owner) : base(owner)
        {
            owner.NickName = "TimeLine(Union)";
            owner.Name = "MotionUnionSlider";
            owner.Category = "Motion";
            owner.SubCategory = "01_Animation";
        }

        protected override void Layout()
        {
            base.Layout();  // 调用基类的 Layout 方法来设置基本布局和文本框

            // 设置滑块的显示属性
            Owner.Slider.DrawControlBorder = true;
            Owner.Slider.ControlEdgeColour = Color.DeepSkyBlue;
            Owner.Slider.DrawControlShadows = true;
            Owner.Slider.DrawControlBackground = false;
            Owner.Slider.GripEdgeColour = Color.DeepSkyBlue;
            Owner.Slider.GripDisplay = GH_SliderGripDisplay.Numeric;
            Owner.Slider.GripTopColour = Color.DeepSkyBlue;
            Owner.Slider.GripBottomColour = Color.DeepSkyBlue;
            Owner.Slider.RailBrightColour = Color.CadetBlue;
            Owner.Slider.RailDisplay = GH_SliderRailDisplay.Etched;
            Owner.Slider.RailDarkColour = Color.FromArgb(255, 40,40,40);
            Owner.Slider.RailEmptyColour = Color.FromArgb(255, 40, 40, 40);
            Owner.Slider.RailBrightColour = Color.FromArgb(255, 40, 40, 40);
            Owner.Slider.RailFullColour = Color.FromArgb(255, 230,230,230);
            
            Owner.Slider.Padding = new Padding(0,0,0,0);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel != GH_CanvasChannel.Objects)
                return;

            int[] radii2 = new int[4] { 0, 0, 0, 0 };

            using (GH_Capsule sliderCapsule = GH_Capsule.CreateCapsule(
                Owner.Slider.Bounds,
                GH_Palette.White,
                radii2,
                5))
            {
                sliderCapsule.AddOutputGrip(OutputGrip.Y);
                sliderCapsule.Render(graphics, Selected, Owner.Locked, false);
            }

            // 绘制滑块
            Owner.Slider.Render(graphics);

            // 绘制区间范围文本
            string rangeText = $"{Owner.Slider.Minimum}-{Owner.Slider.Maximum}";
            var font = GH_FontServer.StandardBold;
            float fontHeight = GH_FontServer.StandardAdjusted.Height;

            using (var brush = new SolidBrush(Color.DeepSkyBlue))
            {
                // 绘制帧数范围
                float rangeTextX = Owner.Slider.Bounds.Left - GH_FontServer.StringWidth(rangeText, font) - 5;
                float rangeTextY = Owner.Slider.Bounds.Top + (Owner.Slider.Bounds.Height - fontHeight) / 2;
                graphics.DrawString(rangeText, font, brush, new PointF(rangeTextX, rangeTextY));

                // 如果处于秒数输入模式，绘制秒数文本
                if (Motion.Toolbar.MotionSliderSettings.IsSecondsInputMode())
                {
                    // 计算秒数
                    double minSeconds = (double)Owner.Slider.Minimum / Motion.Toolbar.MotionSliderSettings.FramesPerSecond;
                    double maxSeconds = (double)Owner.Slider.Maximum / Motion.Toolbar.MotionSliderSettings.FramesPerSecond;
                    string secondsText = $"{minSeconds:F1}s-{maxSeconds:F1}s";

                    // 创建秒数文本的边界
                    var secondsTextBounds = new RectangleF(
                        rangeTextX - 100,  // 在区间文本左侧100像素
                        rangeTextY,
                        95,  // 文本宽度
                        fontHeight
                    );

                    // 绘制秒数��本
                    graphics.DrawString(
                        secondsText,
                        GH_FontServer.Standard,
                        brush,
                        secondsTextBounds,
                        new StringFormat()
                        {
                            Alignment = StringAlignment.Far,  // 右对齐
                            LineAlignment = StringAlignment.Center
                        }
                    );
                }
            }
        }
    }

    //public class MotionUnionSliderLockAttributes : MotionUnionSliderAttributes
    //{
    //    public MotionUnionSlider Owner;
    //    private readonly PointF _lockedPivot;
    //    public MotionUnionSliderLockAttributes(MotionUnionSlider owner) : base(owner)
    //    {
    //        Owner = owner;
    //        // 保持原有的边界和位置信息
    //        this.Bounds = owner.Attributes.Bounds;
    //        _lockedPivot = owner.Attributes.Pivot;
    //    }

    //    // 禁止选择
    //    public override bool Selected
    //    {
    //        get { return false; }
    //        set { /* 不做任何事 */ }
    //    }
    //    protected override Padding SizingBorders => new Padding(0, 0, 0, 0);

    //    // 锁定 Pivot - 只返回固定位置，忽略所有设置
    //    public override PointF Pivot
    //    {
    //        get => _lockedPivot;
    //    }
    //    public override bool IsPickRegion(PointF pt)
    //    {
    //        return false;
    //    }

    //    protected override void Layout()
    //    {
    //        base.Layout();
    //        base.Owner.Slider.DrawControlBorder = true;
    //        base.Owner.Slider.ControlEdgeColour = Color.DeepSkyBlue;
    //        base.Owner.Slider.DrawControlShadows = true;
    //        base.Owner.Slider.DrawControlBackground = false;
    //        base.Owner.Slider.GripEdgeColour = Color.DeepSkyBlue;
    //        base.Owner.Slider.GripDisplay = GH_SliderGripDisplay.Numeric;
    //        base.Owner.Slider.GripTopColour = Color.DeepSkyBlue;
    //        base.Owner.Slider.GripBottomColour = Color.DeepSkyBlue;
    //        base.Owner.Slider.RailBrightColour = Color.CadetBlue;
    //        base.Owner.Slider.RailDisplay = GH_SliderRailDisplay.Etched;
    //        base.Owner.Slider.RailDarkColour = Color.FromArgb(255, 40, 40, 40);
    //        base.Owner.Slider.RailEmptyColour = Color.FromArgb(255, 40, 40, 40);
    //        base.Owner.Slider.RailBrightColour = Color.FromArgb(255, 40, 40, 40);
    //        base.Owner.Slider.RailFullColour = Color.FromArgb(255, 230, 230, 230);

    //        base.Owner.Slider.Padding = new Padding(0, 0, 0, 0);
    //    }
    //    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    //    {
    //        // 首先调用基类的渲染
    //        base.Render(canvas, graphics, channel);

    //        if (channel == GH_CanvasChannel.Objects)
    //        {
    //            // 绘制锁定状态的边框
    //            RectangleF bounds = Owner.Slider.Bounds;
    //            bounds.Inflate(2, 2); // 扩大框范围

    //            // 创建虚线画笔
    //            using (Pen lockPen = new Pen(Color.FromArgb(128, 41, 171, 173), 1f))
    //            {
    //                lockPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
    //                graphics.DrawRectangle(lockPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    //            }
    //        }
    //    }
    //}
} 