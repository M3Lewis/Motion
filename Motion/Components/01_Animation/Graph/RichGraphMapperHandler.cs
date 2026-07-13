using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;

namespace Motion.Components
{
    public class RichGraphMapperHandler : IGraphTypeHandler
    {
        public Guid ComponentGuid { get; } = new Guid("e2996e6c-e067-42fa-8f44-2192c6763262");
        public PointF PositionOffset { get; } = new PointF(100, -15);
        public int InputPortIndex { get; } = 0;
        public bool NeedsBezierGraph { get; } = true;

        public void PostConfigure(GH_Document doc, IGH_ActiveObject graphObject)
        {
            try
            {
                var bezier2GraphGuid = new Guid("34afa8f2-fee6-4e3b-82da-b980ffeb87aa");
                var customGraph = Grasshopper.Instances.ComponentServer.EmitGraph(bezier2GraphGuid);
                if (customGraph == null) return;

                customGraph.PrepareForUse();
                
                var containerProp = graphObject.GetType().GetProperty("Container");
                if (containerProp == null) return;

                var container = containerProp.GetValue(graphObject, null) as GH_GraphContainer 
                                ?? new GH_GraphContainer(customGraph);

                container.Graph = customGraph;
                container.X0 = 0;
                container.X1 = 1;
                container.Y0 = 0;
                container.Y1 = 1;

                containerProp.SetValue(graphObject, null, null);
                containerProp.SetValue(graphObject, container, null);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine(
                    $"[Motion] Failed to set default graph for Rich Graph Mapper: {ex.Message}");
            }
        }
    }
}