using System.Drawing;

namespace Motion.Widget
{
    internal partial class TimelineWidget
    {
        private readonly SizeF _labelSize = new SizeF(40, 20); // 固定标签大小，足够容纳4位数字
        private Rectangle GetFrameLabelBounds(Graphics g)
        {
            // 使用与刻度线相同的计算方式
            float timelineStartFrame = _startFrame - (_timelineBounds.Left / _pixelsPerFrame);
            float currentX = _timelineBounds.Left + (_currentFrame - timelineStartFrame) * _pixelsPerFrame;

            // 计算标签的宽度和高度
            int labelWidth = 40;
            int labelHeight = 20;

            // 将标签居中于刻度线
            return new Rectangle(
                (int)(currentX - labelWidth / 2),  // 水平居中
                _timelineBounds.Top - _timelineBounds.Height - labelHeight - 5, // 在刻度线上方
                labelWidth,
                labelHeight
            );
        }
    }
}
