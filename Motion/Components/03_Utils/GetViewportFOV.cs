using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino;

namespace Motion.Utils
{
    public class GetViewportFOV : GH_Component
    {
        public GetViewportFOV()
          : base("Get Viewport FOV", "ViewportFOV",
              "Gets the vertical and horizontal Field of View (FOV) angles for a specified Rhino viewport.",
              "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "N", "Name of the Rhino viewport.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Vertical FOV", "VFOV", "Vertical Field of View angle in degrees.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Horizontal FOV", "HFOV", "Horizontal Field of View angle in degrees.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string viewName = null;
            if (!DA.GetData(0, ref viewName)) return;

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document found.");
                return;
            }

            RhinoView view = doc.Views.Find(viewName, true);
            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Viewport '{viewName}' not found.");
                return;
            }

            var viewport = view.ActiveViewport;
            if (viewport == null)
            {
                 AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not get active viewport for view '{viewName}'.");
                return;
            }

            var vpInfo = new ViewportInfo(viewport);
            double verticalFov = 0;
            double horizontalFov = 0;

            if (vpInfo.GetCameraAngles(
                out double halfDiagonalAngleRadians,
                out double halfVerticalAngleRadians,
                out double halfHorizontalAngleRadians)
                )
            {
                var verticalAngle = 2.0d * halfVerticalAngleRadians;
                var horizontalAngle = 2.0d * halfHorizontalAngleRadians;
                verticalFov = RhinoMath.ToDegrees(verticalAngle);
                horizontalFov = RhinoMath.ToDegrees(horizontalAngle);
            }
            else
            {
                 AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not retrieve camera angles for viewport '{viewName}'.");
            }


            DA.SetData(0, verticalFov);
            DA.SetData(1, horizontalFov);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("1E8A4F5B-B7C9-4D3E-8A01-F9C7B2E1D0F0"); // Generate a new unique GUID

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}