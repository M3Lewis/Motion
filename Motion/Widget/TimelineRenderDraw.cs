using Grasshopper.GUI.Canvas;
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        // 添加常量定义
        private const int BUTTON_SIZE = 30;
        private const int PADDING = 10;
        private const float VALUE_DISPLAY_HEIGHT = 60;
        private const int TEXT_BOX_HEIGHT = 20;

        // 添加新的矩形字段
        private Rectangle _prevKeyframeBounds;
        private Rectangle _nextKeyframeBounds;
        private Rectangle _startFrameJumpBounds;
        private Rectangle _endFrameJumpBounds;

        private void CalculateBounds()
        {
            _playButtonBounds = new Rectangle(
                _timelineBounds.Right - _timelineBounds.Width / 2,
                _bounds.Top + PADDING,
                TEXT_BOX_HEIGHT,
                TEXT_BOX_HEIGHT);

            _frameCounterBounds = new Rectangle(
                _timelineBounds.Right - 240,
                _bounds.Top + PADDING,
                70,
                TEXT_BOX_HEIGHT);

            _timelineBounds = new Rectangle(
                _bounds.Left + PADDING,
                _bounds.Top + _bounds.Height / 2 - 15,
                _bounds.Right - PADDING,
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

            // 计算导航按钮的大小和位置
            int navButtonSize = BUTTON_SIZE - 10; // 稍微小一点的按钮尺寸
            int verticalSpacing = 5; // 按钮之间的垂直间距

            _startFrameJumpBounds = new Rectangle(
                _timelineBounds.Right - _timelineBounds.Width / 2 - 50,
                _bounds.Top + PADDING,
                navButtonSize,
                navButtonSize);

            _prevKeyframeBounds = new Rectangle(
                _timelineBounds.Right - _timelineBounds.Width / 2 - 25,
                _bounds.Top + PADDING,
                navButtonSize,
                navButtonSize);

            _nextKeyframeBounds = new Rectangle(
                _timelineBounds.Right - _timelineBounds.Width / 2 + 25,
                _bounds.Top + PADDING,
                navButtonSize,
                navButtonSize);

            _endFrameJumpBounds = new Rectangle(
                _timelineBounds.Right - _timelineBounds.Width / 2 + 50,
                _bounds.Top + PADDING,
                navButtonSize,
                navButtonSize);
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
            try
            {
                // 绘制播放/暂停按钮
                using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    if (_isPlaying)
                    {
                        // 绘制暂停图标
                        int padding = 4;
                        g.FillRectangle(brush,
                            _playButtonBounds.X + padding,
                            _playButtonBounds.Y + padding,
                            (_playButtonBounds.Width - padding * 3) / 2,
                            _playButtonBounds.Height - padding * 2);
                        g.FillRectangle(brush,
                            _playButtonBounds.X + _playButtonBounds.Width / 2 + padding / 2,
                            _playButtonBounds.Y + padding,
                            (_playButtonBounds.Width - padding * 3) / 2,
                            _playButtonBounds.Height - padding * 2);
                    }
                    else
                    {
                        // 绘制播放图标（三角形）
                        Point[] points = new Point[]
                        {
                            new Point(_playButtonBounds.X + 4, _playButtonBounds.Y + 4),
                            new Point(_playButtonBounds.X + 4, _playButtonBounds.Bottom - 4),
                            new Point(_playButtonBounds.Right - 4, _playButtonBounds.Y + _playButtonBounds.Height / 2)
                        };
                        g.FillPolygon(brush, points);
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error drawing play button: {ex.Message}");
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
            if (_endFrame <= _startFrame)
            {
                _pixelsPerFrame = 1.0f;
            }
            else
            {
                _timelineZoom = Math.Max(0.1f, Math.Min(_timelineZoom, 10.0f));
                _pixelsPerFrame = (float)_timelineBounds.Width / ((_endFrame - _startFrame) * _timelineZoom);
            }

            // 绘制主轴线
            using (var mainLinePen = new Pen(Color.FromArgb(70, 70, 70), 1))
            {
                float centerY = _timelineBounds.Top + _timelineBounds.Height / 2;
                g.DrawLine(mainLinePen,
                    _timelineBounds.Left,
                    centerY,
                    _timelineBounds.Right,
                    centerY);

                DrawTimelineGraduations(g, mainLinePen);
                DrawCurrentFrameIndicator(g);
            }
        }

        //主轴线刻度线
        private void DrawTimelineGraduations(Graphics g, Pen pen)
        {
            if (_pixelsPerFrame <= 0) return;

            // 定义颜色
            Color backgroundColor = Color.FromArgb(29, 29, 29);      // 最后面的背景色
            Color frameRangeColor = Color.FromArgb(33, 33, 33);     // 普通帧数范围背景色
            Color activeRangeColor = Color.FromArgb(48, 48, 48);    // 当前活动范围背景色
            Color labelColor = Color.FromArgb(128, 128, 128);       // 帧数文字颜色

            // 首先绘制最底层背景
            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(bgBrush, _timelineBounds.Left,
                    _timelineBounds.Top - _timelineBounds.Height,
                    _timelineBounds.Width,
                    _timelineBounds.Height * 5);
            }

            // 计算可见区域的起始和结束帧
            float timelineStartFrame = _startFrame - (_timelineBounds.Left / _pixelsPerFrame);
            float timelineEndFrame = timelineStartFrame + (_timelineBounds.Width / _pixelsPerFrame);

            // 绘制整个时间轴的背景
            float startX = _timelineBounds.Left + (_startFrame - timelineStartFrame) * _pixelsPerFrame;
            float endX = _timelineBounds.Left + (_endFrame - timelineStartFrame) * _pixelsPerFrame;

            // 绘制活动范围背景
            RectangleF activeRangeRect = new RectangleF(
                startX,
                _timelineBounds.Top - _timelineBounds.Height,
                endX - startX,
                _timelineBounds.Height * 5);

            using (var brush = new SolidBrush(activeRangeColor))
            {
                g.FillRectangle(brush, activeRangeRect);
            }

            // 根据缩放比例动态计算标签间隔
            int labelInterval;
            if (_pixelsPerFrame >= 40) labelInterval = 10;
            else if (_pixelsPerFrame >= 20) labelInterval = 20;
            else if (_pixelsPerFrame >= 10) labelInterval = 50;
            else labelInterval = 100;

            int firstTick = ((int)(timelineStartFrame + labelInterval - 1) / labelInterval) * labelInterval;
            int lastTick = ((int)timelineEndFrame / labelInterval) * labelInterval;

            // 绘制刻度和标签
            for (int frame = firstTick; frame <= lastTick; frame += labelInterval)
            {
                float x = _timelineBounds.Left + (frame - timelineStartFrame) * _pixelsPerFrame;

                if (x < _timelineBounds.Left || x > _timelineBounds.Right) continue;

                // 绘制帧数标签
                if (x >= _timelineBounds.Left && x <= _timelineBounds.Right)
                {
                    using (var font = new Font("Arial", 8))
                    {
                        string frameText = frame.ToString();
                        using (var textBrush = new SolidBrush(labelColor))
                        {
                            g.DrawString(frameText, font, textBrush,
                                x + 5,
                                _timelineBounds.Top - _timelineBounds.Height - 20);
                        }
                    }
                }
            }

            // 绘制网格线
            using (var activeGridPen = new Pen(Color.FromArgb(33, 33, 33), 1))  // 活动范围内的网格线颜色
            using (var inactiveGridPen = new Pen(Color.FromArgb(45, 45, 45), 1))  // 活动范围外的网格线颜色
            {
                for (int frame = firstTick; frame <= lastTick; frame += 10)
                {
                    float x = _timelineBounds.Left + (frame - timelineStartFrame) * _pixelsPerFrame;

                    if (x < _timelineBounds.Left || x > _timelineBounds.Right) continue;

                    // 判断当前刻度是否在活动范围内
                    bool isInActiveRange = frame >= _startFrame && frame <= _endFrame;
                    var gridPen = isInActiveRange ? activeGridPen : inactiveGridPen;

                    g.DrawLine(gridPen,
                        x, _timelineBounds.Top - _timelineBounds.Height,
                        x, _timelineBounds.Bottom + 3 * _timelineBounds.Height);
                }
            }
        }

        //当前帧刻度线
        private void DrawCurrentFrameIndicator(Graphics g)
        {
            float timelineStartFrame = _startFrame - (_timelineBounds.Left / _pixelsPerFrame);
            float currentX = _timelineBounds.Left + (_currentFrame - timelineStartFrame) * _pixelsPerFrame;

            using (var indicatorPen = new Pen(Color.DeepSkyBlue, 2))
            {
                g.DrawLine(indicatorPen,
                    currentX, _timelineBounds.Top - _timelineBounds.Height,
                    currentX, _timelineBounds.Bottom + 3 * _timelineBounds.Height);

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
            if (_keyframeGroups.Count == 0) return;

            float yOffset = _timelineBounds.Top;

            // Define colors for different groups
            var groupColors = new Dictionary<string, Color>
            {
                { "Default", Color.Orange },
                { "Group1", Color.LimeGreen },
                { "Group2", Color.DodgerBlue },
                { "Group3", Color.Violet },
                { "Group4", Color.Gold }
            };

            // 先绘制所有组的关键帧和插值线
            foreach (var group in _keyframeGroups)
            {
                if (!_groupVisibility[group.Key]) continue;

                // Draw group header
                DrawGroupHeader(g, group.Key, ref yOffset);

                // Only draw keyframes and interpolation if group is visible and not collapsed
                if (_groupVisibility[group.Key] && !(_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                {
                    float displayTop = yOffset;
                    float displayBottom = displayTop + VALUE_DISPLAY_HEIGHT;

                    // Draw value boundaries for this group
                    DrawValueDisplayBoundaries(g, displayTop, displayBottom, group.Value);

                    var color = groupColors.ContainsKey(group.Key) ? groupColors[group.Key] : Color.Orange;
                    DrawInterpolationLine(g, displayTop, displayBottom, group.Value, color);
                    DrawKeyframePoints(g, displayTop, displayBottom, group.Value, color);

                    // 只有在展开状态下才增加VALUE_DISPLAY_HEIGHT的空间
                    yOffset += VALUE_DISPLAY_HEIGHT;
                }
                
                // 不再添加固定的组间距
            }

            // 最后绘制当前帧指示线，确保它在最上层
            DrawCurrentFrameIndicator(g);
        }

        private void DrawGroupHeader(Graphics g, string groupName, ref float yOffset)
        {
            // 组标题区域
            var headerRect = new RectangleF(
                _timelineBounds.Left,
                yOffset,
                _timelineBounds.Width,
                GROUP_HEADER_HEIGHT
            );

            // 绘制组标题背景
            using (var headerBrush = new SolidBrush(Color.FromArgb(40, Color.White)))
            {
                g.FillRectangle(headerBrush, headerRect);
            }

            // 绘制组名
            using (var font = new Font("Arial", 10))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(groupName, font, brush, 
                    headerRect.Left + 25, 
                    headerRect.Top + (GROUP_HEADER_HEIGHT - font.Height) / 2);
            }

            // 绘制折叠/展开按钮
            var collapseButtonRect = new RectangleF(
                headerRect.Left + 5,
                headerRect.Top + (GROUP_HEADER_HEIGHT - 15) / 2,
                15,
                15
            );

            using (var pen = new Pen(Color.White, 1))
            {
                // 绘制 - 横线
                g.DrawLine(pen,
                    collapseButtonRect.Left + 2,
                    collapseButtonRect.Top + collapseButtonRect.Height / 2,
                    collapseButtonRect.Right - 2,
                    collapseButtonRect.Top + collapseButtonRect.Height / 2);

                // 如果是折叠状态，绘制 + 竖线
                if (_groupCollapsed.ContainsKey(groupName) && _groupCollapsed[groupName])
                {
                    g.DrawLine(pen,
                        collapseButtonRect.Left + collapseButtonRect.Width / 2,
                        collapseButtonRect.Top + 2,
                        collapseButtonRect.Left + collapseButtonRect.Width / 2,
                        collapseButtonRect.Bottom - 2);
                }
            }

            // 更新yOffset，移动到组标题下方
            yOffset += GROUP_HEADER_HEIGHT;
        }

        private void DrawValueDisplayBoundaries(Graphics g, float displayTop, float displayBottom, List<Keyframe> keyframes)
        {
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

        private void DrawInterpolationLine(Graphics g, float displayTop, float displayBottom, List<Keyframe> keyframes, Color color)
        {
            if (keyframes.Count < 2) return;

            using (var interpolationPen = new Pen(color, 1))
            {
                for (int i = 0; i < keyframes.Count - 1; i++)
                {
                    var k1 = keyframes[i];
                    var k2 = keyframes[i + 1];

                    // 计算X坐标时考虑时间轴的起始位置
                    float timelineStartFrame = _startFrame - (_timelineBounds.Left / _pixelsPerFrame);
                    float x1 = _timelineBounds.Left + (k1.Frame - timelineStartFrame) * _pixelsPerFrame;
                    float x2 = _timelineBounds.Left + (k2.Frame - timelineStartFrame) * _pixelsPerFrame;

                    float y1 = MapValueToY(k1.Value, displayTop, displayBottom, keyframes);
                    float y2 = MapValueToY(k2.Value, displayTop, displayBottom, keyframes);

                    g.DrawLine(interpolationPen, x1, y1, x2, y2);
                }
            }
        }

        private void DrawKeyframePoints(Graphics g, float displayTop, float displayBottom, List<Keyframe> keyframes, Color color)
        {
            using (var keyframeBrush = new SolidBrush(color))
            using (var font = new Font("Arial", 8))
            {
                foreach (var keyframe in keyframes)
                {
                    // 计算关键帧的X坐标，考虑时间轴的起始位置
                    float timelineStartFrame = _startFrame - (_timelineBounds.Left / _pixelsPerFrame);
                    float x = _timelineBounds.Left + (keyframe.Frame - timelineStartFrame) * _pixelsPerFrame;
                    float y = MapValueToY(keyframe.Value, displayTop, displayBottom, keyframes);

                    // 绘制关键帧点
                    g.FillEllipse(keyframeBrush, x - 4, y - 4, 8, 8);

                    // 显示关键帧的具体值
                    string valueText = $"{keyframe.Value:F1}";
                    g.DrawString(valueText, font, Brushes.White, x + 5, y - 10);
                }
            }
        }

        private float MapValueToY(double value, float displayTop, float displayBottom, List<Keyframe> keyframes)
        {
            double maxValue = keyframes.Count > 0 ? keyframes.Max(k => k.Value) : 100.0;
            double minValue = keyframes.Count > 0 ? keyframes.Min(k => k.Value) : 0.0;

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

        private void DrawNavigationButtons(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (var pen = new Pen(Color.White, 1))
            {
                // 绘制上一个关键帧按钮
                g.DrawRectangle(pen, _prevKeyframeBounds);
                DrawLeftArrowWithBar(g, _prevKeyframeBounds, brush);

                // 绘制下一个关键帧按钮
                g.DrawRectangle(pen, _nextKeyframeBounds);
                DrawRightArrowWithBar(g, _nextKeyframeBounds, brush);

                // 绘制跳转到起始帧按钮
                g.DrawRectangle(pen, _startFrameJumpBounds);
                DrawDoubleLeftArrow(g, _startFrameJumpBounds, brush);

                // 绘制跳转到结束帧按钮
                g.DrawRectangle(pen, _endFrameJumpBounds);
                DrawDoubleRightArrow(g, _endFrameJumpBounds, brush);
            }
        }

        // 辅助方法：绘制带竖线的左箭头（上一个关键帧）
        private void DrawLeftArrowWithBar(Graphics g, Rectangle bounds, Brush brush)
        {
            int padding = 4;
            int arrowSize = bounds.Width - padding * 2;

            // 绘制竖线
            g.FillRectangle(brush,
                bounds.X + padding,
                bounds.Y + padding,
                2,
                arrowSize);

            // 绘制箭头
            Point[] points = new Point[]
            {
                new Point(bounds.Right - padding, bounds.Y + padding),
                new Point(bounds.X + padding + 4, bounds.Y + bounds.Height/2),
                new Point(bounds.Right - padding, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points);
        }

        // 辅助方法：绘制带竖线的右箭头（下一个关键帧）
        private void DrawRightArrowWithBar(Graphics g, Rectangle bounds, Brush brush)
        {
            int padding = 4;
            int arrowSize = bounds.Width - padding * 2;

            // 绘制竖线
            g.FillRectangle(brush,
                bounds.Right - padding - 2,
                bounds.Y + padding,
                2,
                arrowSize);

            // 绘制箭头
            Point[] points = new Point[]
            {
                new Point(bounds.X + padding, bounds.Y + padding),
                new Point(bounds.Right - padding - 4, bounds.Y + bounds.Height/2),
                new Point(bounds.X + padding, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points);
        }

        // 辅助方法：绘制双左箭头（跳转到起始帧）
        private void DrawDoubleLeftArrow(Graphics g, Rectangle bounds, Brush brush)
        {
            int padding = 4;
            int arrowWidth = (bounds.Width - padding * 3) / 2;

            // 第一个箭头
            Point[] points1 = new Point[]
            {
                new Point(bounds.X + padding + arrowWidth, bounds.Y + padding),
                new Point(bounds.X + padding, bounds.Y + bounds.Height/2),
                new Point(bounds.X + padding + arrowWidth, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points1);

            // 第二个箭头
            Point[] points2 = new Point[]
            {
                new Point(bounds.Right - padding, bounds.Y + padding),
                new Point(bounds.X + padding + arrowWidth + padding, bounds.Y + bounds.Height/2),
                new Point(bounds.Right - padding, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points2);
        }

        // 辅助方法：绘制双右箭头（跳转到结束帧）
        private void DrawDoubleRightArrow(Graphics g, Rectangle bounds, Brush brush)
        {
            int padding = 4;
            int arrowWidth = (bounds.Width - padding * 3) / 2;

            // 第一个箭头
            Point[] points1 = new Point[]
            {
                new Point(bounds.X + padding, bounds.Y + padding),
                new Point(bounds.X + padding + arrowWidth, bounds.Y + bounds.Height/2),
                new Point(bounds.X + padding, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points1);

            // 第二个箭头
            Point[] points2 = new Point[]
            {
                new Point(bounds.X + padding + arrowWidth + padding, bounds.Y + padding),
                new Point(bounds.Right - padding, bounds.Y + bounds.Height/2),
                new Point(bounds.X + padding + arrowWidth + padding, bounds.Bottom - padding)
            };
            g.FillPolygon(brush, points2);
        }
    }
}
