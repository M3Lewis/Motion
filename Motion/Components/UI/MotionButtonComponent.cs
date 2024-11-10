using Grasshopper.Kernel;
using System;

namespace Motion.UI
{
    public abstract class MotionButtonComponent : GH_Component
    {
        public MotionButtonComponent(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory)
        {
        }
        public bool IsButtonActive
        {
            get
            {
                if (Attributes is MotionButton motionButton)
                {
                    return motionButton.Active;
                }
                return false;
            }
            set
            {
                if (Attributes is MotionButton motionButton)
                {
                    motionButton.Active = value;
                }
            }
        }

        protected override void AfterSolveInstance()
        {
            if (Attributes is MotionButton motionButton)
            {
                motionButton.Active = false;
            }
        }
    }
}
