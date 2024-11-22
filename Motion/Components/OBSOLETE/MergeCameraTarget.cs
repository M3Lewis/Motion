using Grasshopper.Kernel;
using System;

namespace Motion.Components.OBSOLETE
{
    public class MergeCameraTarget : MergeCameraBase
    {
        protected override MergeCameraType ComponentType => MergeCameraType.Target;

        public MergeCameraTarget()
            : base("MergeCameraTarget", "MergeCameraTarget", "根据slider数量生成输入端，合并Camera Target数据")
        {
            NickName = "MotionTarget";
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MergeCameraTarget;
        public override Guid ComponentGuid => new Guid("f4ecaa4a-cc47-4223-ac4b-c9d8dfeb6e6d");
        public override GH_Exposure Exposure => GH_Exposure.hidden;
    }
}