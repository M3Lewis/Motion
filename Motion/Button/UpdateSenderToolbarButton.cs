using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Motion.Animation;
using Motion.Motility;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class UpdateSenderToolbarButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 10;
        private ToolStripButton button;
        private List<GH_NumberSlider> allTimelineSliders = new List<GH_NumberSlider>();

        public UpdateSenderToolbarButton()
        {
        }

        private void AddUpdateSenderButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(button);
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
            AddUpdateSenderButton();
        }

        private void Instantiate()
        {
            button.Name = "Update Sender";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.UpdateSender; // 确保有对应的图标资源
            button.ToolTipText = "在多个Motion Slider输出端旁创建Motion Sender";
            button.Click += UpdateSender_Click;
        }

        private void UpdateSender_Click(object sender, EventArgs e)
        {
            FindAssociatedComponents();
            try
            {
                GH_Document doc = Instances.ActiveCanvas?.Document;
                if (doc == null) return;

                var sliderPositions = allTimelineSliders
                    .Where(s => s != null && !(s is MotionSlider))
                    .Select(s => new
                    {
                        Slider = s,
                        Right = s.Attributes.Bounds.Right,
                        Bounds = s.Attributes.Bounds,
                        ConnectRange = new { 
                            Min = s.Attributes.Bounds.Top - s.Attributes.Bounds.Height,
                            Max = s.Attributes.Bounds.Bottom + s.Attributes.Bounds.Height
                        }
                    })
                    .ToList();

                if (!sliderPositions.Any()) return;

                float uniformX = sliderPositions.Max(p => p.Right) + 150;

                foreach (var sliderInfo in sliderPositions)
                {
                    var timelineSlider = sliderInfo.Slider;

                    // 检查是否已经有sender在有效连接范围内
                    bool hasNearbyConnection = timelineSlider.Recipients
                        .OfType<MotionSender>()
                        .Any(sender => 
                            sender.Attributes.Pivot.Y >= sliderInfo.ConnectRange.Min && 
                            sender.Attributes.Pivot.Y <= sliderInfo.ConnectRange.Max
                        );

                    if (hasNearbyConnection) continue;

                    // 创建新的sender
                    MotionSender remoteSender = new MotionSender();
                    doc.AddObject(remoteSender, false);

                    // 设置昵称
                    var range = new Interval(
                        (double)timelineSlider.Slider.Minimum, 
                        (double)timelineSlider.Slider.Maximum
                    );
                    remoteSender.NickName = $"{range.T0}-{range.T1}";

                    // 将sender放在slider的中心位置
                    remoteSender.Attributes.Pivot = new PointF(
                        uniformX, 
                        sliderInfo.Bounds.Top + sliderInfo.Bounds.Height/2
                    );

                    // 建立连接
                    remoteSender.AddSource(timelineSlider);
                }

                doc.NewSolution(false);
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }

        private void FindAssociatedComponents()
        {
            try
            {
                allTimelineSliders = new List<GH_NumberSlider>();
                GH_Document gH_Document = Instances.ActiveCanvas?.Document;

                if (gH_Document == null || gH_Document.Objects == null)
                {
                    return;
                }

                var sliders = gH_Document.Objects
                    .Where(o => o != null && 
                               (o.GetType().ToString() == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider" ||
                                o.GetType().ToString() == "Motion.Animation.MotionSlider") &&
                                o.NickName != "TimeLine(Union)")
                    .Cast<GH_NumberSlider>()
                    .ToList();

                if (sliders != null && sliders.Any())
                {
                    allTimelineSliders.AddRange(sliders);
                }
                else
                {
                    ShowTemporaryMessage(Instances.ActiveCanvas, "请创建一个Motion Slider");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error in FindAssociatedComponents: {ex.Message}");
                allTimelineSliders = new List<GH_NumberSlider>();
            }
        }
    }
} 