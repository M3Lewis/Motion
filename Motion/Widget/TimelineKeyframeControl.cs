using System;
using System.Linq;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using RectangleF = System.Drawing.RectangleF;
using System.Collections.Generic;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        // 添加方法来检查是否点击到关键帧
        private Keyframe GetKeyframeAtPoint(Point location)
        {
            float yOffset = _timelineBounds.Top;
            foreach (var group in _keyframeGroups)
            {
                if (!_groupVisibility[group.Key] ||
                    (_groupCollapsed.ContainsKey(group.Key) && _groupCollapsed[group.Key]))
                {
                    yOffset += GROUP_HEADER_HEIGHT;
                    continue;
                }

                float keyframeAreaTop = yOffset + GROUP_HEADER_HEIGHT;
                float keyframeAreaHeight = GROUP_SPACING;

                // 检查是否在关键帧区域内
                var keyframeArea = new RectangleF(
                    _timelineBounds.Left,
                    keyframeAreaTop,
                    _timelineBounds.Width,
                    keyframeAreaHeight
                );

                if (keyframeArea.Contains(location))
                {
                    // 计算点击位置对应的帧
                    float relativeX = location.X - _timelineBounds.Left;
                    int clickedFrame = _startFrame + (int)(relativeX / _pixelsPerFrame);

                    // 查找最近的关键帧（允许5像素的误差）
                    const float CLICK_TOLERANCE = 5f;
                    return group.Value
                        .FirstOrDefault(k =>
                        {
                            float keyframeX = (k.Frame - _startFrame) * _pixelsPerFrame;
                            return Math.Abs(keyframeX - relativeX) <= CLICK_TOLERANCE;
                        });
                }

                yOffset += GROUP_HEADER_HEIGHT + keyframeAreaHeight;
            }
            return null;
        }

        // 添加删除关键帧的方法
        private void Menu_DeleteKeyframe(object sender, EventArgs e)
        {
            if (_selectedKeyframe != null && _keyframeGroups.ContainsKey(_activeGroup))
            {
                _keyframeGroups[_activeGroup].Remove(_selectedKeyframe);
                _selectedKeyframe = null;
                UpdateCurrentValue();
                Owner?.Refresh();
            }
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
                if (_keyframeGroups.ContainsKey(_activeGroup))
                {
                    _keyframeGroups[_activeGroup].Clear();
                    _selectedKeyframe = null;
                    _currentValue = 0.0;
                    Owner?.Refresh();
                }
            }
        }

        private float MapValueToY(double value, List<Keyframe> keyframes)
        {
            float displayTop = _bounds.Top + PADDING + VALUE_DISPLAY_HEIGHT * 0.1f;
            float displayBottom = _timelineBounds.Top - PADDING;

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
    }
}
