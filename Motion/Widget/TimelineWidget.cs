﻿using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Widgets;
using Grasshopper.Kernel;
using Motion.Parameters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal partial class TimelineWidget : ScrollableControl
    {
        private GH_Canvas Owner => Instances.ActiveCanvas;
        private Dictionary<string, List<Keyframe>> _keyframeGroups = new Dictionary<string, List<Keyframe>>();
        private string _activeGroup = "Default";
        private Dictionary<string, bool> _groupVisibility = new Dictionary<string, bool>();
        private Dictionary<string, bool> _groupCollapsed = new Dictionary<string, bool>();
        private double _currentValue = 0.0;

        private bool _isPlaying = false;
        private bool _isCollapsed = false;
        private int _currentFrame = 1;
        private int _startFrame = 1;
        private int _endFrame = 250;

        private Rectangle _playButtonBounds;
        private Rectangle _frameCounterBounds;
        private Rectangle _timelineBounds;
        private Rectangle _startFrameBounds;
        private Rectangle _endFrameBounds;

        private Timer _animationTimer;
        private TextBox _activeTextBox = null;

        private bool _isHoveringIndicator = false;
        private float _pixelsPerFrame = 0;

        private bool _isDragging = false;
        private float _dragOffset = 0;

        // 添加字段来跟踪选中的关键帧
        private Keyframe _selectedKeyframe = null;

        // 添加缺失的字段定义
        private Rectangle _bounds;

        private MotionTimelineValueParam _valueParam;

        // 添加缩放相关变量
        private float _timelineZoom = 1.0f;
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 10.0f;
        private const float ZOOM_SPEED = 0.1f;

        private const int WIDGET_HEIGHT = 200;
        private const int MAX_HEIGHT = 800; // Maximum expanded height
        private const float GROUP_SPACING = 20f;
        private const float GROUP_HEADER_HEIGHT = 20f;
        private int _scrollOffset = 0;
        private int _totalHeight = 0;

        private static readonly object _initializationLock = new object();
        private bool _isInitialized = false;

        public TimelineWidget()
        {
            lock (_initializationLock)
            {
                if (_isInitialized) return;
                
                try
                {
                    // 设置控件基本属性
                    SetStyle(
                        ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.OptimizedDoubleBuffer |
                        ControlStyles.ResizeRedraw,
                        true);

                    // 初始化计时器 - 延迟初始化
                    _animationTimer = new Timer
                    {
                        Interval = 16, // 约60fps
                        Enabled = false // 默认禁用
                    };
                    _animationTimer.Tick += AnimationTimer_Tick;

                    // 添加必要的事件处理
                    HandleCreated += (s, e) => BeginInvoke(new Action(() =>
                    {
                        InitializeAfterHandleCreated();
                        Invalidate();
                    }));

                    // 设置初始大小
                    Size = new Size(800, WIDGET_HEIGHT);
                    
                    // 启用滚动功能
                    this.AutoScroll = true;
                    this.DoubleBuffered = true;
                    this.ResizeRedraw = true;
                    
                    // 注册鼠标滚轮事件
                    this.MouseWheel += TimelineWidget_MouseWheel;
                    
                    // 确保控件可以获得焦点以接收鼠标滚轮事件
                    this.SetStyle(ControlStyles.Selectable | 
                                 ControlStyles.UserMouse | 
                                 ControlStyles.StandardClick |
                                 ControlStyles.StandardDoubleClick, true);
                    
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine($"TimelineWidget initialization error: {ex.Message}");
                    Cleanup();
                    throw;
                }
            }
        }

        private void Cleanup()
        {
            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
                _animationTimer = null;
            }
            
            HandleCreated -= (s, e) => BeginInvoke(new Action(() =>
            {
                InitializeAfterHandleCreated();
                Invalidate();
            }));
        }

        private void InitializeAfterHandleCreated()
        {
            try
            {
                // 添加事件处理器
                VisibleChanged += TimelineWidget_VisibleChanged;
                MouseDown += TimelineWidget_MouseDown;
                MouseMove += TimelineWidget_MouseMove;
                MouseUp += TimelineWidget_MouseUp;
                MouseWheel += TimelineWidget_MouseWheel;
                DoubleClick += TimelineWidget_DoubleClick;
                DockSideChanged += OnDockSideChanged;

                if (Parent != null)
                {
                    Parent.SizeChanged += (s, e) => BeginInvoke(new Action(UpdateBounds));
                }

                UpdateBounds();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"TimelineWidget post-initialization error: {ex.Message}");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(() =>
            {
                Rhino.RhinoApp.WriteLine("TimelineWidget Handle Created (Override)");
                Invalidate();
            }));
        }

        private int CalculateTotalHeight()
        {
            int totalHeight = (int)GROUP_HEADER_HEIGHT;
            
            foreach (var group in _keyframeGroups)
            {
                // 如果组是可见的
                if (_groupVisibility[group.Key])
                {
                    // 添加组标题的高度
                    totalHeight += (int)GROUP_HEADER_HEIGHT;
                    
                    // 如果组未折叠，添加组内容的高度
                    if (!(_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                    {
                        totalHeight += (int)VALUE_DISPLAY_HEIGHT + (int)GROUP_SPACING;
                    }
                }
            }
            
            // 确保总高度至少等于最小高度
            return Math.Max(totalHeight, WIDGET_HEIGHT);
        }

        private void UpdateBounds()
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                if (Owner?.Viewport == null || Parent == null) return;

                var parentBounds = Parent.ClientRectangle;
                
                // 计算所需的总高度
                int totalHeight = CalculateTotalHeight();
                
                // 设置控件的最小尺寸（用于滚动）
                this.AutoScrollMinSize = new Size(parentBounds.Width, totalHeight);
                
                // 设置控件的实际尺寸
                Size = new Size(parentBounds.Width, Math.Min(totalHeight, parentBounds.Height));
                
                // 更新位置
                Location = m_dockSide == TimelineWidgetDock.Top
                    ? new Point(0, WIDGET_HEIGHT + 130)
                    : new Point(0, parentBounds.Height - Height);

                _bounds = new Rectangle(Location, Size);
                CalculateBounds();

                BeginInvoke(new Action(Invalidate));
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"UpdateBounds error: {ex.Message}");
            }
        }

        private void UpdateScrollPosition(int delta)
        {
            int newOffset = this.AutoScrollPosition.Y - delta;
            newOffset = Math.Max(0, Math.Min(_totalHeight - ClientSize.Height, newOffset));
            this.AutoScrollPosition = new Point(0, newOffset);
            Invalidate();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null)
            {
                Parent.SizeChanged += (s, args) => UpdateBounds();
                UpdateBounds();
            }
        }

        private bool IsOverCurrentFrameHandle(Point location)
        {
            float currentX = _timelineBounds.Left + (_currentFrame - _startFrame) * _pixelsPerFrame;

            // 获取标签的边界
            Rectangle labelBounds = GetFrameLabelBounds(null);

            // 只检查标签区域
            return labelBounds.Contains(location);
        }

        private void TimelineWidget_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            // 处理组和按钮的点击
            var (clickedGroup, isCollapseButton, isVisibilityButton) = GetClickedGroupWithButtons(e.Location);
            if (clickedGroup != null)
            {
                if (isCollapseButton)
                {
                    // 切换组的折叠状态
                    if (!_groupCollapsed.ContainsKey(clickedGroup))
                    {
                        _groupCollapsed[clickedGroup] = false;
                    }
                    _groupCollapsed[clickedGroup] = !_groupCollapsed[clickedGroup];
                    Invalidate();
                    return;
                }
                
                if (isVisibilityButton)
                {
                    // 切换组的可见性
                    _groupVisibility[clickedGroup] = !_groupVisibility[clickedGroup];
                    Invalidate();
                    return;
                }

                // 设置活动组
                _activeGroup = clickedGroup;
                Invalidate();
                return;
            }

            // 处理关键帧点击
            var clickedKeyframe = GetKeyframeAtPoint(e.Location);
            if (clickedKeyframe != null)
            {
                _selectedKeyframe = clickedKeyframe;
                Invalidate();
                return;
            }

            // 其他控件的点击处理保持不变
            if (_playButtonBounds.Contains(e.Location))
            {
                TogglePlayState();
                return;
            }

            // 处理帧计数器点击
            if (_frameCounterBounds.Contains(e.Location))
            {
                HandleFrameCounterClick();
                return;
            }

            // 处理起始帧和结束帧边界点击
            if (_startFrameBounds.Contains(e.Location))
            {
                HandleStartFrameClick();
                return;
            }

            if (_endFrameBounds.Contains(e.Location))
            {
                HandleEndFrameClick();
                return;
            }

            // 处理导航按钮点击
            if (_startFrameJumpBounds.Contains(e.Location))
            {
                HandleStartFrameJump();
                Invalidate();
                return;
            }

            if (_prevKeyframeBounds.Contains(e.Location))
            {
                HandlePrevKeyframe();
                Invalidate();
                return;
            }

            if (_nextKeyframeBounds.Contains(e.Location))
            {
                HandleNextKeyframe();
                Invalidate();
                return;
            }

            if (_endFrameJumpBounds.Contains(e.Location))
            {
                HandleEndFrameJump();
                Invalidate();
                return;
            }

            // 处理当前帧指示器拖动
            if (IsOverCurrentFrameHandle(e.Location))
            {
                StartDragging(e.X);
                return;
            }
        }

        private (string group, bool isCollapseButton, bool isVisibilityButton) GetClickedGroupWithButtons(Point location)
        {
            float yOffset = _timelineBounds.Top;
            foreach (var group in _keyframeGroups)
            {
                // 计算组的总高度
                float groupHeight = GROUP_HEADER_HEIGHT;
                if (!(_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                {
                    groupHeight += GROUP_SPACING;
                }
                
                // 组的整体区域
                var groupRect = new RectangleF(
                    _timelineBounds.Left,
                    yOffset,
                    _timelineBounds.Width,
                    groupHeight
                );

                if (groupRect.Contains(location))
                {
                    // 检查折叠按钮（左侧）
                    var collapseRect = new RectangleF(
                        groupRect.Left + 5,
                        yOffset + (GROUP_HEADER_HEIGHT - 15) / 2,
                        15,
                        15
                    );

                    // 检查可见性按钮（右侧）
                    var visibilityRect = new RectangleF(
                        groupRect.Right - 25,
                        yOffset + (GROUP_HEADER_HEIGHT - 15) / 2,
                        15,
                        15
                    );

                    return (
                        group.Key,
                        collapseRect.Contains(location),
                        visibilityRect.Contains(location)
                    );
                }

                yOffset += groupHeight;
            }
            return (null, false, false);
        }

        

        // 辅助方法，处理播放状态切换
        private void TogglePlayState()
        {
            _isPlaying = !_isPlaying;
            if (_isPlaying)
                _animationTimer.Start();
            else
                _animationTimer.Stop();
            Invalidate();
        }

        // 辅助方法，处理帧计数器点击
        private void HandleFrameCounterClick()
        {
            CreateEditableTextBox(
                _frameCounterBounds,
                _currentFrame,
                newValue =>
                {
                    if (newValue >= _startFrame && newValue <= _endFrame)
                    {
                        _currentFrame = newValue;
                        UpdateCurrentValue();
                        Invalidate();
                    }
                }
            );
        }

        // 辅助方法，开始拖动操作
        private void StartDragging(int mouseX)
        {
            _isDragging = true;
            _dragOffset = mouseX - (_timelineBounds.Left + (_currentFrame - _startFrame) * _pixelsPerFrame);
            Capture = true;
            Invalidate();
        }

        private void TimelineWidget_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                try
                {
                    // 计算新的帧位置
                    float relativeX = e.X - _dragOffset - _timelineBounds.Left;
                    int newFrame = _startFrame + (int)(relativeX / _pixelsPerFrame);

                    // 确保在有效范围内
                    newFrame = Math.Max(_startFrame, Math.Min(_endFrame, newFrame));

                    if (newFrame != _currentFrame)
                    {
                        _currentFrame = newFrame;
                        UpdateCurrentValue();
                        Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine($"Error in timeline drag: {ex.Message}");
                }
            }
        }
        private void TimelineWidget_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                Capture = false;  // 释放鼠标捕获
                Invalidate();
            }
        }
        private void TimelineWidget_MouseWheel(object sender, MouseEventArgs e)
        {
            // 如果按住Ctrl键，则进行缩放
            if (ModifierKeys == Keys.Control)
            {
                float zoomDelta = e.Delta > 0 ? ZOOM_SPEED : -ZOOM_SPEED;
                _timelineZoom = Math.Max(MIN_ZOOM, Math.Min(_timelineZoom + zoomDelta, MAX_ZOOM));
                Invalidate();
            }
            // 否则进行垂直滚动
            else
            {
                int scrollValue = -e.Delta / 120 * SystemInformation.MouseWheelScrollLines * 20; // 调整滚动速度
                int newY = -AutoScrollPosition.Y + scrollValue;
                
                // 确保不会滚动超出范围
                newY = Math.Max(0, Math.Min(newY, AutoScrollMinSize.Height - ClientSize.Height));
                
                AutoScrollPosition = new Point(0, newY);
                Invalidate();
            }

            // 阻止事件继续传播
            ((HandledMouseEventArgs)e).Handled = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Focus();
        }
        private void TimelineWidget_DoubleClick(object sender, EventArgs e)
        {
            Point mousePoint = PointToClient(Control.MousePosition);

            // 获取点击位置所在的组
            float yOffset = _timelineBounds.Top;
            string clickedGroupName = null;
            float clickedAreaTop = 0;

            foreach (var group in _keyframeGroups)
            {
                float groupHeight = GROUP_HEADER_HEIGHT;
                if (!(_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                {
                    groupHeight += GROUP_SPACING;
                }

                var groupArea = new RectangleF(
                    _timelineBounds.Left,
                    yOffset,
                    _timelineBounds.Width,
                    groupHeight
                );

                if (groupArea.Contains(mousePoint))
                {
                    clickedGroupName = group.Key;
                    clickedAreaTop = yOffset;
                    break;
                }

                yOffset += groupHeight;
            }

            if (clickedGroupName == null)
                return;

            // 确保点击在关键帧区域而不是标题区域
            if (mousePoint.Y < clickedAreaTop + GROUP_HEADER_HEIGHT)
                return;

            // 计算点击位置对应的帧
            float relativeX = mousePoint.X - _timelineBounds.Left;
            int clickedFrame = _startFrame + (int)(relativeX / _pixelsPerFrame);

            // 查找是否点击了现有关键帧
            var clickedKeyframe = GetKeyframeAtPoint(mousePoint);
            double initialValue = clickedKeyframe?.Value ?? _currentValue;

            // 显示关键帧编辑对话框
            using (var dialog = new KeyframeDialog(initialValue))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _currentFrame = clickedFrame;
                    UpdateKeyframe(clickedKeyframe, dialog.Value);
                    UpdateCurrentValue();
                    Invalidate();
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                _bounds = ClientRectangle;
                if (_bounds.IsEmpty) return;

                CalculateBounds();

                DrawBackground(g);
               

                if (!_isCollapsed)
                {
                    DrawPlayButton(g);
                    DrawNavigationButtons(g);
                    DrawFrameCounter(g);
                    DrawTimeline(g);
                    DrawFrameInputs(g);
                }
                
                // Draw each visible group
                float groupOffset = 0;
                foreach (var group in _keyframeGroups)
                {
                    if (_groupVisibility[group.Key])
                    {
                        DrawKeyframesAndInterpolation(g);
                        groupOffset += GROUP_SPACING;
                    }
                }

                // 强制立即更新显示
                if (_isPlaying)
                {
                    Update();
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Timeline Widget rendering error: {ex.Message}");
            }
        }

        private static TimelineWidgetDock m_dockSide = (TimelineWidgetDock)Instances.Settings.GetValue("Motion.Widget.Timeline.Side", 1);
        private static bool m_showWidget = Instances.Settings.GetValue("Motion.Widget.Timeline.Show", true);


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
                    onDockSideChanged?.Invoke();
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
                    onWidgetVisibleChanged?.Invoke();
                }
            }
        }

        public bool Visible
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

        public bool Contains(Point pt_control, PointF pt_canvas)
        {
            if (Owner.Document == null)
            {
                return false;
            }
            // 扩大点击检测区域
            var area = WidgetArea;
            area.Inflate(5, 5);  // 向外扩展5个像素
            return area.Contains(pt_control);
        }
        private void UpdateKeyframe(Keyframe existingKeyframe, double newValue)
        {
            // 确保活动组存在
            if (!_keyframeGroups.ContainsKey(_activeGroup))
            {
                _keyframeGroups[_activeGroup] = new List<Keyframe>();
                _groupVisibility[_activeGroup] = true;
            }

            if (existingKeyframe != null)
            {
                // 更新现有关键帧
                existingKeyframe.Value = newValue;
                return;
            }

            // 添加新关键帧
            var newKeyframe = new Keyframe(_currentFrame, newValue, _activeGroup);

            // 检查是否已存在该帧的关键帧
            var frameKeyframe = _keyframeGroups[_activeGroup].FirstOrDefault(k => k.Frame == _currentFrame);
            if (frameKeyframe != null)
            {
                frameKeyframe.Value = newValue;
                return;
            }

            _keyframeGroups[_activeGroup].Add(newKeyframe);
            _keyframeGroups[_activeGroup].Sort((a, b) => a.Frame.CompareTo(b.Frame));
        }

        // 添加一个辅助方法来确保组存在
        private void EnsureGroupExists(string groupName)
        {
            if (!_keyframeGroups.ContainsKey(groupName))
            {
                _keyframeGroups[groupName] = new List<Keyframe>();
                _groupVisibility[groupName] = true;
            }
        }

        private void AddGroup(string groupName)
        {
            // 确保组名唯一
            string uniqueName = groupName;
            int counter = 1;
            while (_keyframeGroups.ContainsKey(uniqueName))
            {
                uniqueName = $"{groupName}{counter}";
                counter++;
            }

            // 添加新组
            _keyframeGroups[uniqueName] = new List<Keyframe>();
            _groupVisibility[uniqueName] = true;
            _groupCollapsed[uniqueName] = false;
            _activeGroup = uniqueName;

            // 更新滚动范围
            _totalHeight = CalculateTotalHeight();
            this.AutoScrollMinSize = new Size(0, _totalHeight);
            
            // 强制重绘
            Invalidate();
        }

        private void RemoveGroup(string groupName)
        {
            if (_keyframeGroups.ContainsKey(groupName))
            {
                _keyframeGroups.Remove(groupName);
                _groupVisibility.Remove(groupName);
                
                if (_activeGroup == groupName)
                {
                    _activeGroup = "Default";
                }
            }
        }

        private void ToggleGroupVisibility(string groupName)
        {
            if (_groupVisibility.ContainsKey(groupName))
            {
                _groupVisibility[groupName] = !_groupVisibility[groupName];
            }
        }

        private void TimelineWidget_HandleCreated(object sender, EventArgs e)
        {
            Rhino.RhinoApp.WriteLine("TimelineWidget Handle Created");
            Invalidate();
        }

        private void TimelineWidget_VisibleChanged(object sender, EventArgs e)
        {
            Rhino.RhinoApp.WriteLine($"TimelineWidget Visibility Changed: {Visible}");
            if (Visible)
            {
                Invalidate();
            }
        }

        private void UpdateCurrentValue()
        {
            try
            {
                if (_valueParam != null)
                {
                    double interpolatedValue = 0.0;

                    // 获取当前活动组的关键帧
                    if (_keyframeGroups.ContainsKey(_activeGroup) && _keyframeGroups[_activeGroup].Count > 0)
                    {
                        var activeKeyframes = _keyframeGroups[_activeGroup];
                        
                        // 找到当前帧所在的关键帧区间
                        var nextKeyframe = activeKeyframes.FirstOrDefault(k => k.Frame > _currentFrame);
                        var prevKeyframe = activeKeyframes.LastOrDefault(k => k.Frame <= _currentFrame);

                        if (prevKeyframe == null)
                        {
                            interpolatedValue = activeKeyframes.First().Value;
                        }
                        else if (nextKeyframe == null)
                        {
                            interpolatedValue = activeKeyframes.Last().Value;
                        }
                        else
                        {
                            // 线性插值
                            double t = (_currentFrame - prevKeyframe.Frame) / (double)(nextKeyframe.Frame - prevKeyframe.Frame);
                            interpolatedValue = prevKeyframe.Value + (nextKeyframe.Value - prevKeyframe.Value) * t;
                        }

                        // 更新当前值
                        _currentValue = interpolatedValue;

                        // 更新参数值
                        _valueParam.PersistentData.Clear();
                        var motionValue = new MotionTimelineValueGoo(_currentValue);
                        _valueParam.PersistentData.Append(motionValue);

                        // 强制参数更新
                        _valueParam.ExpireSolution(true);

                        // 请求文档更新
                        if (Owner?.Document != null)
                        {
                            Owner.Document.NewSolution(false);
                        }
                    }
                }

                Invalidate();
                Update();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error updating current value: {ex.Message}");
            }
        }

        // 在析构函数中清理
        ~TimelineWidget()
        {
            _valueParam = null; // 只清除引用，不删除参数
            DockSideChanged -= OnDockSideChanged;
            DoubleClick -= TimelineWidget_DoubleClick;
        }

        public void RequestRepaint()
        {
            Invalidate();  // 只需要这一行就够了
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Rhino.RhinoApp.WriteLine($"TimelineWidget Resized: {Width}x{Height}");
            Invalidate();
        }

        public override void Refresh()
        {
            base.Refresh();
            Rhino.RhinoApp.WriteLine("TimelineWidget Refresh Called");
            Invalidate();
        }

        private void OnDockSideChanged()
        {
            if (Parent != null)
            {
                var parentBounds = Parent.ClientRectangle;

                // 根据停靠位置设置控件位置
                if (m_dockSide == TimelineWidgetDock.Top)
                {
                    Location = new Point(0, 0);
                }
                else // Bottom
                {
                    Location = new Point(0, parentBounds.Height - WIDGET_HEIGHT);
                }

                // 更新大小和边界
                Size = new Size(parentBounds.Width, WIDGET_HEIGHT);
                UpdateBounds();
                Invalidate();
            }
        }

        // 重写 CreateParams 以确保滚动条正确显示
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x200000;  // WS_VSCROLL
                return cp;
            }
        }
    }
}
