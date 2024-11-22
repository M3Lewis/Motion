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

namespace Motion.Motility
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

            // 获取所有连接的 GraphMapper 和对应的 EventComponent
            var menuItems = new List<(EventComponent component, string nickname)>();
            foreach (var source in Owner.Params.Input[0].Sources)
            {
                var graphMapper = source.Attributes.GetTopLevel.DocObject as GH_GraphMapper;
                if (graphMapper?.Sources.Count > 0)
                {
                    var eventComponent = graphMapper.Sources[0].Attributes.GetTopLevel.DocObject as EventComponent;
                    if (eventComponent != null)
                    {
                        menuItems.Add((eventComponent, eventComponent.NickName));
                    }
                }
            }

            if (!menuItems.Any()) return GH_ObjectResponse.Ignore;

            // 创建上下文菜单
            var menu = new ToolStripDropDown();
            foreach (var item in menuItems)
            {
                var menuItem = new ToolStripMenuItem(item.nickname);
                var targetComponent = item.component; // 捕获目标组件
                menuItem.Click += (s, args) =>
                {
                    // 使用 EventComponent 的 GoComponent 方法跳转
                    targetComponent.GoComponent(targetComponent);
                };
                menu.Items.Add(menuItem);
            }

            // 在组件位置显示菜单
            var screenPoint = sender.Viewport.ProjectPoint(Pivot);
            menu.Show(sender, new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y));

            return GH_ObjectResponse.Handled;
        }
        
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var eventOperation = Owner as EventOperation;
                if (eventOperation != null)
                {
                    var labelFont = new Font(GH_FontServer.StandardBold.FontFamily, 8);

                    double eventValue = eventOperation.CurrentEventValue;
                    string valueLabel = $"{eventValue:F2}";
                    
                    var valueLabelBounds = new RectangleF(
                        Bounds.Left - 30,
                        Bounds.Top- 5,
                        30,
                        labelFont.Height
                    );

                    using (var brush = new SolidBrush(Color.LightSkyBlue))
                    {
                        graphics.DrawString(valueLabel, labelFont, brush, valueLabelBounds);
                    }

                    double currentMappedEventValue = eventOperation.CurrentMappedEventValue;
                    string currentMappedEventValueLabel = $"{currentMappedEventValue:F2}";

                    var currentMappedEventValueBounds = new RectangleF(
                        Bounds.Right + 5,
                        Bounds.Top + 5,
                        30,
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
}