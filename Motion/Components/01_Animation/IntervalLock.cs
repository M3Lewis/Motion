using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

namespace Motion.Animation
{
    public class IntervalLock : GH_Component
    {
        private bool _previousState = false;
        private List<IGH_ActiveObject> _groupComponents = new List<IGH_ActiveObject>();

        public IntervalLock()
            : base("Interval Lock", "Lock",
                "检测时间是否在指定区间内，不在区间内时锁定同组内的组件.",
                "Motion", "01_Animation")
        {
            this.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Time", "T", "时间", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Domains", "D", "区间（可输入多个）", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Include?", "I", "Whether the time is within the interval", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                double time = 0;
                var domains = new List<Interval>();

                if (!DA.GetData(0, ref time)) return;
                if (!DA.GetDataList(1, domains)) return;

                // 检查时间是否在任何区间内
                bool isIncludedInAny = domains.Any(domain => domain.IncludesParameter(time));

                // 设置输出
                DA.SetData(0, isIncludedInAny);

                // 只在状态变化时更新锁定
                if (isIncludedInAny != _previousState)
                {
                    _previousState = isIncludedInAny;
                    
                    // 查找组内组件（如果尚未缓存）
                    if (_groupComponents.Count == 0)
                    {
                        CacheGroupComponents();
                    }
                    
                    // 使用单次ScheduleSolution更新锁定状态
                    OnPingDocument()?.ScheduleSolution(1, doc => SetComponentsLock(!isIncludedInAny));
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in IntervalLock SolveInstance: {ex.Message}");
            }
        }

        private void CacheGroupComponents()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var currentGroup = doc.Objects
                .OfType<GH_Group>()
                .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

            if (currentGroup == null) return;

            _groupComponents = currentGroup.ObjectIDs
                .Select(id => doc.FindObject(id, true))
                .Where(obj => obj != null && obj != this && obj is IGH_ActiveObject)
                .Cast<IGH_ActiveObject>()
                .ToList();
        }

        private void SetComponentsLock(bool lockState)
        {
            try
            {
                _groupComponents.ToList().ForEach(delegate (IGH_ActiveObject o)
                {
                    o.Locked = lockState;
                });
                _groupComponents.ToList().ForEach(delegate (IGH_ActiveObject o)
                {
                    o.ExpireSolution(recompute: false);
                });
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in SetComponentsLock: {ex.Message}");
            }
        }

        private void setObjects(bool active)
        {
            
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            if (this.Params.Input[0].Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            var timelineSlider = doc.Objects
                .OfType<MotionSlider>()
                .FirstOrDefault();

            if (timelineSlider != null)
            {
                var timeParam = Params.Input[0];
                timeParam.AddSource(timelineSlider);
                timeParam.WireDisplay = GH_ParamWireDisplay.hidden;
                ExpireSolution(true);
            }
            
            // 缓存组内组件
            document.ScheduleSolution(5, doc => CacheGroupComponents());
        }
        
        public override void RemovedFromDocument(GH_Document document)
        {
            _groupComponents.Clear();
            base.RemovedFromDocument(document);
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override Bitmap Icon => Properties.Resources.IntervalLock;

        public override Guid ComponentGuid => new Guid("F888B3BB-2882-4EEF-861A-E581785A1786");
    }
}