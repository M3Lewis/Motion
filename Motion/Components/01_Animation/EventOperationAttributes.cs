using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino.Geometry;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel.Special;
using System.Linq;
using Grasshopper;
using System;
using Motion.General;

namespace Motion.Animation
{
    public class EventOperationAttributes : GH_ComponentAttributes
    {
        public EventOperationAttributes(EventOperation owner) : base(owner)
        {
            Owner = owner;
        }
        public new EventOperation Owner;

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            base.RespondToMouseDoubleClick(sender, e);
            if (Owner == null || Owner.Params.Input[0].SourceCount == 0)
                return GH_ObjectResponse.Ignore;

            // 获取所有连接的 GraphMapper 和对应的 EventComponent，并存储区间最小值和最大值
            var menuItems = new List<(EventComponent component, string nickname, double minValue, double maxValue)>();
            foreach (var source in Owner.Params.Input[0].Sources)
            {
                var graphMapperObject = source.Attributes.GetTopLevel.DocObject;
                switch (graphMapperObject)
                {
                    case GH_GraphMapper currentGraphMapper when currentGraphMapper?.Sources.Count > 0:
                    case GH_Component mapperComponent when mapperComponent != null:
                        EventComponent targetEventComponent = null;

                        switch (graphMapperObject)
                        {
                            case GH_GraphMapper graphMapper:
                                IGH_DocumentObject documentObject = graphMapper.Sources[0];
                                targetEventComponent = documentObject.Attributes.GetTopLevel?.DocObject as EventComponent;
                                break;

                            case GH_Component component:
                                var handler = GraphTypeHandlerRegistry.Handlers.Values
                                    .FirstOrDefault(h => h.ComponentGuid == component.ComponentGuid);
                                int portIndex = handler?.InputPortIndex ?? 0;
                                IGH_Param inputParameter = component.Params.Input[portIndex];
                                
                                if (inputParameter?.Sources.Count == 0) break;

                                IGH_DocumentObject sourceObject = inputParameter.Sources[0];
                                targetEventComponent = sourceObject.Attributes.GetTopLevel?.DocObject as EventComponent;
                                break;
                        }

                        if (targetEventComponent != null && MotilityUtils.TryParseNickNameInterval(targetEventComponent.NickName, out double minimumValue, out double maximumValue))
                        {
                            menuItems.Add((targetEventComponent, targetEventComponent.NickName, minimumValue, maximumValue));
                        }
                        break;
                }
            }
            if (!menuItems.Any()) return GH_ObjectResponse.Ignore;

            // 按区间最小值排序，如果最小值相同则按最大值排序
            menuItems.Sort((a, b) =>
            {
                int minComparison = a.minValue.CompareTo(b.minValue);
                return minComparison != 0 ? minComparison : a.maxValue.CompareTo(b.maxValue);
            });
            
            // 创建上下文菜单
            var menu = new ToolStripDropDown();
            foreach (var item in menuItems)
            {
                var menuItem = new ToolStripMenuItem(item.nickname);
                var targetComponent = item.component; // 捕获目标组件
                menuItem.Click += (s, args) =>
                {
                    MotilityUtils.GoComponent(targetComponent);
                };
                menu.Items.Add(menuItem);
            }

            // 在组件位置显示菜单
            var screenPoint = sender.Viewport.ProjectPoint(Pivot);

            // 【新增：安全延迟释放逻辑】
            menu.Closed += (s, args) =>
            {
                // sender 是 GH_Canvas，通过 BeginInvoke 保证释放操作在 Click 事件彻底执行完毕后才触发
                sender.BeginInvoke(new Action(menu.Dispose));
            };

            menu.Show(sender, new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y));

            return GH_ObjectResponse.Handled;
        }
        
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            // 1. 卫语句：如果不是绘制物体的通道，直接返回
            if (channel != GH_CanvasChannel.Objects) 
                return;

            // 2. 卫语句：如果 Owner 为空，直接返回
            var eventOperation = Owner;
            if (eventOperation == null) 
                return;

            // 3. 关键修复：使用 using 包装 Font 以防止 GDI 句柄泄露
            using (var labelFont = new Font(GH_FontServer.StandardBold.FontFamily, 8f))
            {
                // 绘制左侧数值
                double eventValue = eventOperation.CurrentEventValue;
                string valueLabel = $"{eventValue:F2}";

                var valueLabelBounds = new RectangleF(
                    Bounds.Left - 30,
                    Bounds.Top - 5,
                    30,
                    labelFont.Height
                );

                using (var brush = new SolidBrush(Color.LightSkyBlue))
                {
                    graphics.DrawString(valueLabel, labelFont, brush, valueLabelBounds);
                }

                // 绘制右侧映射数值
                double currentMappedEventValue = eventOperation.CurrentMappedEventValue;
        
                // 根据数值大小决定显示格式，使用 Math.Round 进行四舍五入
                string currentMappedEventValueLabel = currentMappedEventValue > 1000
                    ? $"{(int)Math.Round(currentMappedEventValue)}"
                    : $"{currentMappedEventValue:F2}";

                var currentMappedEventValueBounds = new RectangleF(
                    Bounds.Right + 5,
                    Bounds.Top - 5,
                    100,
                    labelFont.Height
                );

                using (var brush = new SolidBrush(Color.Orange))
                {
                    graphics.DrawString(currentMappedEventValueLabel, labelFont, brush, currentMappedEventValueBounds);
                }
            }
        }
    }
}