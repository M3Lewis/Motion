using System.Drawing;
using Grasshopper.Kernel;
using Motion.Animation;
using Motion.Components;
using Motion.Toolbar;

namespace Motion.General
{
    /// <summary>
    /// 同步创建 EventComponent + Graph 组件的工厂方法。
    /// 调用方负责 selection 状态和 ScheduleSolution 刷新。
    /// </summary>
    public static class EventGraphFactory
    {
        public static (EventComponent eventComp, IGH_Component graphComp)
            CreateEventWithGraph(GH_Document doc, MotionSender senderParam, PointF eventPivot)
        {
            // 1. 创建 EventComponent
            var eventComp = new EventComponent();
            eventComp.CreateAttributes();
            eventComp.Attributes.Pivot = eventPivot;

            doc.AddObject(eventComp, false);

            var timeParam = eventComp.Params.Input[0];
            timeParam.AddSource(senderParam);
            timeParam.WireDisplay = GH_ParamWireDisplay.hidden;

            eventComp.LinkToSender(senderParam);

            // 2. 创建 Graph 组件并连接
            IGH_Component graphComponent = null;

            if (GraphTypeHandlerRegistry.Handlers.TryGetValue(
                    MotionSenderSettings.DoubleClickGraphType, out var handler))
            {
                graphComponent =
                    Grasshopper.Instances.ComponentServer.EmitObject(handler.ComponentGuid) as IGH_Component;

                if (graphComponent != null)
                {
                    graphComponent.CreateAttributes();
                    graphComponent.Attributes.Pivot = new PointF(
                        eventPivot.X + handler.PositionOffset.X,
                        eventPivot.Y + handler.PositionOffset.Y
                    );

                    doc.AddObject(graphComponent, false);
                    graphComponent.Params.Input[handler.InputPortIndex].AddSource(eventComp.Params.Output[0]);
                    handler.PostConfigure(doc, graphComponent);
                    graphComponent.Params.Input[handler.InputPortIndex].WireDisplay = GH_ParamWireDisplay.faint;
                }
            }

            return (eventComp, graphComponent);
        }
    }
}
