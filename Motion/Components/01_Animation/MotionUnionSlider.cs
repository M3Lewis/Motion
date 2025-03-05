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
        public override Guid ComponentGuid => new Guid("1a76afcb-c799-42d1-9a52-5f09ba073362");
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public static MotionUnionSlider Instance
        {
            get => _instance;
            set
            {
                if (_instance != value)
                {
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
            _updateCancellation = new CancellationTokenSource();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (Instance == this)
            {
                Instance = null;
            }
            _updateCancellation?.Cancel();
            _updateCancellation?.Dispose();
            _updateLock?.Dispose();
            base.RemovedFromDocument(document);
        }

        public override void CreateAttributes()
        {
            m_attributes = new MotionUnionSliderAttributes(this);
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