using System;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Motion.Utils
{
    public class Win8TileFlipComponent : GH_Component
    {
        public Win8TileFlipComponent()
            : base(
                "Win8 Tile Flip",
                "W8Flip",
                "Animates a seamless flip, scale, and move transition between a tile surface and a fullscreen surface.",
                "Motion",
                "03_Utils")
        {
        }

        public override Guid ComponentGuid => new Guid("9F7A7B0D-6A2E-4C52-84F0-5ECDF8B9152E");

        protected override Bitmap Icon => null;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("SrfA", "A", "Initial tile surface.", GH_ParamAccess.item);
            pManager.AddBrepParameter("SrfB", "B", "Target fullscreen surface.", GH_ParamAccess.item);
            pManager.AddNumberParameter("t", "t", "Animation parameter in [0.0, 1.0].", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("FlipAxis", "Axis", "0 = flip around X axis, 1 = flip around Y axis.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("AnimatedSrf", "S", "Animated brep at time t.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep srfA = null;
            Brep srfB = null;
            double t = 0.0;
            int flipAxis = 0;

            if (!DA.GetData(0, ref srfA)) return;
            if (!DA.GetData(1, ref srfB)) return;
            if (!DA.GetData(2, ref t)) return;
            if (!DA.GetData(3, ref flipAxis)) return;

            if (srfA == null || !srfA.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input SrfA is null or invalid.");
                return;
            }

            if (srfB == null || !srfB.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input SrfB is null or invalid.");
                return;
            }

            t = Math.Max(0.0, Math.Min(1.0, t));
            if (flipAxis != 0 && flipAxis != 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "FlipAxis must be 0 or 1. Value was clamped.");
                flipAxis = flipAxis <= 0 ? 0 : 1;
            }

            BoundingBox bboxA = srfA.GetBoundingBox(true);
            BoundingBox bboxB = srfB.GetBoundingBox(true);
            if (!bboxA.IsValid || !bboxB.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to evaluate bounding box for one or both inputs.");
                return;
            }

            Point3d centerA = bboxA.Center;
            Point3d centerB = bboxB.Center;

            double widthA = Math.Abs(bboxA.Max.X - bboxA.Min.X);
            double heightA = Math.Abs(bboxA.Max.Y - bboxA.Min.Y);
            double widthB = Math.Abs(bboxB.Max.X - bboxB.Min.X);
            double heightB = Math.Abs(bboxB.Max.Y - bboxB.Min.Y);

            Point3d centerT = centerA + (centerB - centerA) * t;
            double widthT = widthA + (widthB - widthA) * t;
            double heightT = heightA + (heightB - heightA) * t;

            Vector3d axis = flipAxis == 0 ? Vector3d.XAxis : Vector3d.YAxis;

            Brep working;
            Point3d baseCenter;
            double baseWidth;
            double baseHeight;
            double angle;

            if (t < 0.5)
            {
                working = srfA.DuplicateBrep();
                baseCenter = centerA;
                baseWidth = widthA;
                baseHeight = heightA;
                angle = t * Math.PI;
            }
            else
            {
                working = srfB.DuplicateBrep();
                baseCenter = centerB;
                baseWidth = widthB;
                baseHeight = heightB;
                angle = (t - 1.0) * Math.PI;
            }

            if (working == null || !working.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to duplicate working brep.");
                return;
            }

            double scaleX = Math.Abs(baseWidth) > RhinoMath.ZeroTolerance ? widthT / baseWidth : 1.0;
            double scaleY = Math.Abs(baseHeight) > RhinoMath.ZeroTolerance ? heightT / baseHeight : 1.0;

            if (double.IsNaN(scaleX) || double.IsInfinity(scaleX))
            {
                scaleX = 1.0;
            }

            if (double.IsNaN(scaleY) || double.IsInfinity(scaleY))
            {
                scaleY = 1.0;
            }

            Transform moveToOrigin = Transform.Translation(Point3d.Origin - baseCenter);
            Transform scale = Transform.Scale(Plane.WorldXY, scaleX, scaleY, 1.0);
            Transform rotate = Transform.Rotation(angle, axis, Point3d.Origin);
            Transform moveToCenter = Transform.Translation(centerT - Point3d.Origin);

            bool ok = true;
            ok &= working.Transform(moveToOrigin);
            ok &= working.Transform(scale);
            ok &= working.Transform(rotate);
            ok &= working.Transform(moveToCenter);

            if (!ok || !working.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to transform output brep.");
                return;
            }

            DA.SetData(0, working);
        }
    }
}
