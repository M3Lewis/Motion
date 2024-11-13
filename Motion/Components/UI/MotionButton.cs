using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Motion.UI
{
    public class MotionButton : MotionButtonTemplate
    {
        public MotionButton(IGH_Component owner, string openButtonText, string exportButtonText, Action<object, GH_CanvasMouseEvent,bool> buttonClickHandler) : base(owner,openButtonText,exportButtonText,buttonClickHandler) { }
    }
}
