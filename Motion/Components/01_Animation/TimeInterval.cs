using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class TimeInterval : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.TimeInterval;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("D2C75940-DF88-4BFD-B398-4A77A488AF27");
        public TimeInterval()
            : base("Time Interval", "Time Interval", "获取时间区间", "Motion", "01_Animation")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Input Data",
                "I",
                "输入数据，可接入Motion Sender/Event的输出端",
                GH_ParamAccess.item
            );
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntervalParameter("Range", "R", "时间区间", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;

            Interval oRange = Interval.Unset;

            DA.GetData(0, ref iData);

            IGH_DocumentObject obj = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject; //获取上一个电池
            string rangeStr = "";
            if (obj is MotionSender || obj is EventComponent)
            {
                rangeStr = obj.NickName;
            }
            else if (obj is MotionSlider)
            { 
                rangeStr = obj.NickName;
            }
            string[] splitStr = rangeStr.Split('-');
            double start = double.Parse(splitStr[0]);
            double end = double.Parse(splitStr[1]);
            oRange = new Interval(start, end);

            DA.SetData(0, oRange);
        }
    }
}
