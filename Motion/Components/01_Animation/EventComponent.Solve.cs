using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;

namespace Motion.Animation
{
    public partial class EventComponent
    {
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Time", "T", "时间参数", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Domain", "D", "区间参数", GH_ParamAccess.item, new Interval(0, 1));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "当前时间在区间内的比例值", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            if (!_isInitialized)
            {
                InitializeAfterLoad();
            }
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!_isInitialized || _timelineSlider == null)
            {
                InitializeAfterLoad();
            }

            // 检查第一个输入端是否为空
            bool hasData = !this.Params.Input[0].VolatileData.IsEmpty;

            if (!hasData)
            {
                // 在空值模式下，直接更新状态
                if (UseEmptyValueMode)
                {
                    UpdateGroupVisibilityAndLock();
                }
                return;
            }
            IGH_DocumentObject targetOperation = null;

            if (Params.Output[0].Recipients.Count == 0) return;

            foreach (var recipient in Params.Output[0].Recipients)
            {
                var topLevelObj = recipient.Attributes.GetTopLevel.DocObject;

                // 处理 Graph Mapper 的情况
                var graphMapper = topLevelObj as GH_GraphMapper;
                if (graphMapper != null)
                {
                    if (graphMapper.Recipients.Count > 0)
                    {
                        targetOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                    }
                }
                // 处理 Component 的情况
                else
                {
                    var component = topLevelObj as GH_Component;
                    if (component != null && component.Params.Input.Count > 0)
                    {
                        bool isGraphMapperPlus = component.ComponentGuid == new Guid("310f9597-267e-4471-a7d7-048725557528");
                        IGH_Param inputParameter = component.Params.Input[isGraphMapperPlus ? 2 : 0];

                        if (inputParameter.Sources.Contains(this.Params.Output[0]))
                        {
                            targetOperation = component;
                        }
                    }
                }

                if (targetOperation == null)
                {
                    _hasEventOperation = false;
                    continue;
                }

                _hasEventOperation = true;
                break;
            }
            if (!_hasEventOperation)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "没有找到关联 of EventOperation 组件");
            }

            double time = 0;
            Interval domain = new Interval(0, 1);

            // 获取输入数据
            if (!DA.GetData(0, ref time)) return;
            if (!DA.GetData(1, ref domain)) return;
            
            // 从NickName解析区间
            string[] parts = this.NickName.Split('-');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                // 用Timeline Slider模式
                if (_timelineSlider != null)
                {
                    double timelineSliderValue = (double)_timelineSlider.CurrentValue;
                    // 计算当前是否在区间内
                    bool currentInInterval = timelineSliderValue > min && timelineSliderValue < max;

                    // 计算比例值
                    double value;
                    if (time <= min)
                        value = 0;
                    else if (time >= max)
                        value = 1;
                    else
                        value = (time - min) / (max - min);

                    // 输出计算得到的值
                    DA.SetData(0, value);

                    // 只在区间状态发生变化时更新
                    if (currentInInterval != _lastInInterval)
                    {
                        UpdateGroupVisibilityAndLock();
                    }
                    // 非空值模式下的处理
                    if (!UseEmptyValueMode)
                    {
                        // 更新状态
                        _lastInInterval = currentInInterval;
                        _lastHasData = true;
                    }
                    // 更新Message以显示当前时间值
                    this.Message = $"[{min}-{max}]\n{value:F2}";
                }
            }
            else
            {
                // 空值模式下，有数据时也需要更新状态
                if (_lastHasData != true)
                {
                    UpdateGroupVisibilityAndLock();
                }
                _lastHasData = true;
            }
        }
    }
}
