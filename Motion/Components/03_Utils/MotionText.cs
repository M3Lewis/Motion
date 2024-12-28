using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Font = Rhino.DocObjects.Font;

namespace Motion.Utils
{
    public class MotionText : GH_Component, IGH_PreviewObject
    {
        public MotionText()
          : base("Motion Text", "Motion Text",
              "设置文字的各种属性并输出文字的Mesh和边缘线",
              "Motion", "03_Utils")
        {
        }

        protected override void BeforeSolveInstance()
        {
            if (Params.Output.Count > 1)
            {
                ((IGH_PreviewObject)Params.Output[1]).Hidden = false;
                ((IGH_PreviewObject)Params.Output[1]).Hidden = true;
            }
            base.BeforeSolveInstance();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "文字内容", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "文字的基准平面", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Height", "H", "文字高度", GH_ParamAccess.item, 10.0);
            pManager.AddTextParameter("Font", "F", "字体", GH_ParamAccess.item,"Microsoft YaHei");
            pManager.AddIntegerParameter("FontWeight", "FW", "字体粗细", GH_ParamAccess.item, 4);
            pManager.AddIntegerParameter("FontStyle", "FS", "字体样式", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("HAlignment", "HA", "水平对齐", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("VAlignment", "VA", "垂直对齐", GH_ParamAccess.item, 3);
            pManager.AddColourParameter("Color", "C", "文字颜色（可输入多种颜色）", GH_ParamAccess.list, Color.White);
            pManager.AddNumberParameter("Spacing", "SP", "字符间距", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Offset", "O", "边界矩形的偏移距离", GH_ParamAccess.item, 0.0);

            // 设置可选参数
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;

            if (pManager[4] is Param_Integer weightInteger)
            {
                weightInteger.AddNamedValue("Thin", 1);
                weightInteger.AddNamedValue("UltraLight", 2);
                weightInteger.AddNamedValue("Light", 3);
                weightInteger.AddNamedValue("Normal", 4);
                weightInteger.AddNamedValue("Medium", 5);
                weightInteger.AddNamedValue("SemiBold", 6);
                weightInteger.AddNamedValue("Bold", 7);
                weightInteger.AddNamedValue("UltraBold", 8);
                weightInteger.AddNamedValue("Heavy", 9);
            }

            if (pManager[5] is Param_Integer styleInteger)
            {
                styleInteger.AddNamedValue("Normal", 1);
                styleInteger.AddNamedValue("Italic", 2);
                styleInteger.AddNamedValue("Oblique", 3);
            }

            if (pManager[6] is Param_Integer hAlignInteger)
            {
                hAlignInteger.AddNamedValue("Left", 0);
                hAlignInteger.AddNamedValue("Center", 1);
                hAlignInteger.AddNamedValue("Right", 2);
            }

            if (pManager[7] is Param_Integer vAlignInteger)
            {
                vAlignInteger.AddNamedValue("Top", 0);
                vAlignInteger.AddNamedValue("MiddleOfTop", 1);
                vAlignInteger.AddNamedValue("BottomOfTop", 2);
                vAlignInteger.AddNamedValue("Middle", 3);
                vAlignInteger.AddNamedValue("MiddleOfBottom", 4);
                vAlignInteger.AddNamedValue("Bottom", 5);
                vAlignInteger.AddNamedValue("BottomOfBoundingBox", 6);
            }
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("TextMesh", "M", "文字Mesh", GH_ParamAccess.tree);
            pManager.AddCurveParameter("TextCurves", "C", "文字的边缘线", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("BoundingBox", "B", "带偏移的边界矩形", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 声明变量
            string text = "";
            Plane plane = Plane.WorldXY;
            double height = 10.0;
            int fontWeight = 4;
            int fontStyle = 1;
            string fontName = "";
            int hAlign = 0;
            int vAlign = 0;
            List<Color> color = new List<Color>();
            double spacing = 0.0;
            double offsetDistance = 0.0;

            // 获取输入数据
            if (!DA.GetData(0, ref text)) return;
            if (!DA.GetData(1, ref plane)) return;
            DA.GetData(2, ref height);
            DA.GetData(3, ref fontName);
            DA.GetData(4, ref fontWeight);
            DA.GetData(5, ref fontStyle);
            DA.GetData(6, ref hAlign);
            DA.GetData(7, ref vAlign);
            DA.GetDataList(8, color);
            DA.GetData(9, ref spacing);
            DA.GetData(10, ref offsetDistance);

            // 创建字体
            Font rhinoFont = new Font(fontName, (Font.FontWeight)fontWeight, (Font.FontStyle)fontStyle, false, false);

            // 创建 TextEntity
            var textEntity = new TextEntity
            {
                PlainText = text,
                TextHeight = height,
                Font = rhinoFont,
                Justification = GetTextJustification(hAlign, vAlign),
                Plane = plane
            };

            // 创建文字曲面和曲线
            const double scale = 1.0;
            DimensionStyle style = new DimensionStyle
            {
                TextHeight = height,
                Font = rhinoFont,
                TextHorizontalAlignment = (TextHorizontalAlignment)hAlign,
                TextVerticalAlignment = (TextVerticalAlignment)vAlign
            };

            // 获取边缘线（使用 TextEntity）
            var curves = textEntity.CreateCurves(style, false, scale, spacing);

            // 获取曲面（使用 TextEntity）
            Brep[] textBreps = textEntity.CreateSurfaces(style, scale, spacing);

            // 创建数据树来存储网格和边缘曲线
            var meshTree = new GH_Structure<GH_Mesh>();
            var curveTree = new GH_Structure<GH_Curve>();

            // 遍历每个 Brep
            for (int i = 0; i < textBreps.Length; i++)
            {
                var brep = textBreps[i];
                var path = new GH_Path(i);

                // 处理边缘曲线
                var edges = brep.Edges;
                var edgeCurves = new List<Curve>();
                
                // 收集所有边缘曲线
                foreach (var edge in edges)
                {
                    if (edge != null)
                    {
                        var curve = edge.ToNurbsCurve();
                        if (curve != null)
                        {
                            edgeCurves.Add(curve);
                        }
                    }
                }

                // Join 曲线并添加到树形结构
                if (edgeCurves.Count > 0)
                {
                    var joinedCurves = Curve.JoinCurves(edgeCurves);
                    foreach (var curve in joinedCurves)
                    {
                        curveTree.Append(new GH_Curve(curve), path);
                    }
                }

                // 处理网格
                var meshParts = Mesh.CreateFromBrep(textBreps[i], MeshingParameters.FastRenderMesh);
                if (meshParts != null && meshParts.Length > 0)
                {
                    // 根据颜色数量决定使用哪个颜色
                    Color meshColor;
                    if (color.Count == 0)
                    {
                        meshColor = Color.White;
                    }
                    else if (color.Count == 1)
                    {
                        meshColor = color[0];
                    }
                    else
                    {
                        // 计算当前字符应该使用哪个颜色
                        double charsPerColor = (double)textBreps.Length / color.Count;
                        int colorIndex = Math.Min((int)(i / charsPerColor), color.Count - 1);
                        meshColor = color[colorIndex];
                    }

                    // 设置颜色并添加到树形结构
                    meshParts[0].VertexColors.CreateMonotoneMesh(meshColor);
                    meshTree.Append(new GH_Mesh(meshParts[0]), path);
                }
            }

            // 获取带偏移的边界矩形
            Rectangle3d boundingRect = GetOffsetTextRectangle(curves,textEntity, offsetDistance);

            // 输出结果
            DA.SetDataTree(0, meshTree);
            DA.SetDataTree(1, curveTree);
            DA.SetData(2, boundingRect);
        }

        private TextJustification GetTextJustification(int hAlign, int vAlign)
        {
            TextJustification justification = TextJustification.None;

            // 处理水平对齐
            switch (hAlign)
            {
                case 1: justification |= TextJustification.Left; break;
                case 2: justification |= TextJustification.Center; break;
                case 4: justification |= TextJustification.Right; break;
            }

            // 处理垂直对齐
            justification |= (TextJustification)vAlign;

            return justification;
        }

        private Rectangle3d GetOffsetTextRectangle(Curve[] curves, TextEntity textEntity, double offsetDistance)
        {
            if (curves == null || curves.Length == 0) return Rectangle3d.Unset;

            // 使用文字的平面来获取边界框，这样可以保证边界框与文字方向一致
            Plane textPlane = textEntity.Plane;

            // 从第一条曲线开始初始化边界区间
            Box firstBoundingBox = new Box(curves[0].GetBoundingBox(textPlane));
            Interval boundX = firstBoundingBox.X;  // X方向的区间
            Interval boundY = firstBoundingBox.Y;  // Y方向的区间

            // 如果有多条曲线，扩展边界区间以包含所有曲线
            if (curves.Length > 1)
            {
                for (int i = 1; i < curves.Length; i++)
                {
                    Box currentBoundingBox = new Box(curves[i].GetBoundingBox(textPlane));
                    
                    // 扩展X方向的区间
                    boundX.T0 = Math.Min(boundX.T0, currentBoundingBox.X.T0);  // 更新最小X
                    boundX.T1 = Math.Max(boundX.T1, currentBoundingBox.X.T1);  // 更新最大X
                    
                    // 扩展Y方向的区间
                    boundY.T0 = Math.Min(boundY.T0, currentBoundingBox.Y.T0);  // 更新最小Y
                    boundY.T1 = Math.Max(boundY.T1, currentBoundingBox.Y.T1);  // 更新最大Y
                }
            }

            // 向四周扩展边界，添加偏移距离
            boundX.T0 -= offsetDistance;  // 左边界向外扩展
            boundX.T1 += offsetDistance;  // 右边界向外扩展
            boundY.T0 -= offsetDistance;  // 下边界向外扩展
            boundY.T1 += offsetDistance;  // 上边界向外扩展

            // 使用文字的平面和扩展后的边界区间创建矩形
            return new Rectangle3d(textPlane, boundX, boundY);
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MotionText;

        public override Guid ComponentGuid => new Guid("94c7f247-9585-4b28-9e10-e3ce9e8308ff");
    }
}