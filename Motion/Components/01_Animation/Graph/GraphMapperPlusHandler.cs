using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;

namespace Motion.Components
{
    public class GraphMapperPlusHandler : IGraphTypeHandler
    {
        public Guid ComponentGuid { get; } = new Guid("310f9597-267e-4471-a7d7-048725557528");
        public PointF PositionOffset { get; } = new PointF(200, -25);
        public int InputPortIndex { get; } = 2;
        public bool NeedsBezierGraph { get; } = false;

        public void PostConfigure(GH_Document doc, IGH_Component graphComponent)
        {
            
        }
    }
}