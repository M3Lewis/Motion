using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Widgets;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Motion.Widgets
{
    internal class TimelineWidget : GH_Widget
    {
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

        public TimelineWidget()
        {
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50; // 20fps
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (currentFrame >= endFrame)
            {
                isPlaying = false;
                animationTimer.Stop();
            }
            else
            {
                currentFrame++;
            }
            Owner?.Refresh();
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
                const int height = 120;  // 边界区域固定高度
                
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

        public override void AppendToMenu(ToolStripDropDownMenu menu)
        {
            base.AppendToMenu(menu);
            GH_DocumentObject.Menu_AppendSeparator(menu);
            GH_DocumentObject.Menu_AppendItem(menu, "Top", Menu_DockTop, enabled: true, m_dockSide == TimelineWidgetDock.Top);
            GH_DocumentObject.Menu_AppendItem(menu, "Bottom", Menu_DockBottom, enabled: true, m_dockSide == TimelineWidgetDock.Bottom);
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
                        
                        // 绘制背景
                        using (var brush = new SolidBrush(Color.White))
                        {
                            g.FillRectangle(brush, labelBounds);
                        }
                        
                        // 绘制边框
                        g.DrawRectangle(Pens.Black, labelBounds);
                        
                        // 绘制文本（居中对齐）
                        using (var font = new Font("Arial", 10))
                        {
                            string frameText = currentFrame.ToString();
                            var format = new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            };
                            g.DrawString(frameText, font, Brushes.Black, labelBounds, format);
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
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // 转换坐标
                PointF canvasPoint = e.CanvasLocation;
                Point screenPoint = Point.Round(sender.Viewport.ProjectPoint(canvasPoint));
                
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
            if (isDragging)
            {
                isDragging = false;
                return GH_ObjectResponse.Release;
            }
            return base.RespondToMouseUp(sender, e);
        }
    }
}