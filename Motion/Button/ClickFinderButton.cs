using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class ClickFinderButton : MotionToolbarButton, IGH_PreviewObject
    {
        protected override int ToolbarOrder => 60;
        private ToolStripButton button;
        private bool isActive = false;
        private List<IGH_DocumentObject> previewObjects;
        private List<BoundingBox> boundingBoxes;
        private Mesh previewMesh;
        private DisplayMaterial previewMaterial;
        private Color previewColor;
        private Timer checkTimer;

        public ClickFinderButton()
        {
            checkTimer = new Timer();
            checkTimer.Interval = 100;
            checkTimer.Tick += CheckMouseClick;
        }

        private void AddClickFinderButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(button); // 使用基类方法添加按钮
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;

            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;

            //Check if DocumentEditor was instantiated
            if (editor == null) return;
            AddClickFinderButton();
        }

        private void Instantiate()
        {
            // 配置按钮
            button.Name = "Click Finder";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.ClickFinder;
            button.ToolTipText = "单击Rhino视口中显示的GH物件以查找组件";
            button.Click += ToggleClickFinderMode;
            button.CheckOnClick = true;
        }

        public void ToggleClickFinderMode(object sender, EventArgs e)
        {
            isActive = button.Checked;
            if (isActive)
            {
                CollectPreviewObjects();
                checkTimer.Start();
                previewMaterial = new DisplayMaterial(Color.FromArgb(128, 255, 255), 0.3);
                previewColor = Color.FromArgb(128, 255, 255);
                // 订阅 Rhino 显示管道事件
                RhinoDoc.ActiveDoc.Views.Redraw();
                foreach (var view in RhinoDoc.ActiveDoc.Views)
                {
                    DisplayPipeline.CalculateBoundingBox += DisplayPipeline_CalculateBoundingBox;
                    DisplayPipeline.DrawForeground += DisplayPipeline_DrawForeground;
                }
            }
            else
            {
                checkTimer.Stop();
                
                // 取消订阅显示管道事件
                foreach (var view in RhinoDoc.ActiveDoc.Views)
                {
                    DisplayPipeline.CalculateBoundingBox -= DisplayPipeline_CalculateBoundingBox;
                    DisplayPipeline.DrawForeground -= DisplayPipeline_DrawForeground;
                }
                
                previewMesh = null;
                previewMaterial = null;
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }

        private void CheckMouseClick(object sender, EventArgs e)
        {
            if (Control.MouseButtons == MouseButtons.Left)
            {
                if (TryGetClickRay(out Line clickRay))
                {
                    int index = FindNearestObject(clickRay);
                    if (index >= 0)
                    {
                        FocusOnComponent(previewObjects[index]);
                        button.Checked = false;
                        isActive = false;
                        checkTimer.Stop();
                        previewMesh = null;
                        previewMaterial = null;
                        
                        RhinoDoc.ActiveDoc?.Views.Redraw();
                    }
                }
            }
            else
            {
                int alpha = Convert.ToInt32(255 * DateTime.Now.Millisecond / 1000.0);
                previewMaterial = new DisplayMaterial(Color.FromArgb(alpha, 255, 255), 0.95);
                previewColor = Color.FromArgb(alpha, 128, 255,255);
                RhinoDoc.ActiveDoc?.Views.Redraw();
            }
        }

        private void CollectPreviewObjects()
        {
            previewObjects = new List<IGH_DocumentObject>();
            boundingBoxes = new List<BoundingBox>();
            previewMesh = new Mesh();
            
            foreach (IGH_DocumentObject obj in Instances.ActiveCanvas.Document.Objects)
            {
                if (obj is IGH_PreviewObject previewObj && !previewObj.Hidden)
                {
                    previewObjects.Add(obj);
                    
                    // 获取所有预览顶点
                    var vertices = new List<Point3d>();
                    
                    // 处理组件
                    if (obj is IGH_Component component)
                    {
                        // 遍历所有输出参数
                        foreach (var param in component.Params.Output)
                        {
                            foreach (var data in param.VolatileData.AllData(true))
                            {
                                AddGeometryVertices(data, vertices);
                            }
                        }
                    }
                    // 处理参数
                    else if (obj is IGH_Param param)
                    {
                        foreach (var data in param.VolatileData.AllData(true))
                        {
                            AddGeometryVertices(data, vertices);
                        }
                    }
                    
                    // 从顶点创建边界框
                    var box = vertices.Count > 0 
                        ? new BoundingBox(vertices) 
                        : previewObj.ClippingBox;  // 如无法获取顶点，回退到ClippingBox
                        
                    if (box.Volume < 0.1) box.Inflate(1);
                    boundingBoxes.Add(box);
                    
                    // 创建预览用的box mesh
                    Mesh boxMesh = Mesh.CreateFromBox(box, 1, 1, 1);
                    if (boxMesh != null && boxMesh.IsValid)
                    {
                        boxMesh.Compact();
                        previewMesh.Append(boxMesh);
                    }
                }
            }
            
            if (!previewMesh.IsValid)
            {
                previewMesh.Weld(Math.PI);
                previewMesh.Compact();
            }
        }

        private void AddGeometryVertices(IGH_Goo data, List<Point3d> vertices)
        {
            if (data is GH_Point pointData)
            {
                if (pointData.Value != Point3d.Unset)
                    vertices.Add(pointData.Value);
            }
            else if (data is GH_Curve curveData)
            {
                if (curveData.Value != null)
                    vertices.AddRange(curveData.Value.GetBoundingBox(false).GetCorners());
            }
            else if (data is GH_Surface surfaceData)
            {
                if (surfaceData.Value != null)
                    vertices.AddRange(surfaceData.Value.GetBoundingBox(false).GetCorners());
            }
            else if (data is GH_Mesh meshData)
            {
                if (meshData.Value != null)
                    vertices.AddRange(meshData.Value.Vertices.ToPoint3dArray());
            }
            else if (data is GH_Brep brepData)
            {
                if (brepData.Value != null)
                    vertices.AddRange(brepData.Value.GetBoundingBox(false).GetCorners());
            }
        }

        private bool TryGetClickRay(out Line clickRay)
        {
            clickRay = new Line();
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            var viewport = view.ActiveViewport;
            var screenPoint = Cursor.Position;
            var viewRect = view.ScreenRectangle;

            if (screenPoint.X > viewRect.Left && screenPoint.X < viewRect.Right &&
                screenPoint.Y > viewRect.Top && screenPoint.Y < viewRect.Bottom)
            {
                Line worldLine;
                viewport.GetFrustumLine(
                    screenPoint.X - viewRect.Left,
                    screenPoint.Y - viewRect.Top,
                    out worldLine);
                clickRay = new Line(worldLine.PointAt(1), -worldLine.UnitTangent);
                return true;
            }
            return false;
        }

        private int FindNearestObject(Line clickRay)
        {
            int nearestIndex = -1;
            double minDistance = double.MaxValue;
            var candidateIndices = new List<(int index, double distance)>();

            // 首先收集所有与射线相交的boundingbox
            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                if (Intersection.LineBox(clickRay, boundingBoxes[i], 0.01, out Interval collision) &&
                    collision.T0 > 0 && collision.T0 < double.MaxValue)
                {
                    candidateIndices.Add((i, collision.T0));
                }
            }

            // 如果有多个候选对象
            if (candidateIndices.Count > 1)
            {
                // 获取点击点在屏幕上的位置
                var screenPoint = Cursor.Position;
                var view = RhinoDoc.ActiveDoc.Views.ActiveView;
                var viewport = view.ActiveViewport;
                var viewRect = view.ScreenRectangle;

                minDistance = double.MaxValue;
                foreach (var candidate in candidateIndices)
                {
                    var box = boundingBoxes[candidate.index];
                    var edges = GetBoxEdges(box);
                    
                    // 计算每条边到鼠标的最短距离
                    foreach (var edge in edges)
                    {
                        // 将边的端点转换为屏幕坐标
                        Point2d screenPt1 = viewport.WorldToClient(edge.From);
                        Point2d screenPt2 = viewport.WorldToClient(edge.To);
                        
                        // 计算鼠标点到线段的距离
                        double dist = DistanceToLineSegment(
                            new Point2d(screenPoint.X - viewRect.Left, screenPoint.Y - viewRect.Top),
                            screenPt1,
                            screenPt2
                        );

                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestIndex = candidate.index;
                        }
                    }
                }
            }
            // 如果只有一个候选对象，直接返回
            else if (candidateIndices.Count == 1)
            {
                nearestIndex = candidateIndices[0].index;
            }

            return nearestIndex;
        }

        private Line[] GetBoxEdges(BoundingBox box)
        {
            var corners = box.GetCorners();
            return new Line[]
            {
                // 底部四边
                new Line(corners[0], corners[1]),
                new Line(corners[1], corners[2]),
                new Line(corners[2], corners[3]),
                new Line(corners[3], corners[0]),
                // 顶部四边
                new Line(corners[4], corners[5]),
                new Line(corners[5], corners[6]),
                new Line(corners[6], corners[7]),
                new Line(corners[7], corners[4]),
                // 竖直四边
                new Line(corners[0], corners[4]),
                new Line(corners[1], corners[5]),
                new Line(corners[2], corners[6]),
                new Line(corners[3], corners[7])
            };
        }

        private double DistanceToLineSegment(Point2d pt, Point2d lineStart, Point2d lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            
            if (dx == 0 && dy == 0) // 线段实际上是一个点
            {
                return pt.DistanceTo(lineStart);
            }

            // 计算投影点参数
            double t = ((pt.X - lineStart.X) * dx + (pt.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);

            if (t < 0) // 最近点在线段起点之前
            {
                return pt.DistanceTo(lineStart);
            }
            else if (t > 1) // 最近点在线段终点之后
            {
                return pt.DistanceTo(lineEnd);
            }
            else // 最近点在线段上
            {
                Point2d projection = new Point2d(
                    lineStart.X + t * dx,
                    lineStart.Y + t * dy
                );
                return pt.DistanceTo(projection);
            }
        }

        private void FocusOnComponent(IGH_DocumentObject obj)
        {
            // 定位到组件
            var canvas = Instances.ActiveCanvas;
            canvas.Viewport.Zoom = 2;
            canvas.Viewport.MidPoint = obj.Attributes.Pivot;
            
            // 选中组件
            canvas.Document.DeselectAll();
            obj.Attributes.Selected = true;
            canvas.Refresh();
        }

        #region IGH_PreviewObject Implementation
        public bool Hidden { get; set; } = false;
        public bool IsPreviewCapable => true;
        public BoundingBox ClippingBox
        {
            get
            {
                if (previewMesh == null) return BoundingBox.Empty;
                
                // 即使mesh无效，也尝试从顶点创建boundingbox
                if (previewMesh.Vertices != null && previewMesh.Vertices.Count > 0)
                {
                    Point3d[] points = previewMesh.Vertices.ToPoint3dArray();
                    return new BoundingBox(points);
                }
                
                return BoundingBox.Empty;
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            //if (isActive && previewMesh != null && previewMaterial != null)
            //{
            //    args.Display.DrawMeshShaded(previewMesh, previewMaterial);
            //}
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            // 可以选择是否添加线框绘制
        }
        #endregion

        private void DisplayPipeline_CalculateBoundingBox(object sender, CalculateBoundingBoxEventArgs e)
        {
            if (isActive && previewMesh != null)
            {
                e.IncludeBoundingBox(previewMesh.GetBoundingBox(true));
            }
        }

        private void DisplayPipeline_DrawForeground(object sender, DrawEventArgs e)
        {
            if (isActive && previewMesh != null && previewMaterial != null)
            {
                e.Display.DrawMeshShaded(previewMesh, previewMaterial);
                e.Display.DrawMeshWires(previewMesh, previewColor,3);
            }
        }
    }
}