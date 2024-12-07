using Grasshopper.Kernel;
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
            
            // 所有输入都是可选的,默认值将在 SolveInstance 中设置
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
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

        protected override System.Drawing.Bitmap Icon => null;//Properties.Resources.ImageTransformSettings;
        public override Guid ComponentGuid => new Guid("30B96D80-4B9D-4B95-B529-9A60053AE5EC");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
    }
} 