using Grasshopper.Kernel;
using System;
using System.Linq;

namespace Motion.Components.OBSOLETE
{
    public class Param_RemoteLocation : Param_RemoteCameraPointBase
    {
        public Param_RemoteLocation() : base()
        {
            Name = "Remote Location";
            Description = "Motion Remote Location Parameter";
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("A45D8759-6C07-4C65-8E99-D2E6E2E678DA");

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
            //    .OfType<MergeCameraLocation>()
            //    .Where(m => m.NickName == "MotionLocation");

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
            return new Param_RemoteLocation();
        }

        public override void ReconnectToMergeComponents()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var mergeComponents = doc.Objects
                .Where(obj => obj.Name.Contains("MergeCameraLocation") && obj is IGH_Component)
                .Cast<IGH_Component>();

            foreach (var merge in mergeComponents)
            {
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