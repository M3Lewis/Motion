using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Parameters;

namespace Motion
{
    public class GraphMapperChanger : GH_Component
    {
        public GraphMapperChanger()
          : base("GraphMapperChanger", "GraphMapperChanger",
            "更改GraphMapper的X区间为前置pOd_Timeline Slider的区间，Y区间为输入值",
            "Motion", "01_Animation")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("GraphMapper Data", "G", "GraphMapper数据", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Y Domain", "D", "Y轴目标区间", GH_ParamAccess.item, new Interval(0, 1));
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("outValue", "V", "输出值", GH_ParamAccess.item);
        }

        public bool expired = false;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;
            Interval iDomain = new Interval(0, 1);
            double oData = 0d;
            DA.GetData(0, ref iData);
            DA.GetData(1, ref iDomain);

            GH_Document ghdoc = this.OnPingDocument();



            IGH_DocumentObject mapperObject = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject;//获取上一个电池
            if (mapperObject.GetType().ToString() == "Grasshopper.Kernel.Special.GH_GraphMapper")//如果电池名称为RichGraphMapper
            {
                GH_GraphMapper graphMapper = (GH_GraphMapper)mapperObject;
                GH_GraphContainer container = graphMapper.Container;
                container.Y0 = iDomain.T0;
                container.Y1 = iDomain.T1;

                graphMapper.WireDisplay = GH_ParamWireDisplay.faint;

                IGH_DocumentObject mapperSource = graphMapper.Sources[0];
                //if (gdo2.GetType().ToString() == "pOd_Animation.L_TimeLine.pOd_TimeLineSlider")
                //{
                //    GH_NumberSlider ns = (GH_NumberSlider)gdo2;
                //    container.X0 = (double)ns.Slider.Minimum;
                //    container.X1 = (double)ns.Slider.Maximum;
                //}
                if (mapperSource.GetType().ToString() == "Motion.Param_RemoteReceiver")
                {
                    Param_GenericObject receiverObject = (Param_GenericObject)mapperSource;
                    string[] splitedReceiverNickname = mapperSource.NickName.Split('-');
                    container.X0 = Convert.ToDouble(splitedReceiverNickname[0]);
                    container.X1 = Convert.ToDouble(splitedReceiverNickname[1]);
                }
                oData = iData;

                //this.OnPingDocument().ScheduleSolution(100000, delegate { RefreshGraphMapper(gm); });
            }
            DA.SetData(0, oData);
            
        }

        //public void RefreshGraphMapper(GH_GraphMapper mapper)
        //{
        //    mapper.ExpireSolution(false);
        //}

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GraphMapperChanger;

        public override Guid ComponentGuid => new Guid("99B193E1-4328-467B-AC81-3D3C40FAC5CC");
    }
}