using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;

namespace Motion
{
    public class AdjustSearchCountComponent : GH_Component
    {
        public AdjustSearchCountComponent()
          : base("AdjustSearchCount", "AdjustSearchCount",
            "调整GH最大搜索数量",
            "Motion", "03_Util")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("count", "C", "最大搜索数量", GH_ParamAccess.item, 30);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int iCount = 0;
            if (!DA.GetData(0, ref iCount)) return;
            Grasshopper.CentralSettings.CanvasMaxSearchResults = iCount;
        }
        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("91f897b4-ad1b-4c35-bffa-8e944bc58955");
    }
}