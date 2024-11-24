using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;

namespace Motion.Animation
{
    public class IntervalLock : GH_Component
    {
        public IntervalLock()
            : base("Interval Lock", "Lock",
                "检测时间是否在指定区间内，不在区间内时锁定同组内的组件",
                "Motion", "01_Animation")
        {
            this.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Time", "T", "当前时间", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Domain", "D", "检测区间", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Include?", "I", "时间是否在区间内", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double time = 0;
            Interval domain = new Interval();

            if (!DA.GetData(0, ref time)) return;
            if (!DA.GetData(1, ref domain)) return;

            bool isIncluded = domain.IncludesParameter(time);
            DA.SetData(0, isIncluded);

            SetGroupComponentsLock(!isIncluded);
        }

        private void SetGroupComponentsLock(bool lockState)
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var currentGroup = doc.Objects
                .OfType<GH_Group>()
                .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

            if (currentGroup == null) return;

            foreach (var id in currentGroup.ObjectIDs)
            {
                var obj = doc.FindObject(id, true);
                if (obj == this) continue;
                if (obj is not IGH_ActiveObject activeObj) continue;

                activeObj.Locked = lockState;
            }
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            if (this.Params.Input[0].Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            var timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase));

            if (timelineSlider != null)
            {
                var timeParam = Params.Input[0];
                timeParam.AddSource(timelineSlider);
                timeParam.WireDisplay = GH_ParamWireDisplay.hidden;
                ExpireSolution(true);
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override Bitmap Icon => Properties.Resources.IntervalLock;

        public override Guid ComponentGuid => new Guid("F888B3BB-2882-4EEF-861A-E581785A1786");
    }
}