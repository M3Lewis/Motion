using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Motion.General
{
    public static class MotionGeneralMethods
    {
        public static void GoComponent(IGH_DocumentObject com)
        {
            PointF view_point = new PointF(com.Attributes.Pivot.X, com.Attributes.Pivot.Y);
            GH_NamedView gH_NamedView = new GH_NamedView("", view_point, 1.5f, GH_NamedViewType.center);
            foreach (IGH_DocumentObject item in com.OnPingDocument().SelectedObjects())
            {
                item.Attributes.Selected = false;
            }
            com.Attributes.Selected = true;
            gH_NamedView.SetToViewport(Instances.ActiveCanvas, 300);
        }
    }
}
