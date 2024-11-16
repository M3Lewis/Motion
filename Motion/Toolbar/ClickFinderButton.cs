using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class ClickFinderButton : GH_AssemblyPriority, IGH_PreviewObject
    {
        private ToolStripButton button;
        private bool isActive = false;
        private List<IGH_DocumentObject> previewObjects;
        private List<BoundingBox> boundingBoxes;
        private Mesh previewMesh;
        private DisplayMaterial previewMaterial;
        private Timer checkTimer;

        public ClickFinderButton()
        {
            checkTimer = new Timer();
            checkTimer.Interval = 100;
            checkTimer.Tick += CheckMouseClick;
        }

        private void AddClickFinderButton()
        {
            ToolStrip toolbar = (ToolStrip)Grasshopper.Instances.DocumentEditor.Controls[0].Controls[1];

            button = new ToolStripButton();
            Instantiate();
            toolbar.Items.Add(button);
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
            button.ToolTipText = "Click GH object displayed in rhino viewport to find component";
            button.Click += OpenControlWindow;
            button.CheckOnClick = true;
        }

        public void OpenControlWindow(object sender, EventArgs e)
        {
            isActive = button.Checked;
            if (isActive)
            {
                CollectPreviewObjects();
                checkTimer.Start();
                previewMaterial = new DisplayMaterial(Color.FromArgb(128, 255, 255), 0.3);
                
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
                previewMaterial = new DisplayMaterial(Color.FromArgb(alpha, 255, 255), 0.3);
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
                    var box = previewObj.ClippingBox;
                    if (box.Volume < 0.1) box.Inflate(1);
                    boundingBoxes.Add(box);
                    previewMesh.Append(Mesh.CreateFromBox(box, 1, 1, 1));
                }
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

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                if (Intersection.LineBox(clickRay, boundingBoxes[i], 0.01, out Interval collision) &&
                    collision.T0 > 0 && collision.T0 < minDistance)
                {
                    minDistance = collision.T0;
                    nearestIndex = i;
                }
            }
            return nearestIndex;
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
        public BoundingBox ClippingBox => previewMesh?.GetBoundingBox(true) ?? BoundingBox.Empty;

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (isActive && previewMesh != null && previewMaterial != null)
            {
                args.Display.DrawMeshShaded(previewMesh, previewMaterial);
            }
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
                e.Display.DrawMeshWires(previewMesh, Color.White);
            }
        }
    }
}