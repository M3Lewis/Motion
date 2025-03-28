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
        private Guid? _currentGroupId = null;
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
                // 检查组内组件数量是否发生变化
                var doc = OnPingDocument();
                if (doc != null)
                {
                    var currentGroup = doc.Objects
                        .OfType<GH_Group>()
                        .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

                    if (currentGroup != null)
                    {
                        // 获取当前组内的所有活动组件
                        var currentComponents = currentGroup.ObjectIDs
                            .Select(id => doc.FindObject(id, true))
                            .Where(obj => obj != null && obj != this && obj is IGH_ActiveObject)
                            .Cast<IGH_ActiveObject>()
                            .ToList();

                        // 检查是否有新组件加入
                        var newComponents = currentComponents.Except(_groupComponents).ToList();
                        if (newComponents.Any())
                        {
                            // 更新缓存列表
                            _groupComponents = currentComponents;
                            // 对新加入的组件应用当前的锁定状态
                            foreach (var component in newComponents)
                            {
                                component.Locked = !_previousState;
                                component.ExpireSolution(false);
                            }
                        }
                        // 检查是否有组件被移除
                        else if (currentComponents.Count != _groupComponents.Count)
                        {
                            var removedComponents = _groupComponents.Except(currentComponents).ToList();
                            foreach (var component in removedComponents)
                            {
                                if (component != null)
                                {
                                    component.Locked = false;
                                    component.ExpireSolution(false);
                                }
                            }
                            _groupComponents = currentComponents;
                        }
                    }
                }

                double time = 0;
                var domains = new List<Interval>();

                if (!DA.GetData(0, ref time)) return;
                if (!DA.GetDataList(1, domains)) return;

                // 检查时间是否在任何区间内
                bool isIncludedInAny = domains.Any(domain => domain.IncludesParameter(time) && domain.Length != 0);

                // 设置输出
                DA.SetData(0, isIncludedInAny);

                // 更新组件列表（如果尚未缓存）
                if (_groupComponents.Count == 0)
                {
                    CacheGroupComponents();
                }

                // 每次都更新锁定状态
                if (_groupComponents.Count > 0)
                {
                    OnPingDocument()?.ScheduleSolution(1, doc => SetComponentsLock(!isIncludedInAny));
                }

                // 更新状态记录
                _previousState = isIncludedInAny;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in IntervalLock SolveInstance: {ex.Message}");
            }
        }

        private void CacheGroupComponents()
        {
            _groupComponents.Clear();

            var doc = OnPingDocument();
            if (doc == null) return;

            var currentGroup = doc.Objects
                .OfType<GH_Group>()
                .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

            if (currentGroup == null)
            {
                _currentGroupId = null;
                return;
            }

            _currentGroupId = currentGroup.InstanceGuid;
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

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            // 添加文档事件监听
            document.ObjectsAdded += Document_ObjectsChanged;
            document.ObjectsDeleted += Document_ObjectsChanged;
            document.ObjectsDeleted += Document_ObjectsDeleted;

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

        private void Document_ObjectsChanged(object sender, GH_DocObjectEventArgs e)
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找当前组
            var currentGroup = doc.Objects
                .OfType<GH_Group>()
                .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

            // 解锁所有旧组件
            foreach (var component in _groupComponents)
            {
                if (component != null)
                {
                    component.Locked = false;
                    component.ExpireSolution(false);
                }
            }

            // 清空并更新组件列表
            _groupComponents.Clear();
            _currentGroupId = currentGroup?.InstanceGuid;

            // 如果存在新组，添加新组件
            if (currentGroup != null)
            {
                _groupComponents = currentGroup.ObjectIDs
                    .Select(id => doc.FindObject(id, true))
                    .Where(obj => obj != null && obj != this && obj is IGH_ActiveObject)
                    .Cast<IGH_ActiveObject>()
                    .ToList();
            }

            // 根据当前状态设置新组件的锁定状态
            if (_groupComponents.Count > 0)
            {
                OnPingDocument()?.ScheduleSolution(1, doc => SetComponentsLock(!_previousState));
            }
        }

        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            // 当有组件被删除时，检查是否需要更新缓存
            if (_groupComponents.Any(comp => e.Objects.Any(obj => obj.InstanceGuid == comp.InstanceGuid)))
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(5, d => CacheGroupComponents());
                }
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (document != null)
            {
                document.ObjectsAdded -= Document_ObjectsChanged;
                document.ObjectsDeleted -= Document_ObjectsChanged;
                document.ObjectsDeleted -= Document_ObjectsDeleted;
            }

            _groupComponents.Clear();
            _currentGroupId = null;
            base.RemovedFromDocument(document);
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override Bitmap Icon => Properties.Resources.IntervalLock;

        public override Guid ComponentGuid => new Guid("F888B3BB-2882-4EEF-861A-E581785A1786");
    }
}