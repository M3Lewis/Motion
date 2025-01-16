using System;
using System.Threading;

//时间轴处理事件

namespace Motion.Widget
{
    internal partial class TimelineWidget 
    {
        public delegate void DockSideChangedEventHandler();
        public delegate void WidgetVisibleChangedEventHandler();
        public static DockSideChangedEventHandler onDockSideChanged;
        public static WidgetVisibleChangedEventHandler onWidgetVisibleChanged;

        public static event DockSideChangedEventHandler DockSideChanged
        {
            add
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = onDockSideChanged;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Combine(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref onDockSideChanged, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
            remove
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = onDockSideChanged;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Remove(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref onDockSideChanged, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
        }

        public static event WidgetVisibleChangedEventHandler WidgetVisibleChanged
        {
            add
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = onWidgetVisibleChanged;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Combine(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref onWidgetVisibleChanged, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
            remove
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = onWidgetVisibleChanged;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Remove(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref onWidgetVisibleChanged, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
        }
    }
}
