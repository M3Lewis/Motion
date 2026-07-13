using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;

namespace Motion.Components
{
    public class GraphMapperHandler : IGraphTypeHandler
    {
        public Guid ComponentGuid { get; } = new Guid("bc984576-7aa6-491f-a91d-e444c33675a7");
        public PointF PositionOffset { get; } = new PointF(100, -75);
        public int InputPortIndex { get; } = 0;
        public bool NeedsBezierGraph { get; } = true;

        public void PostConfigure(GH_Document doc, IGH_ActiveObject graphObject)
        {
            if (graphObject is not GH_GraphMapper graphMapper) return;
            
            var bezierGraph = Grasshopper.Instances.ComponentServer.EmitGraph(new GH_BezierGraph().GraphTypeID);
            if (bezierGraph == null) return;

            bezierGraph.PrepareForUse();
            
            var container = graphMapper.Container ?? new GH_GraphContainer(bezierGraph);
            container.Graph = bezierGraph;
            container.X0 = 0;
            container.X1 = 1;
            container.Y0 = 0;
            container.Y1 = 1;

            graphMapper.Container = null;
            graphMapper.Container = container;
        }
    }
}