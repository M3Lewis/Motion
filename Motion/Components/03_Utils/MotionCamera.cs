using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;

public class MotionCamera : GH_Component
{
    private readonly ViewTable _views = RhinoDoc.ActiveDoc.Views;

    private double _a1;
    private double _a2;
    private double _a3;
    private double _spa;
    private double _lens = 12.0;
    private Point3d _loc;
    private Vector3d _up = Vector3d.ZAxis;
    private Point3d targetp;

    protected override Bitmap Icon => null;

    public override Guid ComponentGuid => new Guid("{7b9c3de2-f1a4-4d85-ac7e-d58f15e8b901}");

    public MotionCamera()
        : base("MotionCamera", "MotionCamera", "Adjusting the active or the specified viewport camera with motion","Motion","01_Animation")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Location", "Loc", "Optional Camera Location", GH_ParamAccess.item);
        pManager.AddPointParameter("Target", "Tar", "Optional Camera Target", GH_ParamAccess.item);
        pManager.AddNumberParameter("Lens", "Lens", "Optional Camera Lens Focal Length", GH_ParamAccess.item,35);
        pManager.AddVectorParameter("Up", "Up", "Optional Camera's Up direction", GH_ParamAccess.item);
        Param_Integer param_Integer = pManager[pManager.AddIntegerParameter("Projection Mode", "Projection", "Perspective Mode \n0-> Parallel View \n1-> Perspective View \n2-> 2D-Perspective View" , GH_ParamAccess.item,1)] as Param_Integer;
        Param_Integer param_Integer2 = pManager[pManager.AddIntegerParameter("View", "View", "Viewport , If omitted,the active viewport will be taken" , GH_ParamAccess.item)] as Param_Integer;
        pManager.AddBooleanParameter("Refresh", "Refresh", "Refresh trigger for recalling the component", GH_ParamAccess.item, @default: true);

        for (int i = 0; i < pManager.ParamCount; i++)
        {
            pManager[i].Optional = true;
        }

        int num = 0;
        foreach (string item in _views.Select((RhinoView c) => c.ActiveViewport.Name))
        {
            param_Integer2?.AddNamedValue(item, num++);
        }

        param_Integer?.AddNamedValue("Parallel View", 0);
        param_Integer?.AddNamedValue("Perspective View", 1);
        param_Integer?.AddNamedValue("2D-Perspective View", 2);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPlaneParameter("Camera", "C", "Camera Location", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Target", "T", "Camera Target", GH_ParamAccess.item);
        pManager.AddNumberParameter("Lens", "L", "Camera Lens Focal Length", GH_ParamAccess.item);
        pManager.AddVectorParameter("Up", "U", "Camera's Up direction", GH_ParamAccess.item);
        pManager.Register_DoubleParam("Horizontal View Angle", "H", "Horizontal lens angle", GH_ParamAccess.item);
        pManager.Register_DoubleParam("Vertical View Angle", "V", "Vertical lens angle", GH_ParamAccess.item);
        pManager.Register_DoubleParam("Screen Ratio", "R", "Screen Port's W/H Aspect", GH_ParamAccess.item);
        pManager.AddRectangleParameter("Frame", "F", "Projection frame", GH_ParamAccess.item);

        if (pManager[4] is IGH_PreviewObject iGH_PreviewObject)
        {
            iGH_PreviewObject.Hidden = true;
        }
        if (pManager[5] is IGH_PreviewObject iGH_PreviewObject2)
        {
            iGH_PreviewObject2.Hidden = true;
        }
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        ViewTable views = RhinoDoc.ActiveDoc.Views;
        bool destination = true;
        if (da.GetData("Refresh", ref destination) && destination)
        {
            int destination2 = 0;
            RhinoViewport rhinoViewport = (da.GetData("View", ref destination2) ? views.ElementAt(destination2).MainViewport : views.ActiveView.MainViewport);
            base.Message = rhinoViewport.Name;
            _loc = rhinoViewport.CameraLocation;
            targetp = rhinoViewport.CameraTarget;
            if (da.GetData("Location", ref _loc) | da.GetData("Target", ref targetp))
            {
                rhinoViewport.SetCameraLocations(targetp, _loc);
            }
            int destination3 = 0;
            if (da.GetData("Projection Mode", ref destination3))
            {
                switch (destination3)
                {
                    case 1:
                        rhinoViewport.ChangeToPerspectiveProjection(symmetricFrustum: true, 50.0);
                        break;
                    case 2:
                        rhinoViewport.ChangeToPerspectiveProjection(symmetricFrustum: false, 50.0);
                        break;
                    default:
                        rhinoViewport.ChangeToParallelProjection(symmetricFrustum: true);
                        break;
                }
            }
            if (da.GetData("Lens", ref _lens))
            {
                rhinoViewport.Camera35mmLensLength = _lens;
            }
            else
            {
                _lens = rhinoViewport.Camera35mmLensLength;
            }
            if (da.GetData("Up", ref _up))
            {
                rhinoViewport.CameraUp = _up;
            }
            else
            {
                _up = rhinoViewport.CameraUp;
            }
            rhinoViewport.GetCameraAngle(out _a1, out _a2, out _a3);
            _spa = rhinoViewport.ScreenPortAspect;
        }
        Vector3d vector3d = Vector3d.CrossProduct(targetp - _loc, _up);
        Plane plane = new Plane(_loc, -vector3d, _up);
        Plane plane2 = new Plane(targetp, vector3d, _up);
        double distance = targetp.DistanceTo(_loc);
        Rectangle3d rectangle3d = new Rectangle3d(plane2, fsize(distance, _a3), fsize(distance, _a2));
        da.SetData(0, plane);
        da.SetData(1, plane2);
        da.SetData(2, _lens);
        da.SetData(3, _up);
        da.SetData(4, _a3);
        da.SetData(5, _a2);
        da.SetData(6, _spa);
        da.SetData(7, rectangle3d);
    }

    private Interval fsize(double distance, double angle)
    {
        double num = Math.Tan(angle) * distance;
        return new Interval(-num, num);
    }

}