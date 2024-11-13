using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Motion.Components
{
    public class TimelineRangeComponent : GH_Component
    {
        private List<GH_NumberSlider> _trackedSliders = new List<GH_NumberSlider>();
        private const string EXCLUDED_NICKNAME = "TimeLine(Union)";

        public override Guid ComponentGuid => new Guid("0cad63d7-7a60-490b-b520-0ca19b310784");
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon => null;

        public TimelineRangeComponent()
            : base("Timeline Range", "TLRange",
                "Output the range of each TimeLine slider on canvas (excluding Union)",
                "Motion", "01_Animation")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new TimelineRangeComponentAttributes(this);
        }

        private class TimelineRangeComponentAttributes : GH_ComponentAttributes
        {
            public TimelineRangeComponentAttributes(TimelineRangeComponent owner) : base(owner) { }

            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                Owner.ExpireSolution(true);
                return GH_ObjectResponse.Handled;
            }
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntervalParameter("Ranges", "R", "Range of each TimeLine slider", GH_ParamAccess.list);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsAdded += Document_ObjectsAdded;
            document.ObjectsDeleted += Document_ObjectsDeleted;
            
            // 初始扫描画布上的所有滑块
            ScanForTimeLineSliders();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            // 确保清理所有事件订阅
            foreach (var slider in _trackedSliders)
            {
                slider.ObjectChanged -= Slider_Changed;
                slider.AttributesChanged -= Slider_Changed;
                slider.Slider.ValueChanged -= Slider_ValueChanged;
            }
            document.ObjectsAdded -= Document_ObjectsAdded;
            document.ObjectsDeleted -= Document_ObjectsDeleted;
            _trackedSliders.Clear();
            base.RemovedFromDocument(document);
        }

        private bool ShouldTrackSlider(GH_NumberSlider slider)
        {
            return slider.NickName != EXCLUDED_NICKNAME 
                   && slider.GetType().Name == "pOd_TimeLineSlider";
        }

        private void Document_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            foreach (var obj in e.Objects)
            {
                if (obj is GH_NumberSlider slider && ShouldTrackSlider(slider))
                {
                    if (!_trackedSliders.Contains(slider))
                    {
                        _trackedSliders.Add(slider);
                        slider.ObjectChanged += Slider_Changed;
                        slider.AttributesChanged += Slider_Changed;
                        slider.Slider.ValueChanged += Slider_ValueChanged;
                        ExpireSolution(true);
                    }
                }
            }
        }

        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            bool needUpdate = false;
            foreach (var obj in e.Objects)
            {
                if (obj is GH_NumberSlider slider)
                {
                    if (_trackedSliders.Contains(slider))
                    {
                        _trackedSliders.Remove(slider);
                        slider.ObjectChanged -= Slider_Changed;
                        slider.AttributesChanged -= Slider_Changed;
                        slider.Slider.ValueChanged -= Slider_ValueChanged;
                        needUpdate = true;
                    }
                }
            }
            if (needUpdate)
            {
                ExpireSolution(true);
            }
        }

        private void Slider_Changed(object sender, EventArgs e)
        {
            ExpireSolution(true);
        }

        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            ExpireSolution(true);
        }

        private void ScanForTimeLineSliders()
        {
            // 先清除所有事件订阅
            foreach (var slider in _trackedSliders)
            {
                slider.ObjectChanged -= Slider_Changed;
                slider.AttributesChanged -= Slider_Changed;
                slider.Slider.ValueChanged -= Slider_ValueChanged;
            }
            _trackedSliders.Clear();

            if (OnPingDocument() == null) return;

            foreach (var obj in OnPingDocument().Objects)
            {
                if (obj is GH_NumberSlider slider && ShouldTrackSlider(slider))
                {
                    _trackedSliders.Add(slider);
                    slider.ObjectChanged += Slider_Changed;
                    slider.AttributesChanged += Slider_Changed;
                    slider.Slider.ValueChanged += Slider_ValueChanged;
                }
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_trackedSliders.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid TimeLine sliders found on canvas");
                return;
            }

            // 创建区间列表
            List<Interval> ranges = new List<Interval>();

            // 为每个滑块创建一个区间
            foreach (var slider in _trackedSliders)
            {
                double min = (double)slider.Slider.Minimum;
                double max = (double)slider.Slider.Maximum;
                ranges.Add(new Interval(min, max));
            }

            // 输出区间列表
            DA.SetDataList(0, ranges);
        }
    }
}