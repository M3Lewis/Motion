using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Motion.Components.OBSOLETE
{
    public class MergeBySliderCount : GH_Component, IGH_VariableParameterComponent
    {
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("C43229D5-5C54-48D4-87A9-B3644A074937");
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public MergeBySliderCount()
            : base(
                "MergeBySliderCount",
                "MergeBySliderCount",
                "根据slider数量生成输入端，合并数据",
                "Motion",
                "03_Utils"
            )
        {
            VariableParameterMaintenance();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(string.Empty, string.Empty, string.Empty, GH_ParamAccess.tree);
            pManager.AddGenericParameter(string.Empty, string.Empty, string.Empty, GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "R", "Result of merge", GH_ParamAccess.tree);
        }

        public int paramDiff = 0;
        List<IGH_DocumentObject> sliderObject = new List<IGH_DocumentObject>();
        List<GH_NumberSlider> sliderList = new List<GH_NumberSlider>();
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> oPointList = new List<Point3d>();
            sliderObject = OnPingDocument().Objects.Where((o) =>
               Utility.LikeOperator(o.GetType().ToString(), "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider"))
               .ToList();
            int sliderCount = sliderObject.Count;

            //check outputs count
            int currentParamCount = Params.Input.Count;
            int targetParamCount = 1 + sliderCount;

            paramDiff = currentParamCount - targetParamCount;


            if (paramDiff != 0)
            {
                GH_Document gdoc = Instances.ActiveCanvas.Document;
                gdoc.ScheduleSolution(0, CallBack);
            }
            GH_Structure<IGH_Goo> gH_Structure = new GH_Structure<IGH_Goo>();
            checked
            {
                int num = Params.Input.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    GH_Structure<IGH_Goo> tree = null;
                    if (DA.GetDataTree(i, out tree) && tree != null)
                    {
                        gH_Structure.MergeStructure(tree);
                    }
                }
                DA.SetDataTree(0, gH_Structure);
            }

            DA.SetData(0, oPointList);

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

        private List<GH_NumberSlider> SortSliderObject()
        {
            try
            {
                foreach (var obj in sliderObject)
                {
                    sliderList.Add((GH_NumberSlider)obj);
                }
            }
            catch { }
            List<GH_NumberSlider> orderedSliderList = sliderList.OrderBy(s => s.Attributes.Bounds.Y).ToList();
            return orderedSliderList;
        }
        public void VariableParameterMaintenance()
        {
            List<GH_NumberSlider> sliders = SortSliderObject();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                try
                {
                    Params.Input[i].Name = sliders[i].Name.ToString();
                    Params.Input[i].NickName = sliders[i].NickName.ToString();
                }
                catch
                {
                }
            }
        }
    }
}
