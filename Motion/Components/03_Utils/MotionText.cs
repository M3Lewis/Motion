﻿using Grasshopper.Kernel;
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
            Font.FontWeight fontWeight = Font.FontWeight.Normal; // Default
            if (fontWeightInt >= (int)Font.FontWeight.Thin && fontWeightInt <= (int)Font.FontWeight.Heavy) // Check range based on known enum values
            {
                 // Cast to byte first, as the underlying type is byte
                 byte fontWeightByte = (byte)fontWeightInt;
                 if (Enum.IsDefined(typeof(Font.FontWeight), fontWeightByte))
                 {
                     fontWeight = (Font.FontWeight)fontWeightByte;
                 }
            }

            Font.FontStyle fontStyle = Font.FontStyle.Upright; // Default
            if (fontStyleInt >= (int)Font.FontStyle.Upright && fontStyleInt <= (int)Font.FontStyle.Italic) // Check range based on known enum values
            {
                 // Cast to byte first, as the underlying type is byte
                 byte fontStyleByte = (byte)fontStyleInt;
                 if (Enum.IsDefined(typeof(Font.FontStyle), fontStyleByte))
                 {
                     fontStyle = (Font.FontStyle)fontStyleByte;
                 }
            }


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
            TextJustification justification = TextJustification.None;
            switch (hAlign) // Based on named values in RegisterInputParams
            {
                case 0: justification |= TextJustification.Left; break;
                case 1: justification |= TextJustification.Center; break;
                case 2: justification |= TextJustification.Right; break;
                default: justification |= TextJustification.Center; break; // Default fallback
            }
            switch (vAlign) // Based on named values, map to closest TextJustification equivalent
            {
                case 0: justification |= TextJustification.Top; break;    // Top
                case 1: justification |= TextJustification.Middle; break; // MiddleOfTop -> Middle
                case 2: justification |= TextJustification.Bottom; break; // BottomOfTop -> Bottom
                case 3: justification |= TextJustification.Middle; break; // Middle
                case 4: justification |= TextJustification.Middle; break; // MiddleOfBottom -> Middle
                case 5: justification |= TextJustification.Bottom; break; // Bottom
                case 6: justification |= TextJustification.Bottom; break; // BottomOfBoundingBox -> Bottom
                default: justification |= TextJustification.Middle; break; // Default fallback
            }
            textEntity.Justification = justification;


            // 创建 DimensionStyle (用于生成几何体)
            // DimensionStyle uses more specific alignment enums which might match inputs better
            TextHorizontalAlignment dimHAlign = TextHorizontalAlignment.Center; // Default
            // Check if hAlign is within the valid range for the enum (assuming values 0, 1, 2 based on RegisterInputParams)
            if (hAlign >= 0 && hAlign <= 2)
            {
                byte hAlignByte = (byte)hAlign;
                if (Enum.IsDefined(typeof(TextHorizontalAlignment), hAlignByte))
                {
                    dimHAlign = (TextHorizontalAlignment)hAlignByte;
                }
            }

            TextVerticalAlignment dimVAlign = TextVerticalAlignment.Middle; // Default
            // Check if vAlign is within the valid range for the enum (assuming values 0 to 6 based on RegisterInputParams)
             if (vAlign >= 0 && vAlign <= 6)
            {
                 byte vAlignByte = (byte)vAlign;
                 if (Enum.IsDefined(typeof(TextVerticalAlignment), vAlignByte))
                 {
                     dimVAlign = (TextVerticalAlignment)vAlignByte;
                 }
            }

            DimensionStyle style = new DimensionStyle
            {
                TextHeight = height,
                Font = rhinoFont,
                TextHorizontalAlignment = dimHAlign,
                TextVerticalAlignment = dimVAlign,
                // Ensure other relevant DimStyle properties are set if needed
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
            if (textBreps != null)
            {
                for (int i = 0; i < textBreps.Length; i++)
                {
                    var brep = textBreps[i];
                    if (brep == null) continue; // Null check

                    var path = new GH_Path(i);

                    // --- 处理边缘曲线 (从 Brep 提取以匹配 Mesh 路径) ---
                    var edges = brep.Edges;
                    if (edges != null)
                    {
                        var edgeCurves = new List<Curve>();
                        foreach (var edge in edges)
                        {
                            if (edge != null)
                            {
                                // Optimization: Try using EdgeCurve directly if it's suitable
                                // Curve edgeCurve = edge.EdgeCurve;
                                // if(edgeCurve != null) edgeCurves.Add(edgeCurve.DuplicateCurve()); // Duplicate if needed
                                // else ... fallback to ToNurbsCurve
                                var curve = edge.ToNurbsCurve(); // Can be costly
                                if (curve != null)
                                {
                                    edgeCurves.Add(curve);
                                }
                            }
                        }

                        if (edgeCurves.Count > 0)
                        {
                            // Curve.JoinCurves can be costly too
                            var joinedCurves = Curve.JoinCurves(edgeCurves, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 2.1); // Use document tolerance
                            if (joinedCurves != null)
                            {
                                foreach (var joinedCurve in joinedCurves)
                                {
                                    if (joinedCurve != null)
                                        curveTree.Append(new GH_Curve(joinedCurve), path);
                                }
                            }
                            else // If join fails, add individual curves
                            {
                                foreach (var edgeCurve in edgeCurves)
                                {
                                     curveTree.Append(new GH_Curve(edgeCurve), path);
                                }
                            }
                        }
                    }
                    // --- 边缘曲线处理结束 ---


                    // --- 处理网格 (主要性能瓶颈之一) ---
                    // Consider allowing user control over MeshingParameters
                    var mp = MeshingParameters.FastRenderMesh;
                    // Example: var mp = MeshingParameters.QualityRenderMesh;
                    // Example: var mp = new MeshingParameters(0.01); // Tolerance based

                    var meshParts = Mesh.CreateFromBrep(brep, mp);
                    if (meshParts != null && meshParts.Length > 0)
                    {
                        // Combine mesh parts if necessary (usually CreateFromBrep returns one mesh for simple Breps)
                        Mesh finalMesh = meshParts[0];
                        if (meshParts.Length > 1)
                        {
                            finalMesh = new Mesh();
                            foreach (var meshPart in meshParts)
                            {
                                if (meshPart != null)
                                    finalMesh.Append(meshPart);
                            }
                            finalMesh.Compact(); // Clean up unused components
                        }

                        if (finalMesh != null && finalMesh.Faces.Count > 0)
                        {
                            // 检查颜色输入参数（索引 8）是否有连接源
                            if (Params.Input[8].SourceCount > 0)
                            {
                                // 设置颜色 (仅当提供了颜色输入时)
                                Color meshColor = Color.White; // 如果列表为空但有连接，则为默认值
                                if (colors.Count > 0)
                                {
                                    // 使用模数进行更清晰的颜色循环
                                    meshColor = colors[i % colors.Count];
                                }
                                finalMesh.VertexColors.CreateMonotoneMesh(meshColor);
                            }
                            // Else: 不应用顶点颜色，保留网格默认颜色

                            meshTree.Append(new GH_Mesh(finalMesh), path);
                        }
                    }
                    // --- 网格处理结束 ---
                }
            }


            // --- 计算带偏移的边界矩形 (优化并内联) ---
            Rectangle3d boundingRect = Rectangle3d.Unset;
            if (curves != null && curves.Length > 0) // Use curves from CreateCurves
            {
                // Use the input plane for bounding box calculation initially
                Plane textPlaneForBounds = plane;

                BoundingBox combinedBox = BoundingBox.Empty;
                bool firstBox = true;

                foreach (Curve curve in curves)
                {
                    if (curve != null)
                    {
                        // Get BoundingBox relative to the text plane
                        BoundingBox curveBox = curve.GetBoundingBox(textPlaneForBounds);
                        if (curveBox.IsValid)
                        {
                            if (firstBox)
                            {
                                combinedBox = curveBox;
                                firstBox = false;
                            }
                            else
                            {
                                combinedBox.Union(curveBox);
                            }
                        }
                    }
                }

                if (combinedBox.IsValid)
                {
                    // Get intervals in the plane's coordinate system
                    Point3d[] corners = combinedBox.GetCorners();
                    Interval boundX = Interval.Unset;
                    Interval boundY = Interval.Unset;
                    bool firstPoint = true;

                    foreach (Point3d corner in corners)
                    {
                        if (textPlaneForBounds.ClosestParameter(corner, out double u, out double v))
                        {
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
                    }


                    if (boundX.IsValid && boundY.IsValid)
                    {
                        // Apply offset to the intervals
                        boundX = new Interval(boundX.T0 - offsetDistance, boundX.T1 + offsetDistance);
                        boundY = new Interval(boundY.T0 - offsetDistance, boundY.T1 + offsetDistance);

                        // Create the final rectangle on the text plane
                        boundingRect = new Rectangle3d(textPlaneForBounds, boundX, boundY);
                    }
                }
            }
            // --- 边界矩形计算结束 ---


            // 输出结果
            DA.SetDataTree(0, meshTree);
            DA.SetDataTree(1, curveTree); // Curve tree still populated from Brep edges for path consistency
            DA.SetData(2, new GH_Rectangle(boundingRect)); // Wrap in GH_Rectangle
        }

        // Removed GetTextJustification method
        // Removed GetOffsetTextRectangle method

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MotionText;

        public override Guid ComponentGuid => new Guid("94c7f247-9585-4b28-9e10-e3ce9e8308ff");
    }
}