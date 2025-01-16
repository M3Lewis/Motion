using System;
using System.Linq;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using RectangleF = System.Drawing.RectangleF;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        // 添加方法来检查是否点击到关键帧
        private Keyframe GetKeyframeAtPoint(Point point)
        {
            if (_keyframes.Count == 0) return null;

            // 检查每个关键帧
            foreach (var keyframe in _keyframes)
            {
                float x = _timelineBounds.Left + (keyframe.Frame - _startFrame) * _pixelsPerFrame;
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
        // 添加删除关键帧的方法
        private void Menu_DeleteKeyframe(object sender, EventArgs e)
        {
            if (_selectedKeyframe != null)
            {
                _keyframes.Remove(_selectedKeyframe);
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
                _keyframes.Clear();
                _selectedKeyframe = null;
                _currentValue = 0.0;
                Owner?.Refresh();
            }
        }

        private float MapValueToY(double value)
        {
            float displayTop = _bounds.Top + PADDING + VALUE_DISPLAY_HEIGHT * 0.1f;
            float displayBottom = _timelineBounds.Top - PADDING;

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

            float normalizedValue = (float)((value - minValue) / (maxValue - minValue));
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
            return displayBottom - (normalizedValue * (displayBottom - displayTop));
        }
    }
}
