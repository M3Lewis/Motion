using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;

namespace Motion.Components.OBSOLETE
{
    public class GraphMapperChanger : GH_Component
    {
        private readonly Guid _graphMapperGuid = new Guid("bc984576-7aa6-491f-a91d-e444c33675a7");
        private Interval _lastDomain;
        private IGH_DocumentObject _lastMapper;

        protected override Bitmap Icon => Properties.Resources.GraphMapperChanger;

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("99B193E1-4328-467B-AC81-3D3C40FAC5CC");

        public GraphMapperChanger()
          : base("GraphMapper Changer", "Graph Mapper Changer",
            "将GraphMapper的X轴域设为前置pOd_Timeline Slider的区间，Y轴域为输入值",
            "Motion", "01_Animation")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("GraphMapper Data", "G", "GraphMapper数据", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Y Domain", "D", "Y轴目标区间", GH_ParamAccess.item, new Interval(0, 1));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("outValue", "V", "输出值", GH_ParamAccess.item);
        }

        double oData = 0d;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;
            Interval iDomain = new Interval(0, 1);


            // 先获取区间数据
            if (!DA.GetData(1, ref iDomain)) return;

            // 获取Graph Mapper
            var sourceParam = Params.Input[0].Sources.FirstOrDefault();
            if (sourceParam == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入端口未连接");
                return;
            }

            var mapperObject = sourceParam.Attributes.GetTopLevel.DocObject;
            if (mapperObject is GH_GraphMapper graphMapper)
            {
                // 检查区间是否发生变化
                bool domainChanged = !_lastDomain.EpsilonEquals(iDomain, 1e-6);
                bool mapperChanged = _lastMapper != graphMapper;

                if (domainChanged || mapperChanged)
                {
                    // 使用Document级别的延迟更新来处理Y轴区间变化
                    OnPingDocument().ScheduleSolution(10, doc =>
                    {
                        try
                        {
                            var container = graphMapper.Container;
                            container.Y0 = iDomain.T0;
                            container.Y1 = iDomain.T1;
                            _lastDomain = iDomain;
                            _lastMapper = graphMapper;
                            graphMapper.ExpireSolution(true);
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"更新Graph Mapper失败: {ex.Message}");
                        }
                    });
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

                // 在所有更新之后获取输入数据
                if (DA.GetData(0, ref iData))
                {
                    oData = iData;
                    // 更新Message，保留2位小数
                    Message = $"{NickName} | {oData:F2}";
                }
            }

            DA.SetData(0, oData);
        }

        public override void AddedToDocument(GH_Document document)
        {
            // 检查第一个输入端是否已经有连接
            bool isNewComponent = Params.Input[0].SourceCount == 0;

            // 只有新添加的组件（没有连接）才创建Graph Mapper
            if (isNewComponent)
            {
                document.ScheduleSolution(5, doc =>
                {
                    // 创建并添加Graph Mapper
                    var graphMapper = Instances.ComponentServer.EmitObject(_graphMapperGuid) as GH_GraphMapper;
                    if (graphMapper == null) return;

                    _lastMapper = graphMapper;

                    // 设置Graph Mapper的位置
                    _lastMapper.CreateAttributes();
                    _lastMapper.Attributes.Pivot = new PointF(
                        Attributes.Pivot.X - 300f,
                        Attributes.Pivot.Y - 85f
                    );

                    // 添加到文档
                    doc.AddObject(_lastMapper, false);

                    // 连接Graph Mapper的输出到当前组件的第一个输入
                    Params.Input[0].AddSource(graphMapper);

                    // 查找附近的RemoteReceiver并连接
                    var nearbyReceivers = doc.Objects
                        .OfType<IGH_Param>()
                        .Where(p => p.GetType().Name == "Param_RemoteReceiver" &&
                                   Math.Abs(p.Attributes.Pivot.Y - Attributes.Pivot.Y) < 50 &&
                                   Math.Abs(p.Attributes.Pivot.X - Attributes.Pivot.X) < 600)
                        .OrderBy(p => Math.Abs(p.Attributes.Pivot.X - Attributes.Pivot.X))
                        .FirstOrDefault();

                    // 如果找到Receiver，连接到Graph Mapper的输入
                    if (nearbyReceivers != null && graphMapper != null)
                    {
                        graphMapper.AddSource(nearbyReceivers);
                    }

                    // 设置为BezierGraph
                    var bezierGraph = Instances.ComponentServer.EmitGraph(new GH_BezierGraph().GraphTypeID);
                    if (bezierGraph != null && graphMapper != null)
                    {
                        bezierGraph.PrepareForUse();
                        var container = graphMapper.Container;
                        graphMapper.Container = null;  // 先清空当前Container

                        if (container == null)
                        {
                            container = new GH_GraphContainer(bezierGraph);
                        }
                        else
                        {
                            container.Graph = bezierGraph;
                        }
                        graphMapper.Container = container;  // 重新设置Container
                    }

                    // 刷新解决方案
                    doc.ScheduleSolution(10, d =>
                    {
                        if (graphMapper != null)
                        {
                            graphMapper.ExpireSolution(true);
                        }
                        ExpireSolution(true);
                    });
                });
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            _lastMapper = null;
            _lastDomain = new Interval(0, 1);
            base.RemovedFromDocument(document);
        }

        public override string NickName
        {
            get => base.NickName;
            set
            {
                base.NickName = value;
                Message = $"{value} | {oData:F2}";
                ExpireSolution(true);
            }
        }
    }
}