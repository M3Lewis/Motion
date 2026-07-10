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
        public override int ToolbarOrder => 60;
        private ToolStripButton _button;
        private bool _isActive = false;
        private List<IGH_DocumentObject> _previewObjects;
        private List<BoundingBox> _boundingBoxes;
        private Mesh _previewMesh;
        private DisplayMaterial _previewMaterial;
        private Color _previewColor;
        private Timer _checkTimer;

        public ClickFinderButton()
        {
            _checkTimer = new Timer();
            _checkTimer.Interval = 100;
            _checkTimer.Tick += CheckMouseClick;
        }

        private void AddClickFinderButton()
        {
            InitializeToolbarGroup();
            _button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(_button); // 使用基类方法添加按钮
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
            _button.Name = "Click Finder";
            _button.Size = new Size(24, 24);
            _button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _button.Image = Properties.Resources.ClickFinder;
            _button.ToolTipText =
                General.LanguageManager.GetString("Button.ClickFinder.Tooltip", "单击Rhino视口中显示的GH物件以查找组件");
            _button.Click += ToggleClickFinderMode;
            _button.CheckOnClick = true;
        }

        public override void UpdateLanguage()
        {
            if (_button != null)
            {
                _button.ToolTipText =
                    General.LanguageManager.GetString("Button.ClickFinder.Tooltip", "单击Rhino视口中显示的GH物件以查找组件");
            }
        }

        public void ToggleClickFinderMode(object sender, EventArgs e)
        {
            _isActive = _button.Checked;
            if (_isActive)
                StartClickFinder();
            else
                StopClickFinder();
        }

        private void StartClickFinder()
        {
            CollectPreviewObjects();
            _checkTimer.Start();
            _previewMaterial = new DisplayMaterial(Color.FromArgb(128, 255, 255), 0.3);
            _previewColor = Color.FromArgb(128, 255, 255);
            RhinoDoc.ActiveDoc.Views.Redraw();

            DisplayPipeline.CalculateBoundingBox += DisplayPipeline_CalculateBoundingBox;
            DisplayPipeline.DrawForeground += DisplayPipeline_DrawForeground;
        }

        private void StopClickFinder()
        {
            _checkTimer.Stop();

            DisplayPipeline.CalculateBoundingBox -= DisplayPipeline_CalculateBoundingBox;
            DisplayPipeline.DrawForeground -= DisplayPipeline_DrawForeground;
            
            if (_previewMesh != null)
            {
                _previewMesh.Dispose();
                _previewMesh = null;
            }

            _previewMaterial = null;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private void CheckMouseClick(object sender, EventArgs e)
        {
            if (Control.MouseButtons != MouseButtons.Left) return;
            if (!TryGetClickRay(out Line clickRay)) return;
            int index = FindNearestObject(clickRay);
            ApplyPreviewSelection(index);
        }

        /// <summary>
        /// 根据索引处理预览选择：有效索引聚焦组件，无效索引更新闪烁材质。
        /// </summary>
        /// <param name="index">预览对象索引，-1 表示未找到有效目标。</param>
        private void ApplyPreviewSelection(int index)
        {
            if (index >= 0)
            {
                FocusOnComponent(_previewObjects[index]);
                _button.Checked = false;
                _isActive = false;
                _checkTimer.Stop();

                if (_previewMesh == null) return;

                _previewMesh.Dispose();
                _previewMesh = null;
                _previewMaterial = null;
            }
            else
            {
                int alpha = Convert.ToInt32(255 * DateTime.Now.Millisecond / 1000.0);
                _previewMaterial = new DisplayMaterial(Color.FromArgb(alpha, 255, 255), 0.95);
                _previewColor = Color.FromArgb(alpha, 128, 255, 255);
            }

            RhinoDoc.ActiveDoc?.Views.Redraw();
        }

        private void ProcessPreviewObject(IGH_DocumentObject obj)
        {
            if (obj is not IGH_PreviewObject previewObj || previewObj.Hidden)
                return;

            _previewObjects.Add(obj);

            var vertices = new List<Point3d>();
            CollectVertices(obj, vertices);

            var box = vertices.Count > 0
                ? new BoundingBox(vertices)
                : previewObj.ClippingBox;

            if (box.Volume < 0.1)
                box.Inflate(1);

            _boundingBoxes.Add(box);

            using (Mesh boxMesh = Mesh.CreateFromBox(box, 1, 1, 1))
            {
                if (boxMesh != null && boxMesh.IsValid)
                {
                    boxMesh.Compact();
                    _previewMesh.Append(boxMesh);
                }
            }
        }

        private static void CollectVertices(IGH_DocumentObject obj, List<Point3d> vertices)
        {
            switch (obj)
            {
                case IGH_Component component:
                    foreach (var param in component.Params.Output)
                    foreach (var data in param.VolatileData.AllData(true))
                        AddGeometryVertices(data, vertices);
                    break;

                case IGH_Param param:
                    foreach (var data in param.VolatileData.AllData(true))
                        AddGeometryVertices(data, vertices);
                    break;
            }
        }

        private static void AddGeometryVertices(IGH_Goo data, List<Point3d> vertices)
        {
            switch (data)
            {
                case GH_Point pointData when pointData.Value != Point3d.Unset:
                    vertices.Add(pointData.Value);
                    break;

                case GH_Curve curveData when curveData.Value != null:
                    vertices.AddRange(curveData.Value.GetBoundingBox(false).GetCorners());
                    break;

                case GH_Surface surfaceData when surfaceData.Value != null:
                    vertices.AddRange(surfaceData.Value.GetBoundingBox(false).GetCorners());
                    break;

                case GH_Mesh meshData when meshData.Value != null:
                    vertices.AddRange(meshData.Value.Vertices.ToPoint3dArray());
                    break;

                case GH_Brep brepData when brepData.Value != null:
                    vertices.AddRange(brepData.Value.GetBoundingBox(false).GetCorners());
                    break;
            }
        }

        private void CollectPreviewObjects()
        {
            _previewObjects = new List<IGH_DocumentObject>();
            _boundingBoxes = new List<BoundingBox>();
            if (_previewMesh != null)
                _previewMesh.Dispose();
            _previewMesh = new Mesh();

            foreach (IGH_DocumentObject obj in Instances.ActiveCanvas.Document.Objects)
                ProcessPreviewObject(obj);

            if (!_previewMesh.IsValid)
            {
                _previewMesh.Weld(Math.PI);
                _previewMesh.Compact();
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
            var candidates = CollectCandidates(clickRay);
            return candidates.Count switch
            {
                0 => -1,
                1 => candidates[0].index,
                _ => SelectNearestFromCandidates(candidates)
            };
        }

        private List<(int index, double distance)> CollectCandidates(Line clickRay)
        {
            var candidates = new List<(int index, double distance)>();
            for (int i = 0; i < _boundingBoxes.Count; i++)
            {
                if (Intersection.LineBox(clickRay, _boundingBoxes[i], 0.01, out Interval collision) &&
                    collision.T0 > 0 && collision.T0 < double.MaxValue)
                {
                    candidates.Add((i, collision.T0));
                }
            }

            return candidates;
        }

        private int SelectNearestFromCandidates(List<(int index, double distance)> candidates)
        {
            var screenPoint = Cursor.Position;
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            var viewport = view.ActiveViewport;
            var viewRect = view.ScreenRectangle;
            var mouseScreenPoint = new Point2d(screenPoint.X - viewRect.Left, screenPoint.Y - viewRect.Top);

            int nearestIndex = -1;
            double minDistance = double.MaxValue;

            foreach (var candidate in candidates)
            {
                var box = _boundingBoxes[candidate.index];
                var edges = GetBoxEdges(box);
                foreach (var edge in edges)
                {
                    Point2d screenPt1 = viewport.WorldToClient(edge.From);
                    Point2d screenPt2 = viewport.WorldToClient(edge.To);
                    double dist = DistanceToLineSegment(mouseScreenPoint, screenPt1, screenPt2);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestIndex = candidate.index;
                    }
                }
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
                if (_previewMesh == null) return BoundingBox.Empty;

                // 即使mesh无效，也尝试从顶点创建boundingbox
                if (_previewMesh.Vertices != null && _previewMesh.Vertices.Count > 0)
                {
                    Point3d[] points = _previewMesh.Vertices.ToPoint3dArray();
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
            if (_isActive && _previewMesh != null)
            {
                e.IncludeBoundingBox(_previewMesh.GetBoundingBox(true));
            }
        }

        private void DisplayPipeline_DrawForeground(object sender, DrawEventArgs e)
        {
            if (_isActive && _previewMesh != null && _previewMaterial != null)
            {
                e.Display.DrawMeshShaded(_previewMesh, _previewMaterial);
                e.Display.DrawMeshWires(_previewMesh, _previewColor, 3);
            }
        }
    }
}