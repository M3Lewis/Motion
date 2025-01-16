using System.Drawing;

namespace Motion.Widget
{
    //时间轴矩形区域大小设置
    internal partial class TimelineWidget
    {
        private Rectangle WidgetArea => Rectangle.Union(ActualControlArea, BorderArea);
        private Rectangle ActualControlArea => CreateControlArea();
        private Rectangle BorderArea
        {
            get
            {
                if (Owner?.Bounds == null || Owner.Bounds.IsEmpty)
                    return Rectangle.Empty;

                var clientRect = Owner.ClientRectangle;
                const int height = 200;  // 边界区域固定高度

                // 根据停靠位置计算边界区域
                return m_dockSide switch
                {
                    TimelineWidgetDock.Top => new Rectangle(
                        clientRect.Left,           // 从窗口左侧开始
                        clientRect.Top + 20,             // 从工具栏下方开始
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
    }
}
