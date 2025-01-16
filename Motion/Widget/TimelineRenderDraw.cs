using Grasshopper.GUI.Canvas;
using System;
using System.Drawing;
using System.Linq;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        // 添加常量定义
        private const int BUTTON_SIZE = 30;
        private const int PADDING = 10;
        private const float VALUE_DISPLAY_HEIGHT = 60;
        private const int TEXT_BOX_HEIGHT = 20;

        private bool InitializeRender(GH_Canvas canvas)
        {
            if (!Visible || canvas?.Graphics == null) return false;

            // 如果还没有关联参数，尝试查找现有的
            if (_valueParam == null)
            {
                _valueParam = FindExistingMotionValue();
            }

            // 在第一次渲染时初始化参数
            InitializeTimelineValueParameter();

            return true;
        }

        private void CalculateBounds()
        {
            // 使用常量替换硬编码值
            _playButtonBounds = new Rectangle(
                _bounds.Left + PADDING,
                _bounds.Top + (_bounds.Height - BUTTON_SIZE) / 2,
                BUTTON_SIZE,
                BUTTON_SIZE);

            _frameCounterBounds = new Rectangle(
                _playButtonBounds.Right + PADDING,
                _bounds.Top + (_bounds.Height - BUTTON_SIZE) / 2,
                80,
                BUTTON_SIZE);

            _timelineBounds = new Rectangle(
                _frameCounterBounds.Right + PADDING,
                _bounds.Top + _bounds.Height / 2 - 15,
                _bounds.Right - _frameCounterBounds.Right - PADDING * 3,
                30);

            _startFrameBounds = new Rectangle(
                _timelineBounds.Right - 160,
                _bounds.Top + PADDING,
                70,
                TEXT_BOX_HEIGHT);

            _endFrameBounds = new Rectangle(
                _timelineBounds.Right - 80,
                _bounds.Top + PADDING,
                70,
                TEXT_BOX_HEIGHT);
        }

        private void DrawBackground(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(53, 53, 53)))
            {
                g.FillRectangle(brush, _bounds);
            }
        }

        private void DrawPlayButton(Graphics g)
        {
            using (var pen = new Pen(Color.White, 2))
            using (var brush = new SolidBrush(Color.White))
            {
                if (!_isPlaying)
                {
                    // 绘制播放三角形
                    Point[] trianglePoints = new Point[]
                    {
                        new Point(_playButtonBounds.Left + 10, _playButtonBounds.Top + 5),
                        new Point(_playButtonBounds.Left + 10, _playButtonBounds.Bottom - 5),
                        new Point(_playButtonBounds.Right - 5, _playButtonBounds.Top + BUTTON_SIZE/2)
                    };
                    g.FillPolygon(brush, trianglePoints);
                }
                else
                {
                    // 绘制暂停符号
                    g.DrawLine(pen,
                        _playButtonBounds.Left + 8, _playButtonBounds.Top + 5,
                        _playButtonBounds.Left + 8, _playButtonBounds.Bottom - 5);
                    g.DrawLine(pen,
                        _playButtonBounds.Right - 8, _playButtonBounds.Top + 5,
                        _playButtonBounds.Right - 8, _playButtonBounds.Bottom - 5);
                }
            }
        }

        private void DrawFrameCounter(Graphics g)
        {
            using (var brush = new SolidBrush(Color.Black))
            using (var pen = new Pen(Color.White, 1))
            {
                g.FillRectangle(brush, _frameCounterBounds);
                g.DrawRectangle(pen, _frameCounterBounds);

                if (_activeTextBox == null || !_frameCounterBounds.Contains(_activeTextBox.Location))
                {
                    using (var font = new Font("Arial", 10, FontStyle.Bold))
                    {
                        string frameText = $"{_currentFrame}";
                        var format = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(frameText, font, Brushes.White, _frameCounterBounds, format);
                    }
                }
            }
        }

        private void DrawTimeline(Graphics g)
        {
            // 添加防御性检查，确保不会出现除零错误
            if (_endFrame <= _startFrame)
            {
                _pixelsPerFrame = 1.0f;
            }
            else
            {
                // 限制缩放范围，防止溢出
                _timelineZoom = Math.Max(0.1f, Math.Min(_timelineZoom, 10.0f));
                _pixelsPerFrame = (float)_timelineBounds.Width / (Math.Max(1, (_endFrame - _startFrame)) * _timelineZoom);
            }

            using (var pen = new Pen(Color.White, 1))
            {
                // 绘制主轴线
                g.DrawLine(pen,
                    _timelineBounds.Left, _timelineBounds.Top + _timelineBounds.Height / 2,
                    _timelineBounds.Right, _timelineBounds.Top + _timelineBounds.Height / 2);

                DrawTimelineGraduations(g, pen);
                DrawCurrentFrameIndicator(g);
            }
        }

        //主轴线刻度线
        private void DrawTimelineGraduations(Graphics g, Pen pen)
        {
            for (int frame = _startFrame; frame <= _endFrame; frame += 10)
            {
                float x = _timelineBounds.Left + (frame - _startFrame) * _pixelsPerFrame;
                int tickHeight = frame % 50 == 0 ? 10 : 5;

                g.DrawLine(pen,
                    x, _timelineBounds.Top + _timelineBounds.Height / 2 - tickHeight,
                    x, _timelineBounds.Top + _timelineBounds.Height / 2 + tickHeight);

                if (frame % 50 == 0)
                {
                    using (var font = new Font("Arial", 8))
                    {
                        g.DrawString(frame.ToString(), font, Brushes.White,
                            x - 10, _timelineBounds.Top + _timelineBounds.Height / 2 + tickHeight + 2);
                    }
                }
            }
        }

        //当前帧刻度线
        private void DrawCurrentFrameIndicator(Graphics g)
        {
            float currentX = _timelineBounds.Left + (_currentFrame - _startFrame) * _pixelsPerFrame;
            using (var indicatorPen = new Pen(Color.DeepSkyBlue, 2))
            {
                g.DrawLine(indicatorPen,
                    currentX, _timelineBounds.Top - _timelineBounds.Height,
                    currentX, _timelineBounds.Bottom + 3*_timelineBounds.Height);

                DrawFrameLabel(g, currentX);
            }
        }

        //当前帧标签
        private void DrawFrameLabel(Graphics g, float currentX)
        {
            Rectangle labelBounds = GetFrameLabelBounds(g);

            using (var brush = new SolidBrush(Color.DeepSkyBlue))
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                // 绘制圆角背景
                int radius = 4;
                DrawRoundedRectangle(path, labelBounds, radius);
                g.FillPath(brush, path);
                g.DrawPath(Pens.DeepSkyBlue, path);

                // 绘制文本
                using (var font = new Font("Arial", 10))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(_currentFrame.ToString(), font, Brushes.White, labelBounds, format);
                }
            }
        }

        private void DrawRoundedRectangle(System.Drawing.Drawing2D.GraphicsPath path, Rectangle bounds, int radius)
        {
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
        }

        private void DrawFrameInputs(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(53, 53, 53)))
            using (var pen = new Pen(Color.White, 1))
            using (var font = new Font("Arial", 8))
            {
                // 起始帧
                g.FillRectangle(brush, _startFrameBounds);
                g.DrawRectangle(pen, _startFrameBounds);
                g.DrawString($"Start: {_startFrame}", font, Brushes.White, _startFrameBounds.X + 2, _startFrameBounds.Y + 2);

                // 结束帧
                g.FillRectangle(brush, _endFrameBounds);
                g.DrawRectangle(pen, _endFrameBounds);
                g.DrawString($"End: {_endFrame}", font, Brushes.White, _endFrameBounds.X + 2, _endFrameBounds.Y + 2);
            }
        }

        private void DrawKeyframesAndInterpolation(Graphics g)
        {
            if (_keyframes.Count == 0) return;

            float displayTop = _bounds.Top + PADDING + VALUE_DISPLAY_HEIGHT * 0.1f;
            float displayBottom = _timelineBounds.Top - PADDING;

            DrawValueDisplayBoundaries(g, displayTop, displayBottom);
            DrawInterpolationLine(g, displayTop, displayBottom);
            DrawKeyframePoints(g, displayTop, displayBottom);
        }
        private void DrawValueDisplayBoundaries(Graphics g, float displayTop, float displayBottom)
        {
            // 计算当前最大最小值
            double maxValue = _keyframes.Count > 0 ? _keyframes.Max(k => k.Value) : 100.0;
            double minValue = _keyframes.Count > 0 ? _keyframes.Min(k => k.Value) : 0.0;

            // 向上/向下取整到10的倍数
            maxValue = Math.Ceiling(maxValue / 10.0) * 10.0;
            minValue = Math.Floor(minValue / 10.0) * 10.0;

            // 确保最大最小值不相等
            if (Math.Abs(maxValue - minValue) < 1e-6)
            {
                maxValue += 10.0;
                minValue -= 10.0;
            }

            using (var boundaryPen = new Pen(Color.FromArgb(60, Color.White)))
            {
                // 绘制上下边界线
                g.DrawLine(boundaryPen,
                    _timelineBounds.Left, displayTop,
                    _timelineBounds.Right, displayTop);
                g.DrawLine(boundaryPen,
                    _timelineBounds.Left, displayBottom,
                    _timelineBounds.Right, displayBottom);

                // 绘制刻度值
                using (var font = new Font("Arial", 8))
                {
                    g.DrawString(maxValue.ToString("F0"), font, Brushes.White,
                        _timelineBounds.Left - 35, displayTop);
                    g.DrawString(minValue.ToString("F0"), font, Brushes.White,
                        _timelineBounds.Left - 35, displayBottom - 8);
                }
            }
        }

        private void DrawInterpolationLine(Graphics g, float displayTop, float displayBottom)
        {
            if (_keyframes.Count < 2) return;

            using (var interpolationPen = new Pen(Color.Orange, 1))
            {
                for (int i = 0; i < _keyframes.Count - 1; i++)
                {
                    var k1 = _keyframes[i];
                    var k2 = _keyframes[i + 1];

                    float x1 = _timelineBounds.Left + (k1.Frame - _startFrame) * _pixelsPerFrame;
                    float x2 = _timelineBounds.Left + (k2.Frame - _startFrame) * _pixelsPerFrame;

                    float y1 = MapValueToY(k1.Value, displayTop, displayBottom);
                    float y2 = MapValueToY(k2.Value, displayTop, displayBottom);

                    g.DrawLine(interpolationPen, x1, y1, x2, y2);
                }
            }
        }

        private void DrawKeyframePoints(Graphics g, float displayTop, float displayBottom)
        {
            using (var keyframeBrush = new SolidBrush(Color.Orange))
            using (var font = new Font("Arial", 8))
            {
                foreach (var keyframe in _keyframes)
                {
                    float x = _timelineBounds.Left + (keyframe.Frame - _startFrame) * _pixelsPerFrame;
                    float y = MapValueToY(keyframe.Value, displayTop, displayBottom);

                    // 绘制关键帧点
                    g.FillEllipse(keyframeBrush, x - 4, y - 4, 8, 8);

                    // 显示关键帧的具体值
                    string valueText = $"{keyframe.Value:F1}";
                    g.DrawString(valueText, font, Brushes.White, x + 5, y - 10);
                }
            }
        }

        private float MapValueToY(double value, float displayTop, float displayBottom)
        {
            double maxValue = _keyframes.Count > 0 ? _keyframes.Max(k => k.Value) : 100.0;
            double minValue = _keyframes.Count > 0 ? _keyframes.Min(k => k.Value) : 0.0;

            // 添加安全边界
            const double MIN_RANGE = 1e-6;
            maxValue = Math.Ceiling(maxValue / 10.0) * 10.0;
            minValue = Math.Floor(minValue / 10.0) * 10.0;

            // 确保值范围合理
            if (maxValue - minValue < MIN_RANGE)
            {
                maxValue = minValue + 10.0;
            }

            // 防止除零错误和溢出
            double range = Math.Max(MIN_RANGE, maxValue - minValue);
            float normalizedValue = (float)((value - minValue) / range);
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));

            // 确保返回值在有效范围内
            return Math.Max(displayTop, Math.Min(displayBottom, 
                displayBottom - (normalizedValue * (displayBottom - displayTop))));
        }
    }
}
