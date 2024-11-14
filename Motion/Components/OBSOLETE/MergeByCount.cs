using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

using GH_IO.Serialization;

using Grasshopper.Kernel.Data;
using Grasshopper;
using System.Windows.Forms;

namespace Motion.Components.OBSOLETE
{
    public class MergeByCount : GH_Component, IGH_VariableParameterComponent
    {
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("{616B593E-50AA-49CC-A0BF-ED96824984FB}");
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public MergeByCount()
          : base("MergeByCount", "MergeByCount", "合并数据",
                   "Motion", "03_Utils")
        {
            VariableParameterMaintenance();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {

            pManager.AddIntegerParameter("Count", "C", "Input count", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Results", "R", "Result of merge", GH_ParamAccess.list);
        }

        public int paramDiff = 0;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int i = 0;

            if (!DA.GetData(0, ref i))
            {
                return;
            }

            //check outputs count
            int currentParamCount = Params.Input.Count;
            int targetParamCount = 1 + i;

            paramDiff = currentParamCount - targetParamCount;

            if (paramDiff != 0)
            {
                GH_Document gdoc = Instances.ActiveCanvas.Document;
                gdoc.ScheduleSolution(0, CallBack);
            }
            else
            {
                DA.SetData(0, i);
            }
        }

        public void CallBack(GH_Document gH_Document)
        {
            //Variable Parameter Maintenance            

            //remove unnecessary inputs
            for (int i = 0; i < paramDiff; i++)
            {
                Params.UnregisterInputParameter(Params.Input[Params.Input.Count - 1]);
                Params.OnParametersChanged();
            }

            //add necessary inputs
            for (int i = 0; i < -paramDiff; i++)
            {
                Param_GenericObject hi = new Param_GenericObject();
                hi.Access = GH_ParamAccess.item;
                hi.Optional = true;
                Params.RegisterInputParam(hi, Params.Input.Count);
            }

            Params.OnParametersChanged();
            VariableParameterMaintenance();
            ExpireSolution(true);
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject();
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            for (int i = 1; i < Params.Input.Count; i++)
            {
                Params.Input[i].Name = (i - 1).ToString();
                Params.Input[i].NickName = (i - 1).ToString();
            }
        }
    }
}
