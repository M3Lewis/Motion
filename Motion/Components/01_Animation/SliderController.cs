using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;

namespace Motion.Animation
{
    public class SliderController : GH_Component
    {
        private List<MotionSlider> _controlledSliders;
        private MotionSlider _unionSlider;

        public SliderController() 
            : base("Slider Controller", "Controller",
                "自动控制画布上所有Motion Slider的统一时间轴",
                "Motion", "01_Animation")
        {
            _controlledSliders = new List<MotionSlider>();
        }

        public override Guid ComponentGuid => new Guid("495FF9AA-69A1-47DC-BF0E-A6C735EDBC67");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // 移除输入参数
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Union Slider", "U", "统一控制的滑块", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 获取当前文档中的所有 MotionSlider
            var doc = OnPingDocument();
            if (doc == null) return;

            var allSliders = doc.Objects
                .OfType<MotionSlider>()
                .Where(slider => slider != _unionSlider) // 排除统一控制滑块自身
                .ToList();

            // 清除旧的控制关系
            foreach (var slider in _controlledSliders)
            {
                if (slider != null) 
                {
                    slider.IsControlled = false;
                }
            }
            
            if (_unionSlider != null)
            {
                _unionSlider.ValueChanged -= OnUnionSliderValueChanged;
            }
            
            _controlledSliders.Clear();
            _controlledSliders.AddRange(allSliders);

            // 建立新的控制关系
            foreach (var slider in _controlledSliders)
            {
                slider.IsControlled = true;
            }

            // 创建或更新 UnionSlider
            if (_unionSlider == null)
            {
                _unionSlider = new MotionSlider();
                _unionSlider.NickName = "UnionSlider";
                _unionSlider.CreateAttributes();
            }
            
            // 添加值变化事件处理
            _unionSlider.ValueChanged += OnUnionSliderValueChanged;

            DA.SetData(0, _unionSlider);
        }

        private void OnUnionSliderValueChanged(object sender, decimal value)
        {
            foreach (var slider in _controlledSliders)
            {
                slider.UpdateValue(value);
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (_unionSlider != null)
            {
                _unionSlider.ValueChanged -= OnUnionSliderValueChanged;
            }
            
            // 清除所有控制关系
            foreach (var slider in _controlledSliders)
            {
                if (slider != null)
                {
                    slider.IsControlled = false;
                }
            }
            
            base.RemovedFromDocument(document);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            // 组件添加到文档时立即触发一次求解
            ExpireSolution(true);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            base.DocumentContextChanged(document, context);
            // 当文档上下文改变时（比如添加/删除组件时）触发求解
            ExpireSolution(true);
        }
    }
} 