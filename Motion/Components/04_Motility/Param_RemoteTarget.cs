using Grasshopper.Kernel;
using System;
using System.Linq;

namespace Motion.Components.OBSOLETE
{
    public class Param_RemoteTarget : Param_RemoteCameraPointBase
    {
        public Param_RemoteTarget() : base()
        {
            Name = "Remote Target";
            Description = "Motion Remote Target Parameter";
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("A45D8759-6C07-4C65-8E99-D2E6E2E678DB");
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            ConnectToMergeComponent();
        }
        protected override void ConnectToMergeComponent()
        {
            //var doc = OnPingDocument();
            //if (doc == null) return;

            //var mergeComps = doc.Objects
            //    .OfType<MergeCameraTarget>()
            //    .Where(m => m.NickName == "MotionTarget");

            //foreach (var mergeComp in mergeComps)
            //{
            //    foreach (var param in mergeComp.Params.Input)
            //    {
            //        if (param.NickName == this.NickName && !param.Sources.Contains(this))
            //        {
            //            param.AddSource(this);
            //            break;
            //        }
            //    }
            //}

            //ExpireSolution(true);
        }

        protected override Param_RemoteCameraPointBase CreateInstance()
        {
            return new Param_RemoteTarget();
        }

        public override void ReconnectToMergeComponents()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 专门查找 MergeCameraTarget 组件
            var mergeComponents = doc.Objects
                .Where(obj => obj.Name.Contains("MergeCameraTarget") && obj is IGH_Component)
                .Cast<IGH_Component>();

            foreach (var merge in mergeComponents)
            {
                // 检查所有输入参数
                foreach (var param in merge.Params.Input)
                {
                    if (param.NickName == this.NickName)
                    {
                        param.AddSource(this);
                    }
                }
            }
        }
    }
} 