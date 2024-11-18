using System;

namespace Motion.Animation
{
    public class MergeCameraLocation : MergeCameraBase
    {
        public MergeCameraLocation()
            : base("MergeCameraLocation", "MergeCameraLocation", "根据slider数量生成输入端，合并Camera Location数据")
        {
            this.NickName = "MotionLocation";
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MergeCameraLocation;
        public override Guid ComponentGuid => new Guid("5d42d439-ef71-4148-81a2-f6b65578f5bf");
        protected override MergeCameraType ComponentType => MergeCameraType.Location;
    }
} 