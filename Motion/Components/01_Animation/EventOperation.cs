using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Motion.Animation
{
    public class EventOperation : GH_Component
    {
        public EventOperation() : base(
            "Event Operation",
            "Event Operation",
            "处理事件序列的操作",
            "Motion", // 替换为您想要的类别
            "01_Animation") // 替换为您想要的子类别
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

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
            List<double> events = new List<double>();

            // 获取输入数据
            if (!DA.GetDataList(0, events)) return;


            // 处理逻辑
            List<double> eventValue = new List<double>();
            List<double> eventStart = new List<double>();
            List<Interval> eventInterval = new List<Interval>();

            var doc = OnPingDocument();
            double timelineSliderValue = (double)doc.Objects
                    .OfType<GH_NumberSlider>()
                    .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase)).Slider.Value;

            for (int i = 0; i < events.Count; i++)
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
                            // 先计算输入值在源区间的比例

                            double t = timelineSliderValue >= timeDomainEnd ? 1 : (timelineSliderValue - timeDomainStart) / timeDomain.Length;
                            // 然后将这个比例映射到目标区间
                            mappedValue = valueDomain.T0 + t * valueDomain.Length;

                            // 确保值在目标区间内
                            mappedValue = Math.Min(Math.Max(mappedValue, valueDomain.Min), valueDomain.Max);
                        }
                        eventValue.Add(mappedValue);
                    }
                }
            }

            sort(eventStart, eventValue, eventInterval, out eventStart, out eventValue, out eventInterval);
            GH_Document ghDocument = OnPingDocument();

            double result = GetCurrentValue(timelineSliderValue, eventInterval, eventValue);

            // 设置输出
            DA.SetData(0, result);
        }

        private void sort(List<double> listA, List<double> listB, List<Interval> listC,
            out List<double> LA_, out List<double> LB_, out List<Interval> LC_)
        {
            List<double> LA = new List<double>();
            List<double> LB = new List<double>();
            List<Interval> LC = new List<Interval>();

            if (listA.Count < 2)
            {
                LA = listA;
                LB = listB;
                LC = listC;
            }
            else
            {
                LA.Add(listA[0]);
                LB.Add(listB[0]);
                LC.Add(listC[0]);
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
                            sign = false;
                            break;
                        }
                    }
                    if (sign)
                    {
                        LA.Add(listA[i]);
                        LB.Add(listB[i]);
                        LC.Add(listC[i]);
                    }
                }
            }

            LA_ = LA;
            LB_ = LB;
            LC_ = LC;
        }

        private double GetCurrentValue(double time, List<Interval> eventInterval, List<double> eventValue)
        {
            bool sign = true;
            double currentValue = eventValue[0];

            for (int i = 0; i < eventInterval.Count - 1; i++)
            {
                Interval dom = new Interval(eventInterval[i].T0, eventInterval[i + 1].T0);
                if (dom.IncludesParameter(time, false))
                {
                    currentValue = eventValue[i];
                    sign = false;
                }
            }

            if (sign)
            {
                if (time >= eventInterval[eventInterval.Count - 1].T0)
                {
                    currentValue = eventValue[eventInterval.Count - 1];
                }
            }
            return currentValue;
        }
    }
}