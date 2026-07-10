using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
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
            // Hide preview for TextCurves output (index 1)
            if (Params.Output.Count > 1 && Params.Output[1] is IGH_PreviewObject previewCurves)
            {
                previewCurves.Hidden = true;
            }

            // Hide preview for BoundingBox output (index 2)
            if (Params.Output.Count > 2 && Params.Output[2] is IGH_PreviewObject previewBox)
            {
                previewBox.Hidden = true;
            }

            base.BeforeSolveInstance();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "文字内容", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "文字的基准平面", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Height", "H", "文字高度", GH_ParamAccess.item, 10.0);
            pManager.AddTextParameter("Font", "F", "字体", GH_ParamAccess.item, "Microsoft YaHei");
            pManager.AddIntegerParameter("FontWeight", "FW", "字体粗细", GH_ParamAccess.item, 4); // Default Normal
            pManager.AddIntegerParameter("FontStyle", "FS", "字体样式", GH_ParamAccess.item, 1); // Default Normal
            pManager.AddIntegerParameter("HAlignment", "HA", "水平对齐", GH_ParamAccess.item, 1); // Default Center
            pManager.AddIntegerParameter("VAlignment", "VA", "垂直对齐", GH_ParamAccess.item, 3); // Default Middle
            pManager.AddColourParameter("Color", "C", "文字颜色（可输入多种颜色）", GH_ParamAccess.list);
            pManager.AddNumberParameter("Spacing", "SP", "字符间距", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Offset", "O", "边界矩形的偏移距离", GH_ParamAccess.item, 0.0);

            // 设置可选参数
            pManager[1].Optional = true; // Plane
            pManager[2].Optional = true; // Height
            pManager[3].Optional = true; // Font
            pManager[4].Optional = true; // FontWeight
            pManager[5].Optional = true; // FontStyle
            pManager[6].Optional = true; // HAlignment
            pManager[7].Optional = true; // VAlignment
            pManager[8].Optional = true; // Color
            pManager[9].Optional = true; // Spacing
            pManager[10].Optional = true; // Offset

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
                vAlignInteger.AddNamedValue("MiddleOfTop", 1); // Note: Maps to Middle in TextEntity
                vAlignInteger.AddNamedValue("BottomOfTop", 2); // Note: Maps to Bottom in TextEntity
                vAlignInteger.AddNamedValue("Middle", 3);
                vAlignInteger.AddNamedValue("MiddleOfBottom", 4); // Note: Maps to Middle in TextEntity
                vAlignInteger.AddNamedValue("Bottom", 5);
                vAlignInteger.AddNamedValue("BottomOfBoundingBox", 6); // Note: Maps to Bottom in TextEntity
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
            int fontWeightInt = 4;
            int fontStyleInt = 1;
            string fontName = "Microsoft YaHei";
            int hAlign = 1; // Default Center
            int vAlign = 3; // Default Middle
            List<Color> colors = new List<Color>();
            double spacing = 0.0;
            double offsetDistance = 0.0;

            // 获取输入数据
            if (!DA.GetData(0, ref text)) return;
            DA.GetData(1, ref plane); // Optional, uses default if fails
            DA.GetData(2, ref height);
            DA.GetData(3, ref fontName);
            DA.GetData(4, ref fontWeightInt);
            DA.GetData(5, ref fontStyleInt);
            DA.GetData(6, ref hAlign);
            DA.GetData(7, ref vAlign);
            DA.GetDataList(8, colors);
            DA.GetData(9, ref spacing);
            DA.GetData(10, ref offsetDistance);

            // 验证字体粗细和样式值
            if (!TryParseFontWeight(fontWeightInt, out Font.FontWeight fontWeight)) return;
            if (!TryParseFontStyle(fontStyleInt, out Font.FontStyle fontStyle)) return;

            // 创建字体
            Font rhinoFont = new Font(fontName, fontWeight, fontStyle, false, false);

            // 创建 TextEntity
            var textEntity = new TextEntity
            {
                PlainText = text,
                TextHeight = height,
                Font = rhinoFont,
                Plane = plane
            };

            // 设置 Justification (映射输入整数到枚举)
            TextJustification justification = MapJustification(hAlign, vAlign);
            textEntity.Justification = justification;
            
            // 创建 DimensionStyle (用于生成几何体)
            // 验证对齐参数并创建 DimensionStyle
            if (!TryParseHorizontalAlignment(hAlign, out TextHorizontalAlignment dimHAlign)) return;
            if (!TryParseVerticalAlignment(vAlign, out TextVerticalAlignment dimVAlign)) return;

            var style = new DimensionStyle
            {
                TextHeight = height,
                Font = rhinoFont,
                TextHorizontalAlignment = dimHAlign,
                TextVerticalAlignment = dimVAlign,
            };

            // 获取边缘线（使用 TextEntity.CreateCurves）
            // This is generally faster than creating surfaces and extracting edges.
            Curve[] curves = textEntity.CreateCurves(style, false, 1.0, spacing);

            // 获取曲面（使用 TextEntity.CreateSurfaces） - 主要性能瓶颈之一
            Brep[] textBreps = textEntity.CreateSurfaces(style, 1.0, spacing);

            // 创建数据树来存储网格和边缘曲线
            var meshTree = new GH_Structure<GH_Mesh>();
            var curveTree = new GH_Structure<GH_Curve>();

            // 填充曲线树 (使用 CreateCurves 的结果)
            // 将所有曲线放入与 Brep 对应的路径中（如果可能）或单个路径中。
            // 为了与 Mesh 路径一致，我们仍然从 Brep 提取边缘。
            // TODO: Explore if CreateCurves result can be reliably mapped to paths matching textBreps.

            // 遍历每个 Brep (主要用于 Mesh 生成和保持曲线路径一致性)
            if (textBreps == null) return;

            for (int i = 0; i < textBreps.Length; i++)
            {
                var brep = textBreps[i];
                if (brep == null) continue; // Null check

                var path = new GH_Path(i);

                // --- 处理边缘曲线 (从 Brep 提取以匹配 Mesh 路径) ---
                ProcessBrepEdgesToCurves(brep, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 2.1, path, curveTree);
                // --- 边缘曲线处理结束 ---
                
                // --- 处理网格 (主要性能瓶颈之一) ---
                // Consider allowing user control over MeshingParameters
                var mp = MeshingParameters.FastRenderMesh;
                // Example: var mp = MeshingParameters.QualityRenderMesh;
                // Example: var mp = new MeshingParameters(0.01); // Tolerance based

                ProcessMeshWithColor(brep, mp, colors, i, path, meshTree);
                // --- 网格处理结束 ---
            }
            
            // --- 计算带偏移的边界矩形 (优化并内联) ---
            Rectangle3d boundingRect = ComputeBoundingRectWithOffset(curves, plane, offsetDistance);
            // --- 边界矩形计算结束 ---
            
            // 输出结果
            DA.SetDataTree(0, meshTree);
            DA.SetDataTree(1, curveTree); // Curve tree still populated from Brep edges for path consistency
            DA.SetData(2, new GH_Rectangle(boundingRect)); // Wrap in GH_Rectangle
        }
        
        /// <summary>
        /// 验证并转换水平对齐参数（0=Left, 1=Center, 2=Right）。
        /// 无效时返回 false。
        /// </summary>
        private bool TryParseHorizontalAlignment(int hAlign, out TextHorizontalAlignment alignment)
        {
            alignment = TextHorizontalAlignment.Center;

            // 修正：用 || 检查范围
            if (hAlign < 0 || hAlign > 2) return false;

            byte b = (byte)hAlign;
            if (!Enum.IsDefined(typeof(TextHorizontalAlignment), b)) return false;

            alignment = (TextHorizontalAlignment)b;
            return true;
        }

        /// <summary>
        /// 验证并转换垂直对齐参数（0=Top, 1=MiddleOfTop, 2=BottomOfTop, 3=Middle, 4=MiddleOfBottom, 5=Bottom, 6=BottomOfBoundingBox）。
        /// 无效时返回 false。
        /// </summary>
        private bool TryParseVerticalAlignment(int vAlign, out TextVerticalAlignment alignment)
        {
            alignment = TextVerticalAlignment.Middle;

            // 修正：用 || 检查范围
            if (vAlign < 0 || vAlign > 6) return false;

            byte b = (byte)vAlign;
            if (!Enum.IsDefined(typeof(TextVerticalAlignment), b)) return false;

            alignment = (TextVerticalAlignment)b;
            return true;
        }
        
        /// <summary>
        /// 验证并转换字体粗细参数。无效时返回 false。
        /// </summary>
        private bool TryParseFontWeight(int fontWeightInt, out Font.FontWeight fontWeight)
        {
            fontWeight = Font.FontWeight.Normal;

            // 检查范围（基于已知枚举值 Thin~Heavy）
            if (fontWeightInt < (int)Font.FontWeight.Thin && fontWeightInt > (int)Font.FontWeight.Heavy)
                return false;

            byte fontWeightByte = (byte)fontWeightInt;
            if (!Enum.IsDefined(typeof(Font.FontWeight), fontWeightByte))
                return false;

            fontWeight = (Font.FontWeight)fontWeightByte;
            return true;
        }

        /// <summary>
        /// 验证并转换字体样式参数。无效时返回 false。
        /// </summary>
        private bool TryParseFontStyle(int fontStyleInt, out Font.FontStyle fontStyle)
        {
            fontStyle = Font.FontStyle.Upright;

            if (fontStyleInt < (int)Font.FontStyle.Upright && fontStyleInt > (int)Font.FontStyle.Italic)
                return false;

            byte fontStyleByte = (byte)fontStyleInt;
            if (!Enum.IsDefined(typeof(Font.FontStyle), fontStyleByte))
                return false;

            fontStyle = (Font.FontStyle)fontStyleByte;
            return true;
        }
        
        /// <summary>
        /// 将水平/垂直对齐整数映射为 TextJustification 枚举。
        /// </summary>
        private TextJustification MapJustification(int hAlign, int vAlign)
        {
            TextJustification justification = TextJustification.None;

            // 水平对齐
            justification |= hAlign switch
            {
                0 => TextJustification.Left,
                1 => TextJustification.Center,
                2 => TextJustification.Right,
                _ => TextJustification.Center // 默认
            };

            // 垂直对齐
            justification |= vAlign switch
            {
                0 => TextJustification.Top,
                1 => TextJustification.Middle,
                2 => TextJustification.Bottom,
                3 => TextJustification.Middle,
                4 => TextJustification.Middle,
                5 => TextJustification.Bottom,
                6 => TextJustification.Bottom,
                _ => TextJustification.Middle
            };

            return justification;
        }

        /// <summary>
        /// 提取 Brep 的边缘曲线，尝试合并后添加到曲线树。合并失败则添加单独曲线。
        /// </summary>
        private void ProcessBrepEdgesToCurves(Brep brep, double tolerance, GH_Path path,
            GH_Structure<GH_Curve> curveTree)
        {
            var edges = brep.Edges;
            if (edges == null) return;

            var edgeCurves = new List<Curve>();
            foreach (var edge in edges)
            {
                if (edge == null) continue;
                var curve = edge.ToNurbsCurve();
                if (curve == null) continue;
                edgeCurves.Add(curve);
            }

            if (edgeCurves.Count == 0) return;

            var joinedCurves = Curve.JoinCurves(edgeCurves, tolerance);
            if (joinedCurves != null)
            {
                foreach (var joinedCurve in joinedCurves)
                {
                    if (joinedCurve == null) continue;
                    curveTree.Append(new GH_Curve(joinedCurve), path);
                }
            }
            else
            {
                foreach (var edgeCurve in edgeCurves)
                {
                    curveTree.Append(new GH_Curve(edgeCurve), path);
                }
            }
        }
        
        /// <summary>
        /// 处理单个 Brep 的网格生成、合并与顶点颜色设置。
        /// </summary>
        private void ProcessMeshWithColor(Brep brep, MeshingParameters mp, List<Color> colors,
            int index, GH_Path path, GH_Structure<GH_Mesh> meshTree)
        {
            var meshParts = Mesh.CreateFromBrep(brep, mp);
            if (meshParts == null || meshParts.Length == 0) return;

            // 合并网格
            Mesh finalMesh = meshParts[0];
            if (meshParts.Length > 1)
            {
                finalMesh = new Mesh();
                foreach (var part in meshParts)
                {
                    if (part != null) finalMesh.Append(part);
                }
                finalMesh.Compact();
            }

            if (finalMesh == null || finalMesh.Faces.Count == 0) return;

            // 顶点颜色（如果有输入）
            if (Params.Input[8].SourceCount > 0 || Params.Input[8].VolatileDataCount > 0)
            {
                if (colors.Count > 0)
                {
                    Color meshColor = colors[index % colors.Count];
                    finalMesh.VertexColors.CreateMonotoneMesh(meshColor);
                }
            }

            meshTree.Append(new GH_Mesh(finalMesh), path);
        }
        
        /// <summary>
        /// 根据曲线集合、文本平面和偏移距离计算带偏移的边界矩形。
        /// 无法计算时返回 Rectangle3d.Unset。
        /// </summary>
        private Rectangle3d ComputeBoundingRectWithOffset(Curve[] curves, Plane plane, double offsetDistance)
        {
            if (curves == null || curves.Length == 0) return Rectangle3d.Unset;

            BoundingBox combinedBox = BoundingBox.Empty;
            bool hasBox = false;

            foreach (Curve curve in curves)
            {
                if (curve == null) continue;

                BoundingBox curveBox = curve.GetBoundingBox(plane);
                if (!curveBox.IsValid) continue;

                if (hasBox)
                    combinedBox.Union(curveBox);
                else
                {
                    combinedBox = curveBox;
                    hasBox = true;
                }
            }

            if (!hasBox || !combinedBox.IsValid) return Rectangle3d.Unset;

            // 获取角点并映射到平面 UV 区间
            Point3d[] corners = combinedBox.GetCorners();
            Interval boundX = Interval.Unset;
            Interval boundY = Interval.Unset;
            bool firstPoint = true;

            foreach (Point3d corner in corners)
            {
                if (!plane.ClosestParameter(corner, out double u, out double v)) continue;

                if (firstPoint)
                {
                    boundX = new Interval(u, u);
                    boundY = new Interval(v, v);
                    firstPoint = false;
                }
                else
                {
                    boundX.Grow(u);
                    boundY.Grow(v);
                }
            }

            if (!boundX.IsValid || !boundY.IsValid) return Rectangle3d.Unset;

            // 应用偏移
            boundX = new Interval(boundX.T0 - offsetDistance, boundX.T1 + offsetDistance);
            boundY = new Interval(boundY.T0 - offsetDistance, boundY.T1 + offsetDistance);

            return new Rectangle3d(plane, boundX, boundY);
        }
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MotionText;

        public override Guid ComponentGuid => new Guid("94c7f247-9585-4b28-9e10-e3ce9e8308ff");
    }
}