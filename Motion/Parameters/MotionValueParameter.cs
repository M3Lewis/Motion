using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel.Data;
using GH_IO;

namespace Motion.Parameters
{
    public class MotionValueGoo : IGH_Goo, GH_ISerializable
    {
        private double value;

        public MotionValueGoo(double value = 0.0)
        {
            this.value = value;
        }

        public double Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public bool IsValid => true;
        public string IsValidWhyNot => string.Empty;
        public string TypeName => "Motion Value";
        public string TypeDescription => "Motion timeline interpolated value";

        public IGH_Goo Duplicate()
        {
            return new MotionValueGoo(value);
        }

        public IGH_GooProxy EmitProxy()
        {
            return null;
        }

        public object ScriptVariable()
        {
            return value;
        }

        public bool CastFrom(object source)
        {
            if (source == null) return false;

            if (source is GH_Number number)
            {
                value = number.Value;
                return true;
            }
            
            if (source is GH_Integer integer)
            {
                value = integer.Value;
                return true;
            }

            if (source is double d)
            {
                value = d;
                return true;
            }

            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (typeof(T) == typeof(GH_Number))
            {
                target = (T)(object)new GH_Number(value);
                return true;
            }

            target = default;
            return false;
        }

        // 实现序列化接口
        public bool Write(GH_IWriter writer)
        {
            writer.SetDouble("Value", value);
            return true;
        }

        public bool Read(GH_IReader reader)
        {
            value = reader.GetDouble("Value");
            return true;
        }

        public override string ToString()
        {
            return $"{value:F3}";
        }
    }

    public class MotionValueParameter : GH_PersistentParam<MotionValueGoo>
    {
        public MotionValueParameter()
            : base(new GH_InstanceDescription(
                "Motion Value",   
                "MValue",        
                "Motion timeline current value", 
                "Motion",        
                "Parameters"))   
        {
        }

        public override Guid ComponentGuid => new Guid("8644399d-b506-40bd-b574-4409c25fc93b");

        protected override MotionValueGoo InstantiateT()
        {
            return new MotionValueGoo();
        }

        public override void ClearData()
        {
            base.ClearData();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
        }

        protected override GH_GetterResult Prompt_Singular(ref MotionValueGoo value)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<MotionValueGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            if (VolatileData.IsEmpty)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No data collected");
            }
        }
    }
} 