using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI.Base;

namespace Motion.Utils
{
    public class IntervalSwitcher : GH_Component
    {
        public IntervalSwitcher()
          : base("Interval Switcher", "IntSwitch",
              "请和V-Ray插件的V-Ray Timeline Component以及Metahopper插件的Set Slider Properties Component配合使用。\nUse with the V-Ray Timeline Component of the V-Ray plug-in and the Set Slider Properties Component of the Metahopper plug-in.",
              "Motion", "01_Animation")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Toggle", "T", "切换模式 (True: 使用区间, False: 使用滑块范围)\nSwitch mode (True: use Interval, False: use Slider range)", GH_ParamAccess.item, false);
            pManager.AddIntervalParameter("Domain", "D", "输入区间\nInput interval", GH_ParamAccess.item);
            pManager.AddGenericParameter("Slider", "S", "连接一个数字滑块\nConnect a Number Slider", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Frames", "Frames", "帧数 (滑块最大值 - 最小值 + 1)\nNumber of frames (Slider Max - Min + 1)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Start", "Start", "区间的起始值\nStart value of the interval", GH_ParamAccess.item);
            pManager.AddNumberParameter("End", "End", "区间的结束值\nEnd value of the interval", GH_ParamAccess.item);
            pManager.AddGenericParameter("Slider Object", "Slider", "获取到的滑块对象\nThe retrieved slider object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool toggle = false;
            Interval domain = Interval.Unset;
            object sliderInput = null;

            if (!DA.GetData(0, ref toggle)) return;
            if (!DA.GetData(1, ref domain)) return;
            if (!DA.GetData(2, ref sliderInput)) return;

            GH_NumberSlider numberSlider = null;
            if (Params.Input[2].Sources.Count > 0)
            {
                IGH_Param sourceParam = Params.Input[2].Sources[0];
                if (sourceParam is GH_NumberSlider sliderComponent)
                {
                    numberSlider = sliderComponent;
                }
                else if (sourceParam.Attributes?.GetTopLevel?.DocObject is GH_NumberSlider topLevelSlider)
                {
                     numberSlider = topLevelSlider;
                }
                 else if (sliderInput is GH_NumberSlider inputAsSlider)
                 {
                     numberSlider = inputAsSlider;
                 }
                 else if (sliderInput is GH_ObjectWrapper wrapper && wrapper.Value is GH_NumberSlider wrappedSlider)
                 {
                     numberSlider = wrappedSlider;
                 }
            }


            if (numberSlider == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "输入 'Slider' 必须连接到一个 GH_NumberSlider。\nInput 'Slider' must be connected to a GH_NumberSlider.");
                return;
            }


            double startValue = 0;
            double endValue = 0;
            double frameCount = 0;

            if (toggle)
            {
                if (!domain.IsValid)
                {
                     AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入区间无效。\nInput 'Domain' is not a valid interval.");
                     startValue = 0;
                     endValue = 0;
                }
                else
                {
                    startValue = domain.T0;
                    endValue = domain.T1;
                }
            }
            else
            {
                startValue = (double)numberSlider.Slider.Minimum;
                endValue = (double)numberSlider.Slider.Maximum;
            }

            if (numberSlider.Slider.Type == GH_SliderAccuracy.Integer)
            {
                 frameCount = (double)(numberSlider.Slider.Maximum - numberSlider.Slider.Minimum + 1);
            }
            else
            {
                 frameCount = (double)(numberSlider.Slider.Maximum - numberSlider.Slider.Minimum + 1);
            }


            var recipients = Params.Output[3].Recipients;
            if (recipients.Count > 0)
            {
                 IGH_Param recipientParam = recipients[0];
                 if (recipientParam.Attributes?.GetTopLevel?.DocObject is GH_ActiveObject activeObj)
                 {
                     if (activeObj.Locked != toggle)
                     {
                         activeObj.Locked = toggle;
                         activeObj.ExpireSolution(true);
                     }
                 }
            }

            DA.SetData(0, frameCount);
            DA.SetData(1, startValue);
            DA.SetData(2, endValue);
            DA.SetData(3, numberSlider);
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override System.Drawing.Bitmap Icon => Motion.Properties.Resources.IntervalSwitcher;

        public override Guid ComponentGuid => new Guid("a8d4b1e0-3f5c-4a9e-8b1d-7e2c9f0a1b3e");
    }
}