using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Linq;

namespace Motion.Animation
{
    public class GraphMapperChanger : GH_Component
    {

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GraphMapperChanger;
        public override Guid ComponentGuid => new Guid("99B193E1-4328-467B-AC81-3D3C40FAC5CC");

        private Interval _lastDomain = new Interval(0, 1);
        private GH_GraphMapper _lastMapper = null;

        public GraphMapperChanger()
          : base("GraphMapper Changer", "Graph Mapper Changer",
            "将GraphMapper的X轴域设为前置pOd_Timeline Slider的区间，Y轴域为输入值",
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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;
            Interval iDomain = new Interval(0, 1);
            double oData = 0d;

            if (!DA.GetData(0, ref iData)) return;
            if (!DA.GetData(1, ref iDomain)) return;

            // 获取Graph Mapper
            var mapperObject = this.Params.Input[0].Sources.FirstOrDefault()?.Attributes.GetTopLevel.DocObject;
            if (mapperObject is GH_GraphMapper graphMapper)
            {
                // 检查区间是否发生变化
                bool domainChanged = !_lastDomain.EpsilonEquals(iDomain, 1e-6);
                bool mapperChanged = _lastMapper != graphMapper;

                if (domainChanged || mapperChanged)
                {
                    UpdateGraphMapper(graphMapper, iDomain);
                    _lastDomain = iDomain;
                    _lastMapper = graphMapper;
                }

                // 设置Graph Mapper的显示样式
                graphMapper.WireDisplay = GH_ParamWireDisplay.faint;

                // 处理X轴区间
                if (graphMapper.Sources.FirstOrDefault() is Param_GenericObject receiverObject)
                {
                    string[] splitedReceiverNickname = receiverObject.NickName.Split('-');
                    if (splitedReceiverNickname.Length == 2 &&
                        double.TryParse(splitedReceiverNickname[0], out double x0) &&
                        double.TryParse(splitedReceiverNickname[1], out double x1))
                    {
                        var container = graphMapper.Container;
                        container.X0 = x0;
                        container.X1 = x1;
                    }
                }

                oData = iData;
            }

            DA.SetData(0, oData);
        }

        private void UpdateGraphMapper(GH_GraphMapper mapper, Interval domain)
        {
            var container = mapper.Container;
            container.Y0 = domain.T0;
            container.Y1 = domain.T1;

            // 清除运行时消息
            mapper.ClearRuntimeMessages();
            
            // 强制Graph Mapper更新
            mapper.ExpireSolution(true);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            // 初始化状态
            _lastDomain = new Interval(0, 1);
            _lastMapper = null;
        }
    }
}