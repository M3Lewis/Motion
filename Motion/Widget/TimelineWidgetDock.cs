using System;

namespace Motion.Widget
{
    /// <summary>
    /// 时间轴停靠
    /// </summary>
    public enum TimelineWidgetDock
    {
        Top,
        Bottom,
    }

    //时间轴停靠位置
    internal partial class TimelineWidget
    {
        private void Menu_DockTop(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Top;
            Owner.Refresh();
        }

        private void Menu_DockBottom(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Bottom;
            Owner.Refresh();
        }
    }
}