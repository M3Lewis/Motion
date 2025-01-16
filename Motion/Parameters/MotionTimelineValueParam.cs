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
    public class MotionTimelineValueGoo : IGH_Goo, GH_ISerializable
    {
        private double value;

        public MotionTimelineValueGoo(double value = 0.0)
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
            return new MotionTimelineValueGoo(value);
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

    public class MotionTimelineValueParam : GH_PersistentParam<MotionTimelineValueGoo>
    {
        public MotionTimelineValueParam()
            : base(new GH_InstanceDescription(
                "Motion Value",   
                "MValue",        
                "Motion timeline current value", 
                "Motion",        
                "Parameters"))   
        {
        }

        public override Guid ComponentGuid => new Guid("8644399d-b506-40bd-b574-4409c25fc93b");

        protected override MotionTimelineValueGoo InstantiateT()
        {
            return new MotionTimelineValueGoo();
        }

        public override void ClearData()
        {
            base.ClearData();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
        }

        protected override GH_GetterResult Prompt_Singular(ref MotionTimelineValueGoo value)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<MotionTimelineValueGoo> values)
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