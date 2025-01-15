using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Widgets;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using Motion.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace Motion.Widgets
{
    internal class TimelineWidget : GH_Widget
    {
        // 关键帧数据结构
        private class Keyframe
        {
            public int Frame { get; set; }
            public double Value { get; set; }
        }
        
        private List<Keyframe> keyframes = new List<Keyframe>();
        private double currentValue = 0.0;
        
        private bool isPlaying = false;
        private int currentFrame = 1;
        private int startFrame = 1;
        private int endFrame = 250;
        private Rectangle playButtonBounds;
        private Rectangle frameCounterBounds;
        private Rectangle timelineBounds;
        private Rectangle startFrameBounds;
        private Rectangle endFrameBounds;
        
        private System.Windows.Forms.Timer animationTimer;
        private TextBox activeTextBox = null;
        private bool isHoveringIndicator = false;
        private float pixelsPerFrame = 0;

        private readonly SizeF labelSize = new SizeF(40, 20); // 固定标签大小，足够容纳4位数字

        // 添加常量定义显示区域的参数
        private const float VALUE_DISPLAY_HEIGHT = 60; // 值显示区域的高度
        private const float VALUE_DISPLAY_MARGIN = 20; // 与时间轴的间距

        // 添加关键帧编辑对话框
        private class KeyframeDialog : Form
        {
            public double Value { get; private set; }
            private TextBox valueTextBox;

            public KeyframeDialog(double initialValue)
            {
                this.Text = "Set Keyframe Value";
                this.Size = new Size(200, 120);
                this.StartPosition = FormStartPosition.CenterParent;
                
                var label = new Label
                {
                    Text = "Value:",
                    Location = new Point(10, 20),
                    Size = new Size(50, 20)
                };
                
                valueTextBox = new TextBox
                {
                    Text = initialValue.ToString(),
                    Location = new Point(70, 20),
                    Size = new Size(100, 20)
                };
                
                var okButton = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(20, 50),
                    Size = new Size(70, 25)
                };
                
                var cancelButton = new System.Windows.Forms.Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(100, 50),
                    Size = new Size(70, 25)
                };
                
                this.Controls.AddRange(new Control[] { label, valueTextBox, okButton, cancelButton });
                this.AcceptButton = okButton;
                this.CancelButton = cancelButton;
            }

            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                if (DialogResult == DialogResult.OK)
                {
                    if (double.TryParse(valueTextBox.Text, out double result))
                    {
                        Value = result;
                    }
                    else
                    {
                        e.Cancel = true;
                        MessageBox.Show("Please enter a valid number.");
                    }
                }
                base.OnFormClosing(e);
            }
        }

        // 添加字段来跟踪选中的关键帧
        private Keyframe selectedKeyframe = null;

        // 添加缺失的字段定义
        private const int padding = 10;
        private Rectangle bounds;

        private MotionValueParameter valueParam;
        private bool isParamInitialized = false;

        public TimelineWidget()
        {
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        // 添加初始化参数的方法
        private void InitializeParameter()
        {
            if (isParamInitialized || Owner?.Document == null) return;

            // 创建参数
            valueParam = new MotionValueParameter();
            valueParam.CreateAttributes();
            
            // 设置参数位置
            valueParam.Attributes.Pivot = new System.Drawing.PointF(100, 100);
            
            // 添加参数到文档
            Owner.Document.AddObject(valueParam, false);
            
            isParamInitialized = true;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                if (currentFrame >= endFrame)
                {
                    currentFrame = startFrame;
                    isPlaying = false;
                    animationTimer.Stop();
                }
                else
                {
                    currentFrame++;
                }
                
                // 更新当前值
                UpdateCurrentValue();
                Owner?.Refresh();
            }
        }

        public override string Name => "Timeline Widget";

        public override string Description => "Motion Animation Timeline widget";

        public override Bitmap Icon_24x24 => null;

        private static TimelineWidgetDock m_dockSide = (TimelineWidgetDock)Instances.Settings.GetValue("Motion.Widget.Timeline.Side", 1);
        private static bool m_showWidget = Instances.Settings.GetValue("Motion.Widget.Timeline.Show", true);

        private static readonly int ControlAreaSize = Global_Proc.UiAdjust(100);//28
        private static readonly int BorderAreaSize = Global_Proc.UiAdjust(30);
        private Rectangle WidgetArea => Rectangle.Union(ActualControlArea, BorderArea);

        private Rectangle ActualControlArea => CreateControlArea();

        private Rectangle BorderArea
        {
            get
            {
                if (Owner?.Bounds == null || Owner.Bounds.IsEmpty)
                    return Rectangle.Empty;

                var clientRect = Owner.ClientRectangle;
                const int height = 200;  // 边界区域固定高度
                
                // 根据停靠位置计算边界区域
                return m_dockSide switch
                {
                    TimelineWidgetDock.Top => new Rectangle(
                        clientRect.Left,           // 从窗口左侧开始
                        clientRect.Top+20,             // 从工具栏下方开始
                        clientRect.Width,          // 覆盖整个窗口宽度
                        height),                   // 固定高度
                        
                    TimelineWidgetDock.Bottom => new Rectangle(
                        clientRect.Left,           // 从窗口左侧开始
                        clientRect.Bottom - height, // 从底部向上偏移固定高度
                        clientRect.Width,          // 覆盖整个窗口宽度
                        height),                   // 固定高度
                        
                    _ => Rectangle.Empty
                };
            }
        }

        private Rectangle CreateControlArea()
        {
            const int padding = 10;  // 内边距
            var border = BorderArea;
            
            if (border.IsEmpty)
                return Rectangle.Empty;
            
            // 从边界区域向内缩进
            return new Rectangle(
                border.Left + padding,           // 左边缘向内缩进
                border.Top + padding,            // 上边缘向内缩进
                border.Width - (padding * 2),    // 宽度减去两侧内边距
                border.Height - (padding * 2)    // 高度减去上下内边距
            );
        }

        public static TimelineWidgetDock DockSide
        {
            get
            {
                return m_dockSide;
            }
            set
            {
                if (m_dockSide != value)
                {
                    m_dockSide = value;
                    Instances.Settings.SetValue("Motion.Widget.Timeline.Side", (int)value);
                    DockSideChangedEvent?.Invoke();
                }
            }
        }

        public static bool SharedVisible
        {
            get
            {
                return m_showWidget;
            }
            set
            {
                if (m_showWidget != value)
                {
                    m_showWidget = value;
                    Instances.Settings.SetValue("Motion.Widget.Timeline.Show", value);
                    WidgetVisibleChangedEvent?.Invoke();
                }
            }
        }

        public override bool Visible
        {
            get
            {
                return SharedVisible;
            }
            set
            {
                SharedVisible = value;
            }
        }
        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
        }

        public delegate void DockSideChangedEventHandler();

        public delegate void WidgetVisibleChangedEventHandler();
        public static DockSideChangedEventHandler DockSideChangedEvent;
        public static WidgetVisibleChangedEventHandler WidgetVisibleChangedEvent;

        public static event DockSideChangedEventHandler DockSideChanged
        {
            add
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = DockSideChangedEvent;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Combine(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref DockSideChangedEvent, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
            remove
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = DockSideChangedEvent;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Remove(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref DockSideChangedEvent, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
        }

        public static event WidgetVisibleChangedEventHandler WidgetVisibleChanged
        {
            add
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Combine(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
            remove
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Remove(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
        }

        // 添加方法来检查是否点击到关键帧
        private Keyframe GetKeyframeAtPoint(Point point)
        {
            if (keyframes.Count == 0) return null;
            
            // 检查每个关键帧
            foreach (var keyframe in keyframes)
            {
                float x = timelineBounds.Left + (keyframe.Frame - startFrame) * pixelsPerFrame;
                float y = MapValueToY(keyframe.Value);
                
                // 创建关键帧点的矩形区域（8x8像素）
                RectangleF keyframeRect = new RectangleF(x - 4, y - 4, 8, 8);
                
                if (keyframeRect.Contains(point))
                {
                    return keyframe;
                }
            }
            
            return null;
        }

        // 重写右键菜单方法
        public override void AppendToMenu(ToolStripDropDownMenu menu)
        {
            base.AppendToMenu(menu);
            
            // 如果有选中的关键帧，添加删除选项
            if (selectedKeyframe != null)
            {
                GH_DocumentObject.Menu_AppendSeparator(menu);
                GH_DocumentObject.Menu_AppendItem(menu, "Delete Keyframe", Menu_DeleteKeyframe);
            }
            else
            {
                // 原有的菜单项
                GH_DocumentObject.Menu_AppendSeparator(menu);
                GH_DocumentObject.Menu_AppendItem(menu, "Top", Menu_DockTop, enabled: true, m_dockSide == TimelineWidgetDock.Top);
                GH_DocumentObject.Menu_AppendItem(menu, "Bottom", Menu_DockBottom, enabled: true, m_dockSide == TimelineWidgetDock.Bottom);
                
                // 添加创建Motion Value参数的选项
                if (keyframes.Count > 0)
                {
                    GH_DocumentObject.Menu_AppendSeparator(menu);
                    if (valueParam != null)
                    {
                        GH_DocumentObject.Menu_AppendItem(menu, "Add Motion Value Parameter", Menu_AddMotionValue);
                    }
                }
            }
        }

        // 添加删除关键帧的方法
        private void Menu_DeleteKeyframe(object sender, EventArgs e)
        {
            if (selectedKeyframe != null)
            {
                keyframes.Remove(selectedKeyframe);
                selectedKeyframe = null;
                UpdateCurrentValue();
                Owner?.Refresh();
            }
        }

        private void Menu_DockTop(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Top;
            m_owner.Refresh();
        }

        private void Menu_DockBottom(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Bottom;
            m_owner.Refresh();
        }

        // 添加清除所有关键帧的方法
        private void Menu_ClearKeyframes(object sender, EventArgs e)
        {
            // 添加确认对话框
            if (MessageBox.Show(
                "Are you sure you want to clear all keyframes?",
                "Clear Keyframes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                keyframes.Clear();
                selectedKeyframe = null;
                currentValue = 0.0;
                Owner?.Refresh();
            }
        }

        // 添加方法来查找现有的 Motion Value 参数
        private MotionValueParameter FindExistingMotionValue()
        {
            if (Owner?.Document == null) return null;
            
            // 查找文档中所有的 MotionValueParameter 实例
            return Owner.Document.Objects
                .OfType<MotionValueParameter>()
                .FirstOrDefault();
        }

        // 修改 Menu_AddMotionValue 方法
        private void Menu_AddMotionValue(object sender, EventArgs e)
        {
            if (Owner?.Document == null) return;

            try
            {
                // 先查找是否已存在 Motion Value 参数
                valueParam = FindExistingMotionValue();
                
                // 如果不存在，则创建新的
                if (valueParam == null)
                {
                    valueParam = new MotionValueParameter();
                    valueParam.CreateAttributes();
                    valueParam.NickName = "Motion Value";
                    
                    // 获取当前widget的位置
                    var widgetBounds = WidgetArea;
                    
                    // 设置参数位置（在widget右侧）
                    valueParam.Attributes.Pivot = new System.Drawing.PointF(
                        widgetBounds.Right + 50,
                        widgetBounds.Top
                    );
                    
                    // 添加参数到文档
                    Owner.Document.AddObject(valueParam, false);
                    
                    // 立即更新值
                    UpdateCurrentValue();
                    
                    // 强制更新解决方案
                    Owner.Document.NewSolution(true);
                    
                    // 调试输出
                    Rhino.RhinoApp.WriteLine("Created new Motion Value parameter");
                }
                else
                {
                    Rhino.RhinoApp.WriteLine("Found existing Motion Value parameter");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error creating Motion Value parameter: {ex.Message}");
            }
        }

        //----------------------------------------------------------------------------------------------------
        public override bool Contains(Point pt_control, PointF pt_canvas)
        {
            if (base.Owner.Document == null)
            {
                return false;
            }
            // 扩大点击检测区域
            var area = WidgetArea;
            area.Inflate(5, 5);  // 向外扩展5个像素
            return area.Contains(pt_control);
        }

        public override void Render(GH_Canvas canvas)
        {
            // 如果还没有关联参数，尝试查找现有的
            if (valueParam == null)
            {
                valueParam = FindExistingMotionValue();
            }
            
            // 在第一次渲染时初始化参数
            InitializeParameter();
            
            bounds = WidgetArea;
            
            if (!Visible || canvas?.Graphics == null) return;
            
            try 
            {
                Graphics g = canvas.Graphics;
                var transform = g.Transform;
                g.ResetTransform();
                
                Rectangle bounds = WidgetArea;
                if (bounds.IsEmpty) return;
                
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // 计算各个区域的边界
                const int buttonSize = 30;
                const int padding = 5;
                const int textBoxHeight = 20;
                
                // 播放按钮区域
                playButtonBounds = new Rectangle(
                    bounds.Left + padding, 
                    bounds.Top + (bounds.Height - buttonSize) / 2,
                    buttonSize, 
                    buttonSize);
                    
                // 帧计数器区域
                frameCounterBounds = new Rectangle(
                    playButtonBounds.Right + padding,
                    bounds.Top + (bounds.Height - buttonSize) / 2,
                    80,
                    buttonSize);
                    
                // 添加起始帧和结束帧输入框 - 移到上方
                startFrameBounds = new Rectangle(
                    timelineBounds.Right - 160,
                    bounds.Top + padding,
                    70,
                    textBoxHeight);
                    
                endFrameBounds = new Rectangle(
                    timelineBounds.Right - 80,
                    bounds.Top + padding,
                    70,
                    textBoxHeight);
                    
                // 时间轴区域 - 移到下方，避免与文本框重叠
                timelineBounds = new Rectangle(
                    frameCounterBounds.Right + padding,
                    bounds.Top + bounds.Height / 2 - 15,  // 将时间轴垂直居中，上下各预留15像素的空间
                    bounds.Right - frameCounterBounds.Right - padding * 3,
                    30  // 设置固定高度为30像素
                );
                
                // 绘制背景
                using (var brush = new SolidBrush(Color.FromArgb(53, 53, 53)))
                {
                    g.FillRectangle(brush, bounds);
                }
                
                // 绘制播放按钮
                using (var pen = new Pen(Color.White, 2))
                using (var brush = new SolidBrush(Color.White))
                {
                    if (!isPlaying)
                    {
                        // 绘制播放三角形
                        Point[] trianglePoints = new Point[]
                        {
                            new Point(playButtonBounds.Left + 10, playButtonBounds.Top + 5),
                            new Point(playButtonBounds.Left + 10, playButtonBounds.Bottom - 5),
                            new Point(playButtonBounds.Right - 5, playButtonBounds.Top + buttonSize/2)
                        };
                        g.FillPolygon(brush, trianglePoints);
                    }
                    else
                    {
                        // 绘制暂停符号
                        g.DrawLine(pen, 
                            playButtonBounds.Left + 8, playButtonBounds.Top + 5,
                            playButtonBounds.Left + 8, playButtonBounds.Bottom - 5);
                        g.DrawLine(pen,
                            playButtonBounds.Right - 8, playButtonBounds.Top + 5,
                            playButtonBounds.Right - 8, playButtonBounds.Bottom - 5);
                    }
                }
                
                // 绘制帧计数器
                using (var brush = new SolidBrush(Color.Black))
                using (var pen = new Pen(Color.White, 1))
                {
                    // 绘制帧计数器背景和边框
                    g.FillRectangle(brush, frameCounterBounds);
                    g.DrawRectangle(pen, frameCounterBounds);
                    
                    // 如果没有活动的编辑框，才绘制文本
                    if (activeTextBox == null || !frameCounterBounds.Contains(activeTextBox.Location))
                    {
                        using (var font = new Font("Arial", 10,FontStyle.Bold))
                        {
                            string frameText = $"{currentFrame}";
                            var format = new StringFormat { 
                                Alignment = StringAlignment.Center, 
                                LineAlignment = StringAlignment.Center 
                            };
                            g.DrawString(frameText, font, Brushes.White, frameCounterBounds, format);
                        }
                    }
                }
                
                // 计算每帧占用的像素数
                pixelsPerFrame = (float)timelineBounds.Width / (endFrame - startFrame);
                
                // 绘制时间轴
                using (var pen = new Pen(Color.White, 1))
                {
                    // 绘制主轴线 - 确保在时间轴区域的正中间
                    g.DrawLine(pen, 
                        timelineBounds.Left, timelineBounds.Top + timelineBounds.Height/2,
                        timelineBounds.Right, timelineBounds.Top + timelineBounds.Height/2);
                    
                    // 绘制刻度线
                    for (int frame = startFrame; frame <= endFrame; frame += 10)
                    {
                        float x = timelineBounds.Left + (frame - startFrame) * pixelsPerFrame;
                        int tickHeight = frame % 50 == 0 ? 10 : 5;
                        
                        // 从中心线向上下对称绘制刻度
                        g.DrawLine(pen,
                            x, timelineBounds.Top + timelineBounds.Height/2 - tickHeight,
                            x, timelineBounds.Top + timelineBounds.Height/2 + tickHeight);
                        
                        if (frame % 50 == 0)
                        {
                            using (var font = new Font("Arial", 8))
                            {
                                // 将帧数标签绘制在下方
                                g.DrawString(frame.ToString(), font, Brushes.White,
                                    x - 10, timelineBounds.Top + timelineBounds.Height/2 + tickHeight + 2);
                            }
                        }
                    }
                    
                    // 绘制当前帧指示器和帧数
                    float currentX = timelineBounds.Left + (currentFrame - startFrame) * pixelsPerFrame;
                    using (var indicatorPen = new Pen(Color.DeepSkyBlue, 2))
                    {
                        // 绘制指示线
                        g.DrawLine(indicatorPen,
                            currentX, timelineBounds.Top,
                            currentX, timelineBounds.Bottom);
                        
                        // 获取并绘制帧数标签
                        Rectangle labelBounds = GetFrameLabelBounds(g);
                        
                        // 绘制圆角背景
                        using (var brush = new SolidBrush(Color.DeepSkyBlue))
                        using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            int radius = 4; // 圆角半径
                            path.AddArc(labelBounds.X, labelBounds.Y, radius * 2, radius * 2, 180, 90);
                            path.AddArc(labelBounds.Right - radius * 2, labelBounds.Y, radius * 2, radius * 2, 270, 90);
                            path.AddArc(labelBounds.Right - radius * 2, labelBounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                            path.AddArc(labelBounds.X, labelBounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                            path.CloseFigure();
                            
                            g.FillPath(brush, path);
                            g.DrawPath(Pens.DeepSkyBlue, path);
                        }
                        
                        // 绘制文本（居中对齐）
                        using (var font = new Font("Arial", 10))
                        {
                            string frameText = currentFrame.ToString();
                            var format = new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            };
                            g.DrawString(frameText, font, Brushes.White, labelBounds, format);
                        }
                    }
                }
                
                // 绘制起始帧和结束帧输入框
                using (var brush = new SolidBrush(Color.FromArgb(53,53,53)))
                using (var pen = new Pen(Color.White, 1))
                using (var font = new Font("Arial", 8))
                {
                    // 起始帧
                    g.FillRectangle(brush, startFrameBounds);
                    g.DrawRectangle(pen, startFrameBounds);
                    g.DrawString($"Start: {startFrame}", font, Brushes.White, startFrameBounds.X + 2, startFrameBounds.Y + 2);
                    
                    // 结束帧
                    g.FillRectangle(brush, endFrameBounds);
                    g.DrawRectangle(pen, endFrameBounds);
                    g.DrawString($"End: {endFrame}", font, Brushes.White, endFrameBounds.X + 2, endFrameBounds.Y + 2);
                }
                
                // 在时间轴上方绘制关键帧和插值线
                if (keyframes.Count > 0)
                {
                    using (var keyframePen = new Pen(Color.Orange, 2))
                    using (var interpolationPen = new Pen(Color.Orange, 1))
                    using (var keyframeBrush = new SolidBrush(Color.Orange))
                    {
                        // 计算当前最大值和最小值
                        double maxValue = keyframes.Count > 0 ? keyframes.Max(k => k.Value) : 100.0;
                        double minValue = keyframes.Count > 0 ? keyframes.Min(k => k.Value) : 0.0;
                        
                        // 向上/向下取整到10的倍数
                        maxValue = Math.Ceiling(maxValue / 10.0) * 10.0;
                        minValue = Math.Floor(minValue / 10.0) * 10.0;
                        
                        // 确保最大最小值不相等
                        if (Math.Abs(maxValue - minValue) < 1e-6)
                        {
                            maxValue += 10.0;
                            minValue -= 10.0;
                        }
                        
                        float displayTop = bounds.Top + padding + VALUE_DISPLAY_HEIGHT * 0.1f;
                        float displayBottom = timelineBounds.Top - padding;
                        
                        // 绘制显示区域边界
                        using (var boundaryPen = new Pen(Color.FromArgb(60, Color.White)))
                        {
                            // 绘制上下边界线
                            g.DrawLine(boundaryPen, 
                                timelineBounds.Left, displayTop,
                                timelineBounds.Right, displayTop);
                            g.DrawLine(boundaryPen,
                                timelineBounds.Left, displayBottom,
                                timelineBounds.Right, displayBottom);
                            
                            // 使用动态最大最小值绘制刻度
                            using (var font = new Font("Arial", 8))
                            {
                                g.DrawString(maxValue.ToString("F0"), font, Brushes.White, timelineBounds.Left - 35, displayTop);
                                g.DrawString(minValue.ToString("F0"), font, Brushes.White, timelineBounds.Left - 35, displayBottom - 8);
                            }
                        }

                        // 修改值映射函数以使用动态最大最小值
                        float MapValueToY(double value)
                        {
                            float normalizedValue = (float)((value - minValue) / (maxValue - minValue));
                            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
                            return displayBottom - (normalizedValue * (displayBottom - displayTop));
                        }

                        // 绘制插值线
                        for (int i = 0; i < keyframes.Count - 1; i++)
                        {
                            var k1 = keyframes[i];
                            var k2 = keyframes[i + 1];
                            
                            float x1 = timelineBounds.Left + (k1.Frame - startFrame) * pixelsPerFrame;
                            float x2 = timelineBounds.Left + (k2.Frame - startFrame) * pixelsPerFrame;
                            
                            float y1 = MapValueToY(k1.Value);
                            float y2 = MapValueToY(k2.Value);
                            
                            g.DrawLine(interpolationPen, x1, y1, x2, y2);
                        }
                        
                        // 绘制关键帧点
                        foreach (var keyframe in keyframes)
                        {
                            float x = timelineBounds.Left + (keyframe.Frame - startFrame) * pixelsPerFrame;
                            float y = MapValueToY(keyframe.Value);
                            
                            // 绘制关键帧点
                            g.FillEllipse(keyframeBrush, x - 4, y - 4, 8, 8);
                            
                            // 可选：显示关键帧的具体值
                            using (var font = new Font("Arial", 8))
                            {
                                string valueText = $"{keyframe.Value:F1}";
                                g.DrawString(valueText, font, Brushes.White, x + 5, y - 10);
                            }
                        }
                    }
                }
                
                g.Transform = transform;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Timeline Widget rendering error: {ex.Message}");
            }
        }

        private bool isDragging = false;
        private float dragOffset = 0;

        private void CreateEditableTextBox(Rectangle bounds, int currentValue, Action<int> onValueChanged)
        {
            // 确保先清理已存在的文本框
            RemoveActiveTextBox();
            
            try 
            {
                // 创建新的文本框
                activeTextBox = new TextBox();
                activeTextBox.Location = bounds.Location;
                activeTextBox.Size = bounds.Size;
                activeTextBox.Text = currentValue.ToString();
                activeTextBox.BorderStyle = BorderStyle.FixedSingle;
                activeTextBox.TextAlign = HorizontalAlignment.Center;
                
                // 处理按键事件
                activeTextBox.KeyDown += (s, e) => {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        if (int.TryParse(activeTextBox.Text, out int newValue))
                        {
                            onValueChanged(newValue);
                        }
                        RemoveActiveTextBox();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        RemoveActiveTextBox();
                    }
                };
                
                // 处理失去焦点事件
                activeTextBox.LostFocus += (s, e) => {
                    if (activeTextBox != null && !activeTextBox.IsDisposed)
                    {
                        if (int.TryParse(activeTextBox.Text, out int newValue))
                        {
                            onValueChanged(newValue);
                        }
                        RemoveActiveTextBox();
                    }
                };
                
                // 添加到画布
                if (Owner != null && !Owner.IsDisposed)
                {
                    Owner.Controls.Add(activeTextBox);
                    activeTextBox.Focus();
                    activeTextBox.SelectAll();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error creating text box: {ex.Message}");
                RemoveActiveTextBox();
            }
        }

        private void RemoveActiveTextBox()
        {
            if (activeTextBox == null) return;

            try
            {
                var textBox = activeTextBox;
                activeTextBox = null; // 立即设置为 null 以防止重复调用

                if (!textBox.IsDisposed)
                {
                    if (Owner != null && !Owner.IsDisposed && Owner.Controls.Contains(textBox))
                    {
                        Owner.Controls.Remove(textBox);
                    }
                    textBox.Dispose();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error removing text box: {ex.Message}");
            }
            finally
            {
                activeTextBox = null;
                if (Owner != null && !Owner.IsDisposed)
                {
                    Owner.Refresh();
                }
            }
        }

        private Rectangle GetFrameLabelBounds(Graphics g = null)
        {
            float currentX = timelineBounds.Left + (currentFrame - startFrame) * pixelsPerFrame;
            
            // 使用固定大小
            float labelX = currentX - labelSize.Width / 2;
            float labelY = timelineBounds.Top - labelSize.Height - 5;
            
            return new Rectangle(
                (int)(labelX - 2),
                (int)(labelY - 2),
                (int)(labelSize.Width + 4),
                (int)(labelSize.Height + 4)
            );
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            // 转换坐标
            PointF canvasPoint = e.CanvasLocation;
            Point screenPoint = Point.Round(sender.Viewport.ProjectPoint(canvasPoint));
            
            // 右键点击
            if (e.Button == MouseButtons.Right)
            {
                // 检查是否点击到关键帧
                selectedKeyframe = GetKeyframeAtPoint(screenPoint);
                if (selectedKeyframe != null)
                {
                    return GH_ObjectResponse.Handled;
                }
            }
            
            // 原有的左键点击处理
            if (e.Button == MouseButtons.Left)
            {
                // 使用转换后的坐标检查点击
                if (playButtonBounds.Contains(screenPoint))
                {
                    isPlaying = !isPlaying;
                    if (isPlaying)
                        animationTimer.Start();
                    else
                        animationTimer.Stop();
                        
                    sender.Refresh();
                    return GH_ObjectResponse.Handled;
                }
                
                if (frameCounterBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        frameCounterBounds,
                        currentFrame,
                        newValue => {
                            if (newValue >= startFrame && newValue <= endFrame)
                            {
                                currentFrame = newValue;
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }
                
                if (startFrameBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        startFrameBounds,
                        startFrame,
                        newValue => {
                            if (newValue < endFrame)
                            {
                                startFrame = newValue;
                                currentFrame = Math.Max(startFrame, currentFrame);
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }
                
                if (endFrameBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        endFrameBounds,
                        endFrame,
                        newValue => {
                            if (newValue > startFrame)
                            {
                                endFrame = newValue;
                                currentFrame = Math.Min(endFrame, currentFrame);
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }
                
                if (GetFrameLabelBounds().Contains(screenPoint))
                {
                    float currentX = timelineBounds.Left + (currentFrame - startFrame) * pixelsPerFrame;
                    isDragging = true;
                    dragOffset = screenPoint.X - currentX;
                    return GH_ObjectResponse.Capture;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            // 转换坐标
            PointF canvasPoint = e.CanvasLocation;
            Point screenPoint = Point.Round(sender.Viewport.ProjectPoint(canvasPoint));
            
            if (isDragging)
            {
                float pixelsPerFrame = (float)timelineBounds.Width / (endFrame - startFrame);
                int newFrame = startFrame + (int)((screenPoint.X - dragOffset - timelineBounds.Left) / pixelsPerFrame);
                newFrame = Math.Max(startFrame, Math.Min(endFrame, newFrame));
                
                if (newFrame != currentFrame)
                {
                    currentFrame = newFrame;
                    // 更新当前值
                    UpdateCurrentValue();
                    sender.Refresh();
                }
                return GH_ObjectResponse.Handled;
            }
            
            bool newHoveringState = GetFrameLabelBounds().Contains(screenPoint);
            if (newHoveringState != isHoveringIndicator)
            {
                isHoveringIndicator = newHoveringState;
                sender.Refresh();
            }
            
            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Right && selectedKeyframe != null)
            {
                // 保持选中状态，等待右键菜单处理
                return GH_ObjectResponse.Release;
            }
            
            if (isDragging)
            {
                isDragging = false;
                return GH_ObjectResponse.Release;
            }
            
            return base.RespondToMouseUp(sender, e);
        }

        // 添加双击响应
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF canvasPoint = e.CanvasLocation;
                Point screenPoint = Point.Round(sender.Viewport.ProjectPoint(canvasPoint));
                
                // 首先检查是否点击到现有的关键帧
                var clickedKeyframe = GetKeyframeAtPoint(screenPoint);
                if (clickedKeyframe != null)
                {
                    // 如果点击到现有关键帧，使用其值
                    using (var dialog = new KeyframeDialog(clickedKeyframe.Value))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            clickedKeyframe.Value = dialog.Value;
                            sender.Refresh();
                            return GH_ObjectResponse.Handled;
                        }
                    }
                }
                // 检查是否在时间轴区域内双击（创建新关键帧）
                else if (timelineBounds.Contains(screenPoint))
                {
                    // 获取当前插值的值作为默认值
                    UpdateCurrentValue(); // 确保currentValue是最新的
                    using (var dialog = new KeyframeDialog(currentValue))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // 添加新关键帧
                            var newKeyframe = new Keyframe 
                            { 
                                Frame = currentFrame,
                                Value = dialog.Value
                            };
                            
                            // 检查是否已存在该帧的关键帧
                            var existingKeyframe = keyframes.FirstOrDefault(k => k.Frame == currentFrame);
                            if (existingKeyframe != null)
                            {
                                existingKeyframe.Value = dialog.Value;
                            }
                            else
                            {
                                keyframes.Add(newKeyframe);
                                keyframes.Sort((a, b) => a.Frame.CompareTo(b.Frame));
                            }
                            
                            sender.Refresh();
                            return GH_ObjectResponse.Handled;
                        }
                    }
                }
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }

        // 辅助方法：将值映射到Y坐标
        private float MapValueToY(double value)
        {
            float displayTop = bounds.Top + padding + VALUE_DISPLAY_HEIGHT * 0.1f;
            float displayBottom = timelineBounds.Top - padding;
            
            // 计算当前最大最小值
            double maxValue = keyframes.Count > 0 ? keyframes.Max(k => k.Value) : 100.0;
            double minValue = keyframes.Count > 0 ? keyframes.Min(k => k.Value) : 0.0;
            
            // 向上/向下取整到10的倍数
            maxValue = Math.Ceiling(maxValue / 10.0) * 10.0;
            minValue = Math.Floor(minValue / 10.0) * 10.0;
            
            // 确保最大最小值不相等
            if (Math.Abs(maxValue - minValue) < 1e-6)
            {
                maxValue += 10.0;
                minValue -= 10.0;
            }
            
            float normalizedValue = (float)((value - minValue) / (maxValue - minValue));
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
            return displayBottom - (normalizedValue * (displayBottom - displayTop));
        }

        // 添加值插值计算方法
        private void UpdateCurrentValue()
        {
            if (valueParam == null) return;
            
            try 
            {
                double interpolatedValue = 0.0;
                
                if (keyframes.Count > 0)
                {
                    // 找到当前帧所在的关键帧区间
                    var nextKeyframe = keyframes.FirstOrDefault(k => k.Frame > currentFrame);
                    var prevKeyframe = keyframes.LastOrDefault(k => k.Frame <= currentFrame);
                    
                    if (prevKeyframe == null)
                    {
                        interpolatedValue = keyframes.First().Value;
                    }
                    else if (nextKeyframe == null)
                    {
                        interpolatedValue = keyframes.Last().Value;
                    }
                    else
                    {
                        // 线性插值
                        double t = (currentFrame - prevKeyframe.Frame) / (double)(nextKeyframe.Frame - prevKeyframe.Frame);
                        interpolatedValue = prevKeyframe.Value + (nextKeyframe.Value - prevKeyframe.Value) * t;
                    }

                    // 更新当前值
                    currentValue = interpolatedValue;

                    // 更新参数值
                    valueParam.PersistentData.Clear();
                    var motionValue = new MotionValueGoo(currentValue);
                    valueParam.PersistentData.Append(motionValue);
                    
                    // 强制参数更新
                    valueParam.ExpireSolution(true);
                    
                    // 请求文档更新
                    if (Owner?.Document != null)
                    {
                        Owner.Document.NewSolution(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in UpdateCurrentValue: {ex.Message}");
            }
        }

        // 在析构函数中清理
        ~TimelineWidget()
        {
            valueParam = null; // 只清除引用，不删除参数
        }
    }
}