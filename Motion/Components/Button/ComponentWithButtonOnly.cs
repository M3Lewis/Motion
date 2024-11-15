using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Motion.Button
{
    public abstract class ComponentWithButtonOnly : GH_PersistentParam<GH_Boolean>
    {
        public bool ButtonPressed
        {
            get
            {
                if (m_attributes is ParamButton paramButton)
                {
                    return paramButton.isPressed;
                }
                return false;
            }
            set
            {
                if (m_attributes is ParamButton paramButton)
                {
                    paramButton.isPressed = value;
                }
            }
        }

        public bool IsDataCollected
        {
            get
            {
                if (base.PersistentData == null || base.PersistentData.DataCount <= 0)
                {
                    return false;
                }
                base.PersistentData.AllData(skipNulls: true).FirstOrDefault().CastTo<bool>(out var target);
                return target;
            }
            set
            {
                if (base.PersistentData != null)
                {
                    base.PersistentData.ClearData();
                    base.PersistentData.Append(new GH_Boolean(value));
                    base.VolatileData.ClearData();
                    AddVolatileData(new GH_Path(0), 0, new GH_Boolean(value));
                }
            }
        }

        public override Guid ComponentGuid { get; }

        protected ComponentWithButtonOnly(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
            IsDataCollected = false;
            base.SolutionExpired += OnSolutionExpired;
        }

        protected override GH_GetterResult Prompt_Singular(ref GH_Boolean value)
        {
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_Boolean> values)
        {
            return GH_GetterResult.success;
        }

        public abstract void FindAssociatedComponents();

        public override void CreateAttributes()
        {
            m_attributes = new ParamButton(this);
        }

        public abstract void OnSolutionExpired(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e);
    }
}