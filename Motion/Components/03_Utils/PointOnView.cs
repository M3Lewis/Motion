using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Motion.Utils
{
    public class PointOnView : GH_Component
    {
        public PointOnView()
          : base("Point On View", "Point On View",
              "根据指定视口和目标分辨率转换坐标",
              "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Viewport Name", "VP", "视口名称", GH_ParamAccess.item);
            pManager.AddNumberParameter("Target Resolution", "R", "目标分辨率 (输入多行数据，1行为宽度，1行为宽度)", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "输入点", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Original Point", "OP", "原始坐标点", GH_ParamAccess.item);
            pManager.AddPointParameter("Scaled Point", "SP", "按分辨率缩放后的坐标点", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 声明变量
            string viewportName = "";
            List<double> resolution = new List<double>();
            Point3d inputPoint = Point3d.Unset;

            // 获取输入数据
            if (!DA.GetData(0, ref viewportName)) return;
            if (!DA.GetDataList(1, resolution)) return;
            if (!DA.GetData(2, ref inputPoint)) return;

            // 检查分辨率列表是否有效
            if (resolution.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "请输入完整的目标分辨率 [宽度,高度]");
                return;
            }

            // 获取指定视口
            var viewport = Rhino.RhinoDoc.ActiveDoc.Views
                .FirstOrDefault(v => v.ActiveViewport.Name.Contains(viewportName))?.ActiveViewport;

            if (viewport == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "找不到指定的视口");
                return;
            }

            // 获取视口尺寸
            double viewportWidth = viewport.Size.Width;
            double viewportHeight = viewport.Size.Height;

            // 计算缩放比例
            double scaleX = resolution[0] / viewportWidth;
            double scaleY = resolution[1] / viewportHeight;

            // 创建原始点
            Point3d originalPoint = new Point3d(inputPoint.X, inputPoint.Y, 0);

            // 创建缩放后的点
            Point3d scaledPoint = new Point3d(
                inputPoint.X * scaleX, 
                inputPoint.Y * scaleY, 
                0);

            // 输出结果
            DA.SetData(0, originalPoint);
            DA.SetData(1, scaledPoint);
        }

        protected override System.Drawing.Bitmap Icon =>    Properties.Resources.PointOnView;

        public override Guid ComponentGuid => new Guid("A4B7C858-6C25-4550-A010-76CFA112E6BD");

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
    }
}