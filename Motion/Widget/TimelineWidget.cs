﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Grasshopper;
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
    internal partial class TimelineWidget : Control
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
        private const float GROUP_SPACING = 30f;
        private const float GROUP_HEADER_HEIGHT = 20f;

        public TimelineWidget()
        {
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
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"TimelineWidget initialization error: {ex.Message}");
            }
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

        private void UpdateBounds()
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                if (Owner?.Viewport == null || Parent == null) return;

                var parentBounds = Parent.ClientRectangle;
                int height = _isCollapsed ? 30 : WIDGET_HEIGHT;
                Size = new Size(parentBounds.Width, height);
                Location = m_dockSide == TimelineWidgetDock.Top
                    ? new Point(0, WIDGET_HEIGHT + 130)
                    : new Point(0, parentBounds.Height - height);

                _bounds = new Rectangle(Location, Size);
                CalculateBounds();

                BeginInvoke(new Action(Invalidate));
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"UpdateBounds error: {ex.Message}");
            }
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

            // Check if clicked on collapse/expand button
            var collapseButtonRect = new Rectangle(
                _bounds.Right - 30,
                _bounds.Top + 5,
                20,
                20
            );

            if (collapseButtonRect.Contains(e.Location))
            {
                _isCollapsed = !_isCollapsed;
                UpdateBounds();
                Invalidate();
                return;
            }

            // 处理播放按钮点击
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

            // 处理组头部点击
            string clickedGroup = GetClickedGroup(e.Location);
            if (clickedGroup != null)
            {
                ToggleGroupVisibility(clickedGroup);
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

        private string GetClickedGroup(Point location)
        {
            float yOffset = 0;
            foreach (var group in _keyframeGroups)
            {
                var headerRect = new RectangleF(
                    _timelineBounds.Left,
                    _timelineBounds.Top + yOffset,
                    _timelineBounds.Width,
                    GROUP_HEADER_HEIGHT
                );

                // Check if clicked on collapse/expand button
                var collapseRect = new RectangleF(
                    headerRect.Right - 50,
                    headerRect.Top + (headerRect.Height - 15) / 2,
                    15,
                    15
                );

                if (collapseRect.Contains(location))
                {
                    // Toggle collapsed state
                    if (_groupCollapsed.ContainsKey(group.Key))
                    {
                        _groupCollapsed[group.Key] = !_groupCollapsed[group.Key];
                    }
                    else
                    {
                        _groupCollapsed[group.Key] = true;
                    }
                    return group.Key;
                }

                // Check if clicked on visibility toggle
                var visibilityRect = new RectangleF(
                    headerRect.Right - 25,
                    headerRect.Top + (headerRect.Height - 15) / 2,
                    15,
                    15
                );

                if (visibilityRect.Contains(location))
                {
                    return group.Key;
                }

                yOffset += GROUP_HEADER_HEIGHT;
                if (_groupVisibility[group.Key] && !(_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                {
                    yOffset += GROUP_SPACING;
                }
            }
            return null;
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
            // 只在按下CTRL键时进行缩放
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // 计算新的缩放级别
                float newZoom = e.Delta > 0
                    ? Math.Max(_timelineZoom / (1 + ZOOM_SPEED), MIN_ZOOM)
                    : Math.Min(_timelineZoom * (1 + ZOOM_SPEED), MAX_ZOOM);

                // 更新缩放级别
                _timelineZoom = newZoom;

                // 重新计算像素每帧的值
                _pixelsPerFrame = _timelineBounds.Width / (float)(_endFrame - _startFrame) * _timelineZoom;

                Invalidate();
            }
        }
        private void TimelineWidget_DoubleClick(object sender, EventArgs e)
        {
            Point mousePoint = PointToClient(Control.MousePosition);

            // 检查是否双击在某个组区域内
            string groupName = GetClickedGroup(mousePoint);
            if (groupName == null)
                return;

            // 定义关键帧显示区域
            float displayTop = _bounds.Top + PADDING + VALUE_DISPLAY_HEIGHT * 0.1f;
            float displayBottom = _timelineBounds.Top - PADDING;
            RectangleF keyframeDisplayArea = new RectangleF(
                _timelineBounds.Left,
                displayTop,
                _timelineBounds.Width,
                displayBottom - displayTop
            );

            // 检查点击是否在关键帧显示区域内
            if (!keyframeDisplayArea.Contains(mousePoint))
                return;

            // 检查是否点击到现有的关键帧
            var clickedKeyframe = GetKeyframeAtPoint(mousePoint);
            double initialValue = clickedKeyframe?.Value ?? _currentValue;

            // 显示关键帧编辑对话框
            using (var dialog = new KeyframeDialog(initialValue))
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                UpdateKeyframe(clickedKeyframe, dialog.Value);
                UpdateCurrentValue();
                Invalidate();
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
                
                // Draw collapse/expand button
                var collapseButtonRect = new Rectangle(
                    _bounds.Right - 30,
                    _bounds.Top + 5,
                    20,
                    20
                );
                
                using (var pen = new Pen(Color.White, 2))
                {
                    // Draw minus/plus sign based on collapsed state
                    g.DrawLine(pen,
                        collapseButtonRect.Left + 2,
                        collapseButtonRect.Top + collapseButtonRect.Height/2,
                        collapseButtonRect.Right - 2,
                        collapseButtonRect.Top + collapseButtonRect.Height/2);
                    
                    if (_isCollapsed)
                    {
                        g.DrawLine(pen,
                            collapseButtonRect.Left + collapseButtonRect.Width/2,
                            collapseButtonRect.Top + 2,
                            collapseButtonRect.Left + collapseButtonRect.Width/2,
                            collapseButtonRect.Bottom - 2);
                    }
                }

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
                        DrawGroupHeader(g, group.Key, ref groupOffset);
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
            if (!_keyframeGroups.ContainsKey(groupName))
            {
                _keyframeGroups[groupName] = new List<Keyframe>();
                _groupVisibility[groupName] = true;
            }
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
    }
}
