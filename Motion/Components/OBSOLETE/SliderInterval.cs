using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Motion.Animation
{
    public class SliderInterval : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.SliderInterval;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("D2C75940-DF88-4BFD-B398-4A77A488AF27");
        public SliderInterval()
            : base("SliderInterval", "SliderInterval", "获取Slider的区间", "Motion", "01_Animation")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Input Data",
                "I",
                "输入数据,请接入pOd_Slider",
                GH_ParamAccess.item
            );
            pManager.AddNumberParameter(
                "Remote Sender",
                "RS",
                "请接入Telepathy.RemoteSender",
                GH_ParamAccess.item
            );
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Output Data", "O", "输出数据", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Range", "R", "Slider区间", GH_ParamAccess.item);
        }

        public override void AddedToDocument(GH_Document document) //自动连线
        {
            base.AddedToDocument(document);

            GH_Document ghdoc = Grasshopper.Instances.ActiveCanvas.Document;
            if (ghdoc == null)
                return;

            List<IGH_DocumentObject> senderObject_From = this.OnPingDocument()
                .Objects.Where(
                    (IGH_DocumentObject o) =>
                        Grasshopper.Utility.LikeOperator(
                            o.GetType().ToString(),
                            "Motion.MotionSender"
                        )
                )
                .ToList();

            int senderObjectCount = senderObject_From.Count;

            List<IGH_DocumentObject> slider_From = this.OnPingDocument()
                .Objects.Where(
                    (IGH_DocumentObject o) =>
                        Grasshopper.Utility.LikeOperator(
                            o.GetType().ToString(),
                            "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider"
                        )
                )
                .ToList();

            int sliderObjectCount = slider_From.Count;

            PointF nowPivot = this.Attributes.Pivot;
            List<PointF> senderPivots = new List<PointF>();
            List<PointF> sliderPivots = new List<PointF>();
            foreach (var sender in senderObject_From)
            {
                senderPivots.Add(sender.Attributes.Pivot);
            }
            foreach (var slider in slider_From)
            {
                sliderPivots.Add(slider.Attributes.Pivot);
            }

            try
            {
                List<double> senderDistList = new List<double>();
                List<double> sliderDistList = new List<double>();
                for (int i = 0; i < senderPivots.Count; i++)
                {
                    double senderDist = Math.Sqrt(
                        Math.Abs(nowPivot.X - senderPivots[i].X)
                            * Math.Abs(nowPivot.X - senderPivots[i].X)
                            + Math.Abs(nowPivot.Y - senderPivots[i].Y)
                                * Math.Abs(nowPivot.Y - senderPivots[i].Y)
                    );
                    senderDistList.Add(senderDist);
                }
                int senderIndex = senderDistList.IndexOf(senderDistList.Min());

                for (int i = 0; i < sliderPivots.Count; i++)
                {
                    double sliderDist = Math.Abs(nowPivot.Y - sliderPivots[i].Y);
                    sliderDistList.Add(sliderDist);
                }
                int sliderIndex = sliderDistList.IndexOf(sliderDistList.Min());

                GH_Component nowComp = this;

                if (senderDistList[senderIndex] < 60)
                {
                    Param_GenericObject senderParam = (Param_GenericObject)
                        senderObject_From[senderIndex];
                    nowComp.Params.Input[1].AddSource(senderParam);
                }
                if (sliderDistList[sliderIndex] < 350)
                {
                    GH_NumberSlider sliderInstance = (GH_NumberSlider)slider_From[sliderIndex];
                    nowComp.Params.Input[0].AddSource(sliderInstance);
                }
            }
            catch (Exception) { }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;
            double iRemoteSenderData = 0d;

            double oData;
            Interval oRange = Interval.Unset;

            DA.GetData(0, ref iData);
            DA.GetData(0, ref iRemoteSenderData);

            IGH_DocumentObject sliderObject = this.Params
                .Input[0]
                .Sources[0]
                .Attributes
                .GetTopLevel
                .DocObject; //获取上一个电池
            if (
                sliderObject.GetType().ToString()
                == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider"
            ) //如果电池名称为RichGraphMapper
            {
                GH_NumberSlider slider = (GH_NumberSlider)sliderObject;
                Interval range = new Interval(
                    (double)slider.Slider.Minimum,
                    (double)slider.Slider.Maximum
                );
                string rangeStr = range.ToString();
                string[] splitStr = rangeStr.Split(',');
                sliderObject.NickName = string.Join("-", splitStr);
                IGH_DocumentObject senderObject = this.Params.Input[1].Sources[0];

                if (senderObject.GetType().ToString() == "Motion.MotionSender")
                {
                    senderObject.NickName = sliderObject.NickName;
                    Param_GenericObject senderParam = (Param_GenericObject)senderObject;
                    IList<IGH_Param> nextObjectList = senderParam.Recipients;

                    foreach (IGH_Param nextObject in nextObjectList)
                    {
                        if (nextObject.GetType().ToString() == "Motion.Param_RemoteReceiver")
                        {
                            nextObject.NickName = sliderObject.NickName;
                        }
                    }
                }

                oRange = range;
            }

            oData = iData;
            DA.SetData(0, oData);
            DA.SetData(1, oRange);
        }
    }
}
