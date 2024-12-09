using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Motion.Utils
{
    public class ImageTransformSettings : GH_Component
    {
        public ImageTransformSettings()
            : base("Image Transform Settings", "ImgTransform",
                "设置图片变换参数",
                "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTransformParameter("Diffuse Transform", "DT", "漫反射贴图变换", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transparency Transform", "TT", "透明贴图变换", GH_ParamAccess.item);
            pManager.AddTransformParameter("Bump Transform", "BT", "凹凸贴图变换", GH_ParamAccess.item);
            
            // 所有输入都是可选的,并设置默认值
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;

            // 为每个输入设置默认的单位变换
            Transform defaultTransform = Transform.Scale(Plane.WorldXY, 1, 1, 1);
            var defaultGHTransform = new GH_Transform(defaultTransform);
            
            // 需要将参数转换为 Param_Transform 类型才能访问 PersistentData
            ((Param_Transform)pManager[0]).PersistentData.Append(defaultGHTransform);
            ((Param_Transform)pManager[1]).PersistentData.Append(defaultGHTransform);
            ((Param_Transform)pManager[2]).PersistentData.Append(defaultGHTransform);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Settings", "TS", "贴图变换设置列表", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 创建默认变换(单位变换)
            Transform transparencyTransform = Transform.Identity;
            Transform diffuseTransform = Transform.Identity;
            Transform bumpTransform = Transform.Identity;

            // 获取输入的变换
            DA.GetData("Diffuse Transform", ref diffuseTransform);
            DA.GetData("Transparency Transform", ref transparencyTransform);
            DA.GetData("Bump Transform", ref bumpTransform);

            // 创建变换列表
            List<Transform> transformSettings = new List<Transform>
            {
                transparencyTransform,
                diffuseTransform,
                bumpTransform
            };

            DA.SetDataList(0, transformSettings);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ImageTransformSettings;
        public override Guid ComponentGuid => new Guid("30B96D80-4B9D-4B95-B529-9A60053AE5EC");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
    }
} 