using System.Drawing;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private readonly SizeF _labelSize = new SizeF(40, 20); // 固定标签大小，足够容纳4位数字
        private Rectangle GetFrameLabelBounds(Graphics g = null)
        {
            float currentX = _timelineBounds.Left + (_currentFrame - _startFrame) * _pixelsPerFrame;

            // 使用固定大小
            float labelX = currentX - _labelSize.Width / 2;
            float labelY = _timelineBounds.Top - _labelSize.Height - _timelineBounds.Height;

            return new Rectangle(
                (int)(labelX - 2),
                (int)(labelY - 2),
                (int)(_labelSize.Width + 4),
                (int)(_labelSize.Height + 4)
            );
        }
    }
}
