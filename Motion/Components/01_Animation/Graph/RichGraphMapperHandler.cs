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

        public void PostConfigure(GH_Document doc, IGH_Component graphComponent)
        {
            try
            {
                var bezier2GraphGuid = new Guid("34afa8f2-fee6-4e3b-82da-b980ffeb87aa");
                var customGraph = Grasshopper.Instances.ComponentServer.EmitGraph(bezier2GraphGuid);
                if (customGraph != null)
                {
                    customGraph.PrepareForUse();
                    var containerProp = graphComponent.GetType().GetProperty("Container");
                    if (containerProp != null)
                    {
                        var container =
                            containerProp.GetValue(graphComponent, null) as GH_GraphContainer;
                        containerProp.SetValue(graphComponent, null, null);

                        if (container == null)
                        {
                            container = new GH_GraphContainer(customGraph);
                        }
                        else
                        {
                            container.Graph = customGraph;
                        }

                        container.X0 = 0;
                        container.X1 = 1;
                        container.Y0 = 0;
                        container.Y1 = 1;

                        containerProp.SetValue(graphComponent, container, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine(
                    $"[Motion] Failed to set default graph for Rich Graph Mapper: {ex.Message}");
            }
        }
    }
}