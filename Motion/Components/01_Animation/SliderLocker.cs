using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using System;
using System.Linq;

namespace Motion
{

    public class SliderLocker : GH_Component
    {
        public SliderLocker()
          : base("SliderLocker", "SliderLocker",
            "判断主Slider数值是否在子Slider对应区间内",
            "Motion", "01_Animation")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sub Slider", "S", "pOd子Slider", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("isLock?", "L?", "是否锁定", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool oLock = false;

            this.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;//把第一个输入端导线改为hidden模式

            IGH_DocumentObject unionSliderObject = this.OnPingDocument().Objects.Where((IGH_DocumentObject o)
                => Grasshopper.Utility.LikeOperator(o.NickName, "TimeLine(Union)")).ToList()[0];

            if (unionSliderObject.GetType().ToString() == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider")//如果是pod slider
            {
                GH_NumberSlider unionSlider = (GH_NumberSlider)unionSliderObject;//新建实例，赋值

                IGH_DocumentObject receiverObject = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject;
                if (receiverObject.GetType().ToString() == "Telepathy.Param_RemoteReceiver")
                {
                    Param_GenericObject receiverParam = (Param_GenericObject)receiverObject;
                    double unionSliderValue = (double)unionSlider.Slider.Value;
                    string[] splitedReceiverNickname = receiverParam.NickName.Split('-');
                    if (unionSliderValue >= Convert.ToDouble(splitedReceiverNickname[0])
                        && unionSliderValue <= Convert.ToDouble(splitedReceiverNickname[1]))
                    {
                        oLock = true;
                    }
                    else oLock = false;
                }
            }
            this.Message = oLock.ToString();
            DA.SetData(0, oLock);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.SliderLocker;

        public override Guid ComponentGuid => new Guid("2FB92C8E-D75F-48B7-9F46-4365FD33A621");
    }

    
}