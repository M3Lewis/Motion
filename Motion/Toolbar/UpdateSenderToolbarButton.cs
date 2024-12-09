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

                float maxRightBoundary = float.MinValue;
                foreach (GH_NumberSlider slider in allTimelineSliders)
                {
                    float rightBoundary = slider.Attributes.Bounds.Right;
                    if (rightBoundary > maxRightBoundary)
                    {
                        maxRightBoundary = rightBoundary;
                    }
                }

                float uniformX = maxRightBoundary + 150;

                foreach (GH_NumberSlider timelineSlider in allTimelineSliders)
                {
                    bool isConnectedToSender = false;
                    bool isUnionSlider = timelineSlider.NickName == "TimeLine(Union)";

                    var range = new Interval((double)timelineSlider.Slider.Minimum, (double)timelineSlider.Slider.Maximum);
                    var rangeStr = range.ToString();
                    var splitStr = rangeStr.Split(',');
                    var potentialNickname = string.Join("-", splitStr);

                    var existingSender = doc.Objects
                        .OfType<MotionSender>()
                        .FirstOrDefault(s => s.NickName == potentialNickname);

                    if (existingSender != null)
                    {
                        continue;
                    }

                    foreach (var recipient in timelineSlider.Recipients)
                    {
                        if (recipient is MotionSender)
                        {
                            isConnectedToSender = true;
                            break;
                        }
                    }

                    if (isConnectedToSender || isUnionSlider) continue;

                    MotionSender remoteSender = new MotionSender();
                    doc.AddObject(remoteSender, false);

                    PointF sliderPivot = timelineSlider.Attributes.Pivot;
                    float offsetY = timelineSlider.Attributes.Bounds.Height / 2;
                    remoteSender.Attributes.Pivot = new PointF(uniformX, sliderPivot.Y + offsetY);

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