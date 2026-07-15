using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Linq;
using Motion.General;

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

            double time = 0;
            Interval domain = new Interval(0, 1);

            // 获取输入数据
            if (!DA.GetData(0, ref time)) return;
            if (!DA.GetData(1, ref domain)) return;

            // 检查下游 EventOperation 连接情况
            CheckDownstreamEventOperation();

            // 从 NickName 解析区间
            if (!MotilityUtils.TryParseNickNameInterval(this.NickName, out double min, out double max))
            {
                return;
            }

            if (_timelineSlider == null) return;

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

            // 更新状态变量与面板消息
            _lastInInterval = currentInInterval;
            this.Message = $"[{min}-{max}]\n{value:F2}";
        }

        private void CheckDownstreamEventOperation()
        {
            if (Params.Output[0].Recipients.Count == 0)
            {
                _hasEventOperation = false;
                return;
            }

            foreach (var recipient in Params.Output[0].Recipients)
            {
                var topLevelObj = recipient.Attributes.GetTopLevel.DocObject;
                var graphMapper = topLevelObj as GH_GraphMapper;
                IGH_DocumentObject targetOperation = null;

                if (graphMapper != null)
                {
                    if (graphMapper.Recipients.Count > 0)
                    {
                        targetOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                    }
                }
                else
                {
                    var component = topLevelObj as GH_Component;
                    if (component != null && component.Params.Input.Count > 0)
                    {
                        var handler = GraphTypeHandlerRegistry.FindByGuid(component.ComponentGuid);
                        int portIndex = handler?.InputPortIndex ?? 0;
                        IGH_Param inputParameter = component.Params.Input[portIndex];
                        if (inputParameter.Sources.Contains(this.Params.Output[0]))
                        {
                            targetOperation = component;
                        }
                    }
                }

                if (targetOperation != null)
                {
                    _hasEventOperation = true;
                    return;
                }
            }

            _hasEventOperation = false;
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "没有找到关联 of EventOperation 组件");
        }
    }
}