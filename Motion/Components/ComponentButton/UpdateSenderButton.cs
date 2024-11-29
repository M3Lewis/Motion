using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System;
using System.Drawing;
using Motion.Motility;
using System.Linq;
using Rhino.Geometry;
using Motion.Animation;

namespace Motion.Button
{
    public class Param_UpdateSenderButton : ComponentWithButtonOnly
    {
        private List<GH_NumberSlider> allTimelineSliders = new List<GH_NumberSlider>();

        protected override Bitmap Icon => Properties.Resources.UpdateSender;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("af57219a-1d36-4edc-9527-0707824fb52c");

        public Param_UpdateSenderButton()
            : base("Update Sender", "Update Sender", "Place Sender besides of all TimelineSliders", "Motion", "04_Motility")
        {
        }

        public override void OnSolutionExpired(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            FindAssociatedComponents();
            try
            {
                if (base.ButtonPressed)
                {
                    GH_Document doc = OnPingDocument();

                    // 找出所有slider右边界的最大x值
                    float maxRightBoundary = float.MinValue;
                    foreach (GH_NumberSlider slider in allTimelineSliders)
                    {
                        float rightBoundary = slider.Attributes.Bounds.Right;
                        if (rightBoundary > maxRightBoundary)
                        {
                            maxRightBoundary = rightBoundary;
                        }
                    }

                    // 设置统一的x坐标（最大右边界 + 50）
                    float uniformX = maxRightBoundary + 150;

                    foreach (GH_NumberSlider timelineSlider in allTimelineSliders)
                    {
                        // 检查 slider 是否已经连接到 sender
                        bool isConnectedToSender = false;
                        bool isUnionSlider = false;
                        if (timelineSlider.NickName == "TimeLine(Union)")
                        {
                            isUnionSlider = true;
                        }

                        // 检查是否已经存在使用相同范围作为昵称的 Sender
                        var range = new Interval((double)timelineSlider.Slider.Minimum, (double)timelineSlider.Slider.Maximum);
                        var rangeStr = range.ToString();
                        var splitStr = rangeStr.Split(',');
                        var potentialNickname = string.Join("-", splitStr);

                        var existingSender = doc.Objects
                            .OfType<Param_RemoteSender>()
                            .FirstOrDefault(s => s.NickName == potentialNickname);

                        if (existingSender != null)
                        {
                            continue;  // 跳过已经存在相同昵称的情况
                        }

                        foreach (var recipient in timelineSlider.Recipients)
                        {
                            if (recipient is Param_RemoteSender)
                            {
                                isConnectedToSender = true;
                                break;
                            }
                        }

                        // 如果已经连接到 sender 或是 union slider，跳过这个 slider
                        if (isConnectedToSender || isUnionSlider) continue;

                        // 创建新的 Param_RemoteSender
                        Param_RemoteSender remoteSender = new Param_RemoteSender();
                        doc.AddObject(remoteSender, false);

                        // 设置位置 - 使用统一的x坐标，y坐标保持不变
                        PointF sliderPivot = timelineSlider.Attributes.Pivot;
                        float offsetY = timelineSlider.Attributes.Bounds.Height / 2;
                        remoteSender.Attributes.Pivot = new PointF(uniformX, sliderPivot.Y + offsetY);

                        // 连接 slider 到 remoteSender
                        remoteSender.AddSource(timelineSlider);
                    }
                    base.ButtonPressed = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                base.ButtonPressed = false;
                throw new Exception("无法完成操作: " + ex.Message);
            }
        }

        public override void FindAssociatedComponents()
        {
            try
            {
                allTimelineSliders = new List<GH_NumberSlider>();
                GH_Document gH_Document = OnPingDocument();

                // 检查文档是否为空
                if (gH_Document == null || gH_Document.Objects == null)
                {
                    return;
                }

                // 查找所有 pOd_TimeLineSlider，排除 TimeLine(Union)
                var sliders = gH_Document.Objects
                    .Where(o => o != null && 
                               o.GetType().ToString() == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider" ||
                               o.GetType().ToString() == "Motion.Animation.MotionSlider" &&
                               o.NickName != "TimeLine(Union)")
                    .Cast<GH_NumberSlider>()
                    .ToList();

                // 检查 sliders 是否为空
                if (sliders != null && sliders.Any())
                {
                    allTimelineSliders.AddRange(sliders);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                Rhino.RhinoApp.WriteLine($"Error in FindAssociatedComponents: {ex.Message}");
                allTimelineSliders = new List<GH_NumberSlider>(); // 确保列表不为 null
            }
        }
    }
}