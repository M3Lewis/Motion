using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.GUI.Canvas;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Motion.Motility
{
    public class EventOperation : GH_Component
    {
        public List<EventComponent> ChildEvent = new List<EventComponent>();
        private double _currentEventValue = 0;
        private double _currentMappedEventValue = 0;
        public EventOperation() : base(
            "Event Operation",
            "Event Operation",
            "处理事件序列的操作",
            "Motion", // 替换为您想要的类别
            "04_Motility") // 替换为您想要的子类别
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("4293226C-974C-4D88-A5BB-0231347BDD5D");

        protected override Bitmap Icon => Properties.Resources.EventOperation;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Events", "E", "事件值列表", GH_ParamAccess.list);
            pManager.AddNumberParameter("Time", "T", "当前时间", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "当前事件值", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 声明输入变量
            List<double> eventValues = new List<double>();

            // 获取输入数据
            if (!DA.GetDataList(0, eventValues)) return;


            // 处理逻辑
            List<double> mappedEventValues = new List<double>();
            List<double> eventStart = new List<double>();
            List<Interval> eventInterval = new List<Interval>();
            Interval currentInterval = new Interval(0, 1);
            double currentEventValue = 0;

            var doc = OnPingDocument();
            double timelineSliderValue = (double)doc.Objects
                    .OfType<GH_NumberSlider>()
                    .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase)).Slider.Value;

            for (int i = 0; i < eventValues.Count; i++)
            {
                IGH_DocumentObject graphMapperDocumentObject = Params.Input[0].Sources[i].Attributes.GetTopLevel.DocObject;
                if (graphMapperDocumentObject.GetType().ToString() == "Grasshopper.Kernel.Special.GH_GraphMapper")
                {
                    var graphMapper = (Grasshopper.Kernel.Special.GH_GraphMapper)graphMapperDocumentObject;

                    graphMapper.WireDisplay = GH_ParamWireDisplay.faint;
                    IGH_DocumentObject eventComponentDocumentObject = graphMapper.Sources[0].Attributes.GetTopLevel.DocObject;
                    if (eventComponentDocumentObject.GetType().ToString() == "Motion.Motility.EventComponent")
                    {
                        IGH_Component eventComponent = (IGH_Component)eventComponentDocumentObject;

                        string eventComponentNickName = eventComponent.NickName;
                        string[] timeDomainExtremes = eventComponentNickName.Split('-');

                        double timeDomainStart = double.Parse(timeDomainExtremes[0]);
                        double timeDomainEnd = double.Parse(timeDomainExtremes[1]);
                        Interval timeDomain = new Interval(timeDomainStart, timeDomainEnd);
                        eventStart.Add(timeDomainStart);
                        eventInterval.Add(timeDomain);

                        GH_Interval ghValueDomain = eventComponent.Params.Input[1].VolatileData.get_Branch(0)[0] as GH_Interval;
                        Interval valueDomain = ghValueDomain.Value;

                        // 计算映射值
                        double mappedValue;
                        if (valueDomain.IsSingleton || valueDomain.IsSingleton)
                        {
                            // 处理单点区间的情况
                            mappedValue = valueDomain.Mid;
                        }
                        else
                        {
                            //注意，这里eventValues[i]就是Graph Mapper的Y值。之前Event求的是Graph Mapper的X值。
                            mappedValue = valueDomain.T0 + eventValues[i] * valueDomain.Length;

                            // 确保值在目标区间内
                            mappedValue = Math.Min(Math.Max(mappedValue, valueDomain.Min), valueDomain.Max);
                        }
                        mappedEventValues.Add(mappedValue);
                    }
                }
            }

            sort(eventStart, mappedEventValues, eventInterval, eventValues, out eventStart, out mappedEventValues, out eventInterval, out eventValues);
            GH_Document ghDocument = OnPingDocument();

            double result = GetCurrentValue(timelineSliderValue, eventInterval, mappedEventValues, eventValues, out currentInterval, out currentEventValue);
            _currentEventValue = currentEventValue;
            _currentMappedEventValue = result;
            this.Message = $"【{currentInterval.T0}-{currentInterval.T1}】";
            
            // 设置输出
            DA.SetData(0, result);

            // 强制重绘组件
            this.OnDisplayExpired(true);
        }
        private void sort(List<double> listA, List<double> listB, List<Interval> listC, List<double> listD,
            out List<double> LA_, out List<double> LB_, out List<Interval> LC_, out List<double> LD_)
        {
            List<double> LA = new List<double>();
            List<double> LB = new List<double>();
            List<Interval> LC = new List<Interval>();
            List<double> LD = new List<double>();

            if (listA.Count < 2)
            {
                LA = listA;
                LB = listB;
                LC = listC;
                LD = listD;
            }
            else
            {
                LA.Add(listA[0]);
                LB.Add(listB[0]);
                LC.Add(listC[0]);
                LD.Add(listD[0]);
                for (int i = 1; i < listA.Count; i++)
                {
                    bool sign = true;
                    for (int j = 0; j < LA.Count; j++)
                    {
                        if (listA[i] < LA[j])
                        {
                            LA.Insert(j, listA[i]);
                            LB.Insert(j, listB[i]);
                            LC.Insert(j, listC[i]);
                            LD.Insert(j, listD[i]);
                            sign = false;
                            break;
                        }
                    }
                    if (sign)
                    {
                        LA.Add(listA[i]);
                        LB.Add(listB[i]);
                        LC.Add(listC[i]);
                        LD.Add(listD[i]);
                    }
                }
            }

            LA_ = LA;
            LB_ = LB;
            LC_ = LC;
            LD_ = LD;
        }

        private double GetCurrentValue(double time, List<Interval> eventInterval, List<double> mappedEventValues, List<double> eventValues, out Interval currentInterval, out double currentEventValue)
        {
            bool sign = true;
            double currentValue = mappedEventValues[0];
            currentInterval = new Interval(0, 1);
            currentEventValue = eventValues[0];

            for (int i = 0; i < eventInterval.Count - 1; i++)
            {
                Interval dom = new Interval(eventInterval[i].T0, eventInterval[i + 1].T0);
                if (dom.IncludesParameter(time, false))
                {
                    currentValue = mappedEventValues[i];
                    currentInterval = dom;
                    currentEventValue = eventValues[i];
                    sign = false;
                    break;
                }
            }

            if (sign)
            {
                if (time >= eventInterval[eventInterval.Count - 1].T0)
                {
                    currentValue = mappedEventValues[eventInterval.Count - 1];
                    currentInterval = eventInterval[eventInterval.Count - 1];
                    currentEventValue = eventValues[eventValues.Count - 1];
                }
            }

            return currentValue;
        }

        // 添加一个公共属性来访问 _currentEventValue
        public double CurrentEventValue => _currentEventValue;
        public double CurrentMappedEventValue => _currentMappedEventValue;
        // 重写 CreateAttributes 方法
        public override void CreateAttributes()
        {
            m_attributes = new EventOperationAttributes(this);
        }
    }
}