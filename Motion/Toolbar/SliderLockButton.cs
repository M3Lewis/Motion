using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Motion.Animation;

namespace Motion.Toolbar
{
    public class SliderLockButton : MotionToolbarButton
    {
        private ToolStripButton button;
        private bool isLocked = false;
        protected override int ToolbarOrder => 70;
        // 添加公共属性以访问锁定状态
        public bool IsLocked => isLocked;

        private void AddSliderLockButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button);
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;

            if (editor == null) return;
            AddSliderLockButton();
        }

        private void Instantiate()
        {
            button.Name = "Lock Sliders Position";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.SliderLock; // 需要添加对应的图标资源
            button.ToolTipText = "Lock/Unlock all Motion Sliders position on canvas";
            button.Click += ToggleSlidersLock;
            button.CheckOnClick = true;
        }

        private void ToggleSlidersLock(object sender, EventArgs e)
        {
            isLocked = !isLocked;

            if (isLocked)
            {
                button.BackColor = Color.Orange;
            }
            else
            {
                button.BackColor = Color.FromArgb(255, 255, 255);
            }

            var doc = Instances.ActiveCanvas?.Document;
            if (doc == null) return;

            // 获取所有的 MotionSlider
            var allSliders = doc.Objects
                .OfType<MotionSlider>()
                .ToList();

            foreach (var slider in allSliders)
            {
                try
                {
                    // 保存原有的位置和边界信息
                    var oldBounds = slider.Attributes.Bounds;
                    var oldPivot = slider.Attributes.Pivot;

                    if (isLocked)
                    {
                        // 锁定逻辑保持不变
                        if (slider is MotionUnionSlider unionSlider)
                        {
                            var lockAttrs = new MotionUnionSliderLockAttributes(unionSlider);
                            lockAttrs.Bounds = oldBounds;
                            lockAttrs.Pivot = oldPivot;
                            unionSlider.Attributes = lockAttrs;
                        }
                        else if (slider is MotionSlider motionSlider)
                        {
                            var lockAttrs = new MotionSliderLockAttributes(motionSlider);
                            lockAttrs.Bounds = oldBounds;
                            lockAttrs.Pivot = oldPivot;
                            motionSlider.Attributes = lockAttrs;
                        }
                    }
                    else
                    {
                        // 恢复正常的 Attributes
                            slider.CreateAttributes();
                        slider.Attributes.Bounds = oldBounds;
                        slider.Attributes.Pivot = oldPivot;
                        
                        // 强制更新布局
                        slider.Attributes.ExpireLayout();
                    }
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine($"Error processing slider: {ex.Message}");
                }
            }

            
            
            // 刷新画布以更新视觉效果
            Instances.ActiveCanvas.Refresh();
        }

    }
} 