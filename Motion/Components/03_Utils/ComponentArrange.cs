

// Hare.ComponentArrange
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Motion.Utils
{
    public class ComponentArrange : GH_Component
    {
        private GH_Document ghDocument = null;

        protected override Bitmap Icon => Properties.Resources.ArrangeTabComponents;

        public override Guid ComponentGuid => new Guid("7574476e-994f-4553-be02-ac262e3d5386");

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public ComponentArrange()
            : base("Arrange Tab Components", "A", "列出指定标签的所有电池", "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Category", "Ca", "指定标签", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "R", "运行", GH_ParamAccess.item);
            // 移除了SubCategory输入参数
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string iCategory = "";
            bool iRun = false;

            DA.GetData(0, ref iCategory);
            if (iCategory == "") return;

            DA.GetData(1, ref iRun);
            if (!iRun) return;

            ghDocument = OnPingDocument();
            Dictionary<string, List<IGH_DocumentObject>> subCategoryGroups = new Dictionary<string, List<IGH_DocumentObject>>();
            PointF pivot = new PointF(200f, 200f);
            int totalCount = 0;

            // 收集并按subcategory分组
            foreach (IGH_ObjectProxy item in Instances.ComponentServer.ObjectProxies)
            {
                if (item?.Type == null) continue;

                try
                {
                    IGH_DocumentObject comp = item.CreateInstance();
                    if (comp == null || comp == this ||
                        comp.Exposure == GH_Exposure.hidden ||
                        comp.Name == "Window Attribute") continue;

                    if (comp.Category == iCategory)
                    {
                        string subCat = comp.SubCategory;
                        if (!subCategoryGroups.ContainsKey(subCat))
                        {
                            subCategoryGroups[subCat] = new List<IGH_DocumentObject>();
                        }
                        subCategoryGroups[subCat].Add(comp);
                        totalCount++;
                    }
                }
                catch { }
            }

            // 为每个subcategory创建组并布局组件
            foreach (var group in subCategoryGroups)
            {
                var components = group.Value;
                if (components.Count == 0) continue;

                // 创建group并设置颜色
                GH_Group ghGroup = new GH_Group();
                ghGroup.NickName = $"{iCategory}-{group.Key}";
                // 设置淡蓝色 (R:173, G:216, B:230, A:80)
                ghGroup.Colour = System.Drawing.Color.FromArgb(80, 173, 216, 230);
                ghDocument.AddObject(ghGroup, false);

                // 布局该组的组件
                foreach (var comp in components)
                {
                    float height = (comp.Attributes == null) ? 100f : comp.Attributes.Bounds.Height;
                    pivot.Y += height / 2f;

                    ghDocument.AddObject(comp, false, ghDocument.ObjectCount + 1);
                    comp.Attributes.Pivot = pivot;
                    pivot.Y += height / 2f + 80f;

                    // 将组件添加到group中
                    ghGroup.AddObject(comp.InstanceGuid);

                    if (components.IndexOf(comp) % 10 == 9)
                    {
                        pivot.X += 300f;
                        pivot.Y = 200f;
                    }
                }

                // 移动到下一组的起始位置
                pivot.X += 500f;
                pivot.Y = 200f;
            }
        }
    }
}