using Grasshopper;
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
    internal partial class TimelineWidget : GH_Widget
    {
        private List<Keyframe> _keyframes = new List<Keyframe>();
        private double _currentValue = 0.0;

        private bool _isPlaying = false;
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

        public TimelineWidget()
        {
            _animationTimer = new Timer();
            _animationTimer.Interval = 50;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public override string Name => "Timeline Widget";

        public override string Description => "Motion Animation Timeline widget";

        public override Bitmap Icon_24x24 => null;

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

        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e) { }

        // 重写右键菜单方法
        public override void AppendToMenu(ToolStripDropDownMenu menu)
        {
            // 如果有选中的关键帧，添加删除选项
            if (_selectedKeyframe != null)
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
                if (_keyframes.Count > 0)
                {
                    GH_DocumentObject.Menu_AppendSeparator(menu);
                    if (_valueParam != null)
                    {
                        GH_DocumentObject.Menu_AppendItem(menu, "Add Motion Value Parameter", Menu_AddMotionValue);
                    }
                }
            }
        }

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
            // 初始化检查
            if (!InitializeRender(canvas)) return;

            try
            {
                Graphics g = canvas.Graphics;
                var transform = g.Transform;
                g.ResetTransform();

                _bounds = WidgetArea;
                if (_bounds.IsEmpty) return;

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 计算各个区域的边界
                CalculateBounds();

                // 绘制各个部分
                DrawBackground(g);
                DrawPlayButton(g);
                DrawFrameCounter(g);
                DrawTimeline(g);
                DrawFrameInputs(g);
                DrawKeyframesAndInterpolation(g);

                g.Transform = transform;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Timeline Widget rendering error: {ex.Message}");
            }
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
                _selectedKeyframe = GetKeyframeAtPoint(screenPoint);
                if (_selectedKeyframe != null)
                {
                    return GH_ObjectResponse.Handled;
                }
            }

            // 原有的左键点击处理
            if (e.Button == MouseButtons.Left)
            {
                // 使用转换后的坐标检查点击
                if (_playButtonBounds.Contains(screenPoint))
                {
                    _isPlaying = !_isPlaying;
                    if (_isPlaying)
                        _animationTimer.Start();
                    else
                        _animationTimer.Stop();

                    sender.Refresh();
                    return GH_ObjectResponse.Handled;
                }

                if (_frameCounterBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        _frameCounterBounds,
                        _currentFrame,
                        newValue =>
                        {
                            if (newValue >= _startFrame && newValue <= _endFrame)
                            {
                                _currentFrame = newValue;
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }

                if (_startFrameBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        _startFrameBounds,
                        _startFrame,
                        newValue =>
                        {
                            if (newValue < _endFrame)
                            {
                                _startFrame = newValue;
                                _currentFrame = Math.Max(_startFrame, _currentFrame);
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }

                if (_endFrameBounds.Contains(screenPoint))
                {
                    CreateEditableTextBox(
                        _endFrameBounds,
                        _endFrame,
                        newValue =>
                        {
                            if (newValue > _startFrame)
                            {
                                _endFrame = newValue;
                                _currentFrame = Math.Min(_endFrame, _currentFrame);
                                sender.Refresh();
                            }
                        }
                    );
                    return GH_ObjectResponse.Handled;
                }

                if (GetFrameLabelBounds().Contains(screenPoint))
                {
                    float currentX = _timelineBounds.Left + (_currentFrame - _startFrame) * _pixelsPerFrame;
                    _isDragging = true;
                    _dragOffset = screenPoint.X - currentX;
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

            if (_isDragging)
            {
                float pixelsPerFrame = (float)_timelineBounds.Width / (_endFrame - _startFrame);
                int newFrame = _startFrame + (int)((screenPoint.X - _dragOffset - _timelineBounds.Left) / pixelsPerFrame);
                newFrame = Math.Max(_startFrame, Math.Min(_endFrame, newFrame));

                if (newFrame != _currentFrame)
                {
                    _currentFrame = newFrame;
                    // 更新当前值
                    UpdateCurrentValue();
                    sender.Refresh();
                }
                return GH_ObjectResponse.Handled;
            }

            bool newHoveringState = GetFrameLabelBounds().Contains(screenPoint);
            if (newHoveringState != _isHoveringIndicator)
            {
                _isHoveringIndicator = newHoveringState;
                sender.Refresh();
            }

            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Right && _selectedKeyframe != null)
            {
                // 保持选中状态，等待右键菜单处理
                return GH_ObjectResponse.Release;
            }

            if (_isDragging)
            {
                _isDragging = false;
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
                else if (_timelineBounds.Contains(screenPoint))
                {
                    // 获取当前插值的值作为默认值
                    UpdateCurrentValue();
                    using (var dialog = new KeyframeDialog(_currentValue))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // 添加新关键帧
                            var newKeyframe = new Keyframe
                            {
                                Frame = _currentFrame,
                                Value = dialog.Value
                            };

                            // 检查是否已存在该帧的关键帧
                            var existingKeyframe = _keyframes.FirstOrDefault(k => k.Frame == _currentFrame);
                            if (existingKeyframe != null)
                            {
                                existingKeyframe.Value = dialog.Value;
                            }
                            else
                            {
                                _keyframes.Add(newKeyframe);
                                _keyframes.Sort((a, b) => a.Frame.CompareTo(b.Frame));
                            }

                            sender.Refresh();
                            return GH_ObjectResponse.Handled;
                        }
                    }
                }
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }
        public GH_ObjectResponse RespondToMouseWheel(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            // 转换坐标
            PointF canvasPoint = e.CanvasLocation;
            Point screenPoint = Point.Round(sender.Viewport.ProjectPoint(canvasPoint));

            // 检查鼠标是否在时间轴区域内
            if (_timelineBounds.Contains(screenPoint))
            {
                // 计算鼠标位置对应的帧
                float mouseFrame = _startFrame + (screenPoint.X - _timelineBounds.Left) / _pixelsPerFrame;

                // 根据滚轮方向调整缩放
                if (e.Delta > 0)
                {
                    _timelineZoom = Math.Min(_timelineZoom * (1 + ZOOM_SPEED), MAX_ZOOM);
                }
                else
                {
                    _timelineZoom = Math.Max(_timelineZoom / (1 + ZOOM_SPEED), MIN_ZOOM);
                }

                // 调整可见帧范围，保持鼠标位置对应的帧不变
                int visibleFrames = (int)((_endFrame - _startFrame) / _timelineZoom);
                int newStartFrame = (int)(mouseFrame - (mouseFrame - _startFrame) / _timelineZoom);
                int newEndFrame = newStartFrame + visibleFrames;

                // 确保范围有效
                if (newStartFrame < 1)
                {
                    newStartFrame = 1;
                    newEndFrame = newStartFrame + visibleFrames;
                }

                _startFrame = newStartFrame;
                _endFrame = newEndFrame;

                sender.Refresh();
                return GH_ObjectResponse.Handled;
            }

            return GH_ObjectResponse.Ignore;
        }
        // 添加值插值计算方法
        private void UpdateCurrentValue()
        {
            if (_valueParam == null) return;

            try
            {
                double interpolatedValue = 0.0;

                if (_keyframes.Count > 0)
                {
                    // 找到当前帧所在的关键帧区间
                    var nextKeyframe = _keyframes.FirstOrDefault(k => k.Frame > _currentFrame);
                    var prevKeyframe = _keyframes.LastOrDefault(k => k.Frame <= _currentFrame);

                    if (prevKeyframe == null)
                    {
                        interpolatedValue = _keyframes.First().Value;
                    }
                    else if (nextKeyframe == null)
                    {
                        interpolatedValue = _keyframes.Last().Value;
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
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in UpdateCurrentValue: {ex.Message}");
            }
        }

        // 在析构函数中清理
        ~TimelineWidget()
        {
            _valueParam = null; // 只清除引用，不删除参数
        }


    }
}