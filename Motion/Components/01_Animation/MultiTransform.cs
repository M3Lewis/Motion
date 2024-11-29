using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Types.Transforms;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Motion.Animation
{
    public class MultiTransform : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{a9f004f8-5bcb-4b12-9f42-4e96771b9290}");

        protected override Bitmap Icon => Properties.Resources.MultiTransform;

        public override bool IsPreviewCapable => false;

        public MultiTransform()
            : base("Multi Transform", "Multi Transform", "Apply multiple transforms to geometry in sequence", "Transform", "Util")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Base geometry", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transforms", "T", "Transformations to compound and apply", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Transformed geometry", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 获取输入几何体
            IGH_GeometricGoo geometry = null;
            if (!DA.GetData(0, ref geometry)) return;

            // 获取变换列表
            List<GH_Transform> transforms = new List<GH_Transform>();
            if (!DA.GetDataList(1, transforms)) return;

            // 创建复合变换
            GH_Transform compoundTransform = new GH_Transform();
            foreach (GH_Transform transform in transforms)
            {
                foreach (ITransform xform in transform.CompoundTransforms)
                {
                    compoundTransform.CompoundTransforms.Add(xform.Duplicate());
                }
            }
            compoundTransform.ClearCaches();

            // 应用变换到几何体
            IGH_GeometricGoo result = geometry.DuplicateGeometry();
            result = result.Transform(compoundTransform.Value);
            
            // 输出结果
            DA.SetData(0, result);
        }
    }
}