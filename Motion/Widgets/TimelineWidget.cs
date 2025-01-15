using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Widgets;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using static Grasshopper.GUI.Widgets.GH_MarkovWidget;

namespace Motion.Widgets
{
    internal class TimelineWidget : GH_Widget
    {
        public TimelineWidget()
        {
        }
        public override string Name => "Timeline Widget";

        public override string Description => "Motion Animation Timeline widget";

        public override Bitmap Icon_24x24 => null;

        private static TimelineWidgetDock m_dockSide = (TimelineWidgetDock)Instances.Settings.GetValue("Motion.Widget.Timeline.Side", 1);
        private static bool m_showWidget = Instances.Settings.GetValue("Motion.Widget.Timeline.Show", true);

        private static readonly int ControlAreaSize = Global_Proc.UiAdjust(100);//28
        private static readonly int BorderAreaSize = Global_Proc.UiAdjust(30);
        private Rectangle WidgetArea => Rectangle.Union(ActualControlArea, BorderArea);

        private Rectangle ActualControlArea => CreateControlArea();

        //private Rectangle ControlArea
        //{
        //    get
        //    {
        //        Rectangle controlArea = ActualControlArea;
        //        if (controlArea == null)
        //        {
        //            Rectangle clientRectangle = base.Owner.ClientRectangle;
        //            Rectangle sideRectangle;
        //            switch (m_dockSide)
        //            {
        //                case TimelineWidgetDock.Top:
        //                    sideRectangle = new Rectangle(clientRectangle.Left, clientRectangle.Top, 0, ControlAreaSize);
        //                    return sideRectangle;

        //                case TimelineWidgetDock.Bottom:
        //                    sideRectangle = new Rectangle(clientRectangle.Left, clientRectangle.Bottom - ControlAreaSize, 0, ControlAreaSize);
        //                    return sideRectangle;
        //            }
        //        }
        //        return Rectangle.Empty;
        //    }
        //}

        private Rectangle BorderArea
        {
            get
            {
                if (Owner?.Bounds == null || Owner.Bounds.IsEmpty)
                    return Rectangle.Empty;

                var clientRect = Owner.ClientRectangle;
                const int height = 100;  // 边界区域固定高度
                
                // 根据停靠位置计算边界区域
                return m_dockSide switch
                {
                    TimelineWidgetDock.Top => new Rectangle(
                        clientRect.Left,           // 从窗口左侧开始
                        clientRect.Top,             // 从工具栏下方开始
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
                    DockSideChangedEvent?.Invoke();
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
                    WidgetVisibleChangedEvent?.Invoke();
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

        public delegate void DockSideChangedEventHandler();

        public delegate void WidgetVisibleChangedEventHandler();
        public static DockSideChangedEventHandler DockSideChangedEvent;
        public static WidgetVisibleChangedEventHandler WidgetVisibleChangedEvent;

        public static event DockSideChangedEventHandler DockSideChanged
        {
            add
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = DockSideChangedEvent;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Combine(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref DockSideChangedEvent, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
            remove
            {
                DockSideChangedEventHandler dockSideChangedEventHandler = DockSideChangedEvent;
                DockSideChangedEventHandler dockSideChangedEventHandler2;
                do
                {
                    dockSideChangedEventHandler2 = dockSideChangedEventHandler;
                    DockSideChangedEventHandler value2 = (DockSideChangedEventHandler)Delegate.Remove(dockSideChangedEventHandler2, value);
                    dockSideChangedEventHandler = Interlocked.CompareExchange(ref DockSideChangedEvent, value2, dockSideChangedEventHandler2);
                }
                while ((object)dockSideChangedEventHandler != dockSideChangedEventHandler2);
            }
        }

        public static event WidgetVisibleChangedEventHandler WidgetVisibleChanged
        {
            add
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Combine(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
            remove
            {
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
                WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
                do
                {
                    widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
                    WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Remove(widgetVisibleChangedEventHandler2, value);
                    widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
                }
                while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
            }
        }

        public override void AppendToMenu(ToolStripDropDownMenu menu)
        {
            base.AppendToMenu(menu);
            GH_DocumentObject.Menu_AppendSeparator(menu);
            GH_DocumentObject.Menu_AppendItem(menu, "Top", Menu_DockTop, enabled: true, m_dockSide == TimelineWidgetDock.Top);
            GH_DocumentObject.Menu_AppendItem(menu, "Bottom", Menu_DockBottom, enabled: true, m_dockSide == TimelineWidgetDock.Bottom);
        }

        private void Menu_DockTop(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Top;
            m_owner.Refresh();
        }

        private void Menu_DockBottom(object sender, EventArgs e)
        {
            DockSide = TimelineWidgetDock.Bottom;
            m_owner.Refresh();
        }

        //----------------------------------------------------------------------------------------------------
        public override bool Contains(Point pt_control, PointF pt_canvas)
        {
            if (base.Owner.Document == null)
            {
                return false;
            }
            return WidgetArea.Contains(pt_control);
        }

        public override void Render(GH_Canvas canvas)
        {
            if (!Visible || canvas?.Graphics == null) return;
            
            try 
            {
                Graphics g = canvas.Graphics;
                
                // 保存当前的转换矩阵
                var transform = g.Transform;
                
                // 重置转换矩阵，使绘制不受画布缩放和平移的影响
                g.ResetTransform();
                
                // 获取widget区域（这个是相对于窗口的固定位置）
                Rectangle bounds = WidgetArea;
                if (bounds.IsEmpty) return;
                
                // 使用抗锯齿
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // 直接使用窗口坐标绘制
                // 绘制背景
                using (var brush = new SolidBrush(Color.FromArgb(200, 240, 240, 240)))
                {
                    g.FillRectangle(brush, bounds);
                }
                
                // 绘制边框
                using (var pen = new Pen(Color.FromArgb(100, 100, 100), 1.0f))
                {
                    g.DrawRectangle(pen, bounds);
                }
                
                // 恢复原始转换矩阵
                g.Transform = transform;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Timeline Widget rendering error: {ex.Message}");
            }
        }
    }
}