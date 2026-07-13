using System.Drawing;
using Grasshopper.Kernel;
using Motion.Animation;
using Motion.Components;
using Motion.Toolbar;

namespace Motion.General
{
    /// <summary>
    /// 同步创建 EventComponent + Graph 组件/参数的工厂类。
    /// 遵循 Happy Path 直下原则，将复杂逻辑拆分为小段无深层嵌套的私有方法。
    /// </summary>
    public static class EventGraphFactory
    {
        public static (EventComponent eventComp, IGH_ActiveObject graphObj)
            CreateEventWithGraph(GH_Document doc, MotionSender senderParam, PointF eventPivot)
        {
            var eventComp = CreateAndConnectEvent(doc, senderParam, eventPivot);
            var graphObj = CreateAndConnectGraph(doc, eventComp, eventPivot);

            return (eventComp, graphObj);
        }

        private static EventComponent CreateAndConnectEvent(GH_Document doc, MotionSender senderParam, PointF eventPivot)
        {
            var eventComp = new EventComponent();
            eventComp.CreateAttributes();
            eventComp.Attributes.Pivot = eventPivot;

            doc.AddObject(eventComp, false);

            var timeParam = eventComp.Params.Input[0];
            timeParam.AddSource(senderParam);
            timeParam.WireDisplay = GH_ParamWireDisplay.hidden;

            eventComp.LinkToSender(senderParam);
            return eventComp;
        }

        private static IGH_ActiveObject CreateAndConnectGraph(GH_Document doc, EventComponent eventComp, PointF eventPivot)
        {
            if (!GraphTypeHandlerRegistry.Handlers.TryGetValue(
                    MotionSenderSettings.DoubleClickGraphType, out var handler))
            {
                return null;
            }

            var emitted = Grasshopper.Instances.ComponentServer.EmitObject(handler.ComponentGuid);
            if (emitted is not IGH_ActiveObject graphObj)
            {
                return null;
            }

            graphObj.CreateAttributes();
            graphObj.Attributes.Pivot = new PointF(
                eventPivot.X + handler.PositionOffset.X,
                eventPivot.Y + handler.PositionOffset.Y
            );

            doc.AddObject(graphObj, false);

            ConnectGraphToEvent(graphObj, eventComp, handler.InputPortIndex);

            handler.PostConfigure(doc, graphObj);

            return graphObj;
        }

        private static void ConnectGraphToEvent(IGH_ActiveObject graphObj, EventComponent eventComp, int inputPortIndex)
        {
            var sourceParam = eventComp.Params.Output[0];

            if (graphObj is IGH_Component graphComp)
            {
                if (inputPortIndex >= 0 && inputPortIndex < graphComp.Params.Input.Count)
                {
                    var inputParam = graphComp.Params.Input[inputPortIndex];
                    inputParam.AddSource(sourceParam);
                    inputParam.WireDisplay = GH_ParamWireDisplay.faint;
                }
            }
            else if (graphObj is IGH_Param graphParam)
            {
                graphParam.AddSource(sourceParam);
                graphParam.WireDisplay = GH_ParamWireDisplay.faint;
            }
        }
    }
}
