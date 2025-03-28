using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class EventOperation : GH_Component, IGH_VariableParameterComponent
    {
        public List<EventComponent> ChildEvent = new List<EventComponent>();
        private double _currentEventValue = 0;
        private double _currentMappedEventValue = 0;
        private int _currentEventIndex = 0;

        public string GroupMessage { get; private set; }
        public EventOperation() : base(
            "Event Operation",
            "Event Operation",
            "处理事件序列的操作\n" +
            "放大组件 可以添加额外的[当前事件序号]/[当前事件区间]输出端",
            "Motion", // 替换为您想要的类别
            "01_Animation") // 替换为您想要的子类别
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("4293226C-974C-4D88-A5BB-0231347BDD5D");

        protected override Bitmap Icon => Properties.Resources.EventOperation;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendSeparator(menu);
            ToolStripMenuItem namingCurrentGroup = Menu_AppendItem(menu, "命名当前组名称为Event名称");
            namingCurrentGroup.Click += (sender, e) =>
            {
                // 获取组件所在的组
                var currentGroup = Instances.ActiveCanvas.Document.Objects
                    .OfType<GH_Group>()
                    .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));
                currentGroup.NickName = GroupMessage.Trim();
            };
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Events", "E", "事件值列表", GH_ParamAccess.list);
            pManager.AddNumberParameter("Time", "T", "当前时间", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Remapped Value", "R", "当前事件值(映射)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Value", "V", "当前事件值(0-1)", GH_ParamAccess.item);
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            if (side != GH_ParameterSide.Output) return false;

            // 获取当前输出端的状态
            bool hasIndex = Params.Output.Any(p => p.Name == "Index");
            bool hasValueDomain = Params.Output.Any(p => p.Name == "Value Domain");
            bool hasTimeDomain = Params.Output.Any(p => p.Name == "Time Domain");

            // 只允许在最后一个参数后添加新参数
            if (index != Params.Output.Count) return false;

            // 如果三个可选参数都不存在
            if (!hasIndex && !hasValueDomain && !hasTimeDomain) return true;

            // 如果只有一个或两个可选参数存在
            if (Params.Output.Count < 5) return true;

            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            // 允许移除 Index、Value Domain 和 Time Domain 输出端
            return side == GH_ParameterSide.Output && (index == 2 || index == 3 || index == 4);
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Output)
            {
                // 检查当前已存在的参数
                bool hasIndex = Params.Output.Any(p => p.Name == "Index");
                bool hasValueDomain = Params.Output.Any(p => p.Name == "Value Domain");
                bool hasTimeDomain = Params.Output.Any(p => p.Name == "Time Domain");

                // 优先创建 Index
                if (!hasIndex)
                {
                    return new Param_Integer
                    {
                        Name = "Index",
                        NickName = "I",
                        Description = "当前事件在序列中的序号",
                        Access = GH_ParamAccess.item,
                        Optional = true
                    };
                }
                // 其次创建 Value Domain
                else if (!hasValueDomain)
                {
                    return new Param_Interval
                    {
                        Name = "Value Domain",
                        NickName = "VD",
                        Description = "当前事件的值域区间",
                        Access = GH_ParamAccess.item,
                        Optional = true
                    };
                }
                // 最后创建 Time Domain
                else if (!hasTimeDomain)
                {
                    return new Param_Interval
                    {
                        Name = "Time Domain",
                        NickName = "TD",
                        Description = "当前事件的时间区间",
                        Access = GH_ParamAccess.item,
                        Optional = true
                    };
                }
            }
            return null;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true; // 允许销毁参数
        }

        public void VariableParameterMaintenance()
        {
            // 维护可选输出参数的属性
            foreach (var param in Params.Output)
            {
                switch (param.Name)
                {
                    case "Index":
                        param.Name = "Index";
                        param.NickName = "I";
                        param.Description = "当前事件在序列中的序号";
                        param.Access = GH_ParamAccess.item;
                        param.Optional = true;
                        break;
                    case "Value Domain":
                        param.Name = "Value Domain";
                        param.NickName = "VD";
                        param.Description = "当前事件的值域区间";
                        param.Access = GH_ParamAccess.item;
                        param.Optional = true;
                        break;
                    case "Time Domain":
                        param.Name = "Time Domain";
                        param.NickName = "TD";
                        param.Description = "当前事件的时间区间";
                        param.Access = GH_ParamAccess.item;
                        param.Optional = true;
                        break;
                }
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 检查时间输入端是否已连接到 TimeLine(Union) Slider
            var timeParam = Params.Input[1];

            var timelineSlider = OnPingDocument()?.Objects
                .OfType<MotionSlider>()
                .FirstOrDefault();

            if (timelineSlider == null || !timeParam.Sources.Contains(timelineSlider))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "请放置 MotionSlider 并连接到时间输入端");
                return;
            }

            // 1. 预分配容器大小以避免动态扩容
            List<double> eventValues = new List<double>();
            if (!DA.GetDataList(0, eventValues)) return;

            int capacity = eventValues.Count;
            var mappedEventValues = new List<double>(capacity);
            var eventStart = new List<double>(capacity);
            var eventInterval = new List<Interval>(capacity);
            var isReversedIntervals = new List<bool>(capacity);
            var valueDomains = new List<Interval>(capacity);

            Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            // 2. 缓存常用对象
            var doc = OnPingDocument();

            if (timelineSlider == null) return;
            double timelineSliderValue = (double)timelineSlider.Slider.Value;

            // 3. 优化循环
            var sources = Params.Input[0].Sources;
            var groupNames = new List<string>();  // 用于收集 Group 昵称

            // 获取所有 Group
            var allGroups = doc.Objects.OfType<GH_Group>().ToList();

            for (int i = 0; i < eventValues.Count; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                // 获取顶层对象
                var topLevelObj = source.Attributes.GetTopLevel.DocObject;
                
                // 处理 Graph Mapper 的情况
                var graphMapper = topLevelObj as GH_GraphMapper;
                EventComponent eventComponent = null;

                if (graphMapper != null)
                {
                    // Graph Mapper 情况
                    graphMapper.WireDisplay = GH_ParamWireDisplay.faint;
                    eventComponent = graphMapper.Sources[0]?.Attributes.GetTopLevel.DocObject as EventComponent;
                }
                else
                {
                    // GH_Component 情况
                    var component = topLevelObj as GH_Component;
                    if (component != null && component.Params.Input.Count > 0)
                    {
                        var firstInput = component.Params.Input[0];
                        if (firstInput.SourceCount > 0)
                        {
                            eventComponent = firstInput.Sources[0]?.Attributes.GetTopLevel.DocObject as EventComponent;
                        }
                        component.Params.Input[0].WireDisplay = GH_ParamWireDisplay.faint;
                    }
                }

                if (eventComponent == null) continue;

                // 查找包含这个组件的 Group
                var containingGroup = allGroups.FirstOrDefault(g => 
                    g.ObjectIDs.Contains(graphMapper?.InstanceGuid ?? topLevelObj.InstanceGuid));
                if (containingGroup != null && !string.IsNullOrEmpty(containingGroup.NickName))
                {
                    groupNames.Add(containingGroup.NickName);
                }

                // 4. 使用 Split 的重载版本避免创建新数组
                string[] timeDomainExtremes = eventComponent.NickName.Split(new char[] { '-' }, 2);
                if (timeDomainExtremes.Length != 2) continue;

                if (!double.TryParse(timeDomainExtremes[0], out double timeDomainStart) ||
                    !double.TryParse(timeDomainExtremes[1], out double timeDomainEnd))
                    continue;

                var timeDomain = new Interval(timeDomainStart, timeDomainEnd);
                eventStart.Add(timeDomainStart);
                eventInterval.Add(timeDomain);

                var ghValueDomain = eventComponent.Params.Input[1].VolatileData.get_Branch(0)[0] as GH_Interval;
                if (ghValueDomain == null) continue;

                Interval valueDomain = ghValueDomain.Value;
                valueDomains.Add(valueDomain);
                isReversedIntervals.Add(valueDomain.T0 > valueDomain.T1);

                // 5. 优化数值计算
                double mappedValue = valueDomain.IsSingleton
                    ? valueDomain.Mid
                    : Math.Min(Math.Max(valueDomain.T0 + eventValues[i] * valueDomain.Length, valueDomain.Min), valueDomain.Max);

                mappedEventValues.Add(mappedValue);
            }

            // 6. 避免不必要的排序
            if (eventStart.Count == 0) return;

            sort(eventStart, mappedEventValues, eventInterval, eventValues, isReversedIntervals, valueDomains,
                out var sortedStartTimes, out var sortedMappedValues, out var sortedTimeIntervals,
                out var sortedEventValues, out var sortedIsReversed, out var sortedValueDomains);

            // 7. 优化输出处理
            double result = GetCurrentValue(timelineSliderValue, sortedTimeIntervals, sortedMappedValues,
                sortedEventValues, sortedIsReversed, sortedValueDomains,
                out var currentInterval, out var currentEventValue, out var currentIndex, out var currentValueDomain);

            _currentEventValue = currentEventValue;
            _currentMappedEventValue = result;
            _currentEventIndex = currentIndex;

            // 8. 优化条件判断并更新消息
            bool isInEventInterval = currentInterval.IncludesParameter(timelineSliderValue);

            // 构建消息
            string intervalMessage = !isInEventInterval
                ? "OUTSIDE" 
                : $"【{currentInterval.T0}-{currentInterval.T1}】";
            
            // 添加 Group 昵称
            string groupMessage = groupNames.Count > 0 
                ? $"\n{string.Join("\n", groupNames.Distinct())}" 
                : "";

            GroupMessage = groupMessage;
            this.Message = intervalMessage + groupMessage;

            // 9. 优化输出赋值
            DA.SetData(0, result);
            DA.SetData(1, currentEventValue);

            var outputParams = Params.Output;
            if (outputParams.Count > 2)
            {
                if (outputParams[2].Name == "Index")
                    DA.SetData(2, currentIndex);
                if (outputParams.Count > 3 && outputParams[3].Name == "Value Domain")
                    DA.SetData(3, currentValueDomain);
                if (outputParams.Count > 4 && outputParams[4].Name == "Time Domain")
                    DA.SetData(4, isInEventInterval ? currentInterval : null);
            }
        }

        private double GetCurrentValue(
            double time,
            List<Interval> eventInterval,
            List<double> mappedEventValues,
            List<double> eventValues,
            List<bool> isReversedIntervals,
            List<Interval> valueDomains,
            out Interval currentInterval,
            out double currentEventValue,
            out int currentIndex,
            out Interval currentDomain)
        {
            // 初始化为第一个值
            double currentValue = mappedEventValues[0];
            currentInterval = new Interval(0, 1);
            currentEventValue = isReversedIntervals[0] ? 1 - eventValues[0] : eventValues[0];
            currentIndex = 0;
            currentDomain = valueDomains[0];

            // 找到最后一个小于当前时间的区间
            int lastValidIndex = -1;
            for (int i = 0; i < eventInterval.Count; i++)
            {
                Interval dom = eventInterval[i];
                if (dom.IncludesParameter(time, false))
                {
                    // 如果时间在区间内，直接使用该区间
                    currentValue = mappedEventValues[i];
                    currentInterval = dom;
                    currentEventValue = isReversedIntervals[i] ? 1 - eventValues[i] : eventValues[i];
                    currentIndex = i;
                    currentDomain = valueDomains[i];
                    return currentValue;
                }
                else
                {
                    currentInterval = Interval.Unset;
                }
                // 记录最后一个结束时间小于当前时间的区间
                if (dom.T1 <= time)
                {
                    lastValidIndex = i;
                }

            }

            // 如果没有找到包含当前时间的区间，使用最后一个有效区间的值
            if (lastValidIndex != -1)
            {
                currentValue = mappedEventValues[lastValidIndex];
                currentInterval = eventInterval[lastValidIndex];
                currentEventValue = isReversedIntervals[lastValidIndex] ? 1 - eventValues[lastValidIndex] : eventValues[lastValidIndex];
                currentIndex = lastValidIndex;
                currentDomain = valueDomains[lastValidIndex];
            }

            return currentValue;
        }

        private void sort(
            List<double> eventStartTimes,
            List<double> mappedValues,
            List<Interval> timeIntervals,
            List<double> originalValues,
            List<bool> isReversedIntervals,
            List<Interval> valueDomains,  // 新增：值域列表
            out List<double> sortedStartTimes,
            out List<double> sortedMappedValues,
            out List<Interval> sortedTimeIntervals,
            out List<double> sortedOriginalValues,
            out List<bool> sortedIsReversed,
            out List<Interval> sortedValueDomains)  // 新增：排序后的值域列表
        {
            // 创建新的排序后列表
            List<double> newStartTimes = new List<double>();
            List<double> newMappedValues = new List<double>();
            List<Interval> newTimeIntervals = new List<Interval>();
            List<double> newOriginalValues = new List<double>();
            List<bool> newIsReversed = new List<bool>();
            List<Interval> newValueDomains = new List<Interval>();  // 新增：新的值域列表

            if (eventStartTimes.Count < 2)
            {
                // 如果列表长度小于2，无需排序
                sortedStartTimes = eventStartTimes;
                sortedMappedValues = mappedValues;
                sortedTimeIntervals = timeIntervals;
                sortedOriginalValues = originalValues;
                sortedIsReversed = isReversedIntervals;
                sortedValueDomains = valueDomains;
            }
            else
            {
                // 添加第一个元素
                newStartTimes.Add(eventStartTimes[0]);
                newMappedValues.Add(mappedValues[0]);
                newTimeIntervals.Add(timeIntervals[0]);
                newOriginalValues.Add(originalValues[0]);
                newIsReversed.Add(isReversedIntervals[0]);
                newValueDomains.Add(valueDomains[0]);

                // 插入排序
                for (int i = 1; i < eventStartTimes.Count; i++)
                {
                    bool inserted = false;
                    for (int j = 0; j < newStartTimes.Count; j++)
                    {
                        if (eventStartTimes[i] < newStartTimes[j])
                        {
                            newStartTimes.Insert(j, eventStartTimes[i]);
                            newMappedValues.Insert(j, mappedValues[i]);
                            newTimeIntervals.Insert(j, timeIntervals[i]);
                            newOriginalValues.Insert(j, originalValues[i]);
                            newIsReversed.Insert(j, isReversedIntervals[i]);
                            newValueDomains.Insert(j, valueDomains[i]);
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted)
                    {
                        newStartTimes.Add(eventStartTimes[i]);
                        newMappedValues.Add(mappedValues[i]);
                        newTimeIntervals.Add(timeIntervals[i]);
                        newOriginalValues.Add(originalValues[i]);
                        newIsReversed.Add(isReversedIntervals[i]);
                        newValueDomains.Add(valueDomains[i]);
                    }
                }

                sortedStartTimes = newStartTimes;
                sortedMappedValues = newMappedValues;
                sortedTimeIntervals = newTimeIntervals;
                sortedOriginalValues = newOriginalValues;
                sortedIsReversed = newIsReversed;
                sortedValueDomains = newValueDomains;
            }
        }

        public double CurrentEventValue => _currentEventValue;
        public double CurrentMappedEventValue => _currentMappedEventValue;

        

        public override void CreateAttributes()
        {
            m_attributes = new EventOperationAttributes(this);

        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            ConnectToTimelineSlider();
        }

        private void ConnectToTimelineSlider()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找 TimeLine(Union) Slider
            var timelineSlider = doc.Objects
                .OfType<MotionSlider>()
                .FirstOrDefault();

            if (timelineSlider != null)
            {
                // 获取时间输入参数
                var timeParam = Params.Input[1];  // "Time" 末端

                // 检查是否已经连接
                if (!timeParam.Sources.Any())
                {
                    // 添加数据连接
                    timeParam.AddSource(timelineSlider);

                    // 设置连线显示类型为隐藏
                    timeParam.WireDisplay = GH_ParamWireDisplay.hidden;

                    // 强制更新组件
                    ExpireSolution(true);
                }
            }
        }
    }
}