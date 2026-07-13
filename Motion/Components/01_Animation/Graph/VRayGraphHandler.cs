using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;

namespace Motion.Components
{
    public class VRayGraphHandler : IGraphTypeHandler
    {
        public Guid ComponentGuid { get; } = new Guid("6b30c365-2690-4d61-b2ca-8ec5f2118665");
        public PointF PositionOffset { get; } = new PointF(100, -57);
        public int InputPortIndex { get; } = 0;
        public bool NeedsBezierGraph { get; } = false;

        public void PostConfigure(GH_Document doc, IGH_ActiveObject graphObject)
        {
            
        }
    }
}