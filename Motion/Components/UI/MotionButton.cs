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
        public MotionButton(IGH_Component owner, string button1Text, string button2Text, Action<object, GH_CanvasMouseEvent,bool> buttonClickHandler) : base(owner,button1Text, button2Text, buttonClickHandler) { }
    }
}
