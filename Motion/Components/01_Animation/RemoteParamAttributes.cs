using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;
using Motion.Toolbar;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace Motion.Animation
{
    public class RemoteParamAttributes : GH_FloatingParamAttributes
    {
        private Rectangle m_textBounds; //maintain a rectangle of the text bounds
        private GH_StateTagList m_stateTags;
        // 添加锁定按钮相关字段
        private RectangleF HideButtonBounds;
        private RectangleF LockButtonBounds;
        private RectangleF DataButtonBounds;

        private readonly int ButtonWidth = 18;
        private readonly int ButtonHeight = 18;
        private readonly int ButtonSpacing = 4;

        // 添加折叠按钮相关字段
        private RectangleF CollapseButtonBounds;
        public bool IsCollapsed { get; private set; } = false;

        // 添加鼠标悬停状态字段
        private bool mouseOver = false;

        public RemoteParamAttributes(IGH_Param owner) : base(owner)
        {
        }

        //private bool UpdateAffectedObjects(GH_Canvas sender, Param_RemoteReceiver receiver)
        //{
        //    var selectedObjects = sender.Document.SelectedObjects()?.ToList() ?? new List<IGH_DocumentObject>();
        //    if (!selectedObjects.Any())
        //        return false;

        //    receiver.affectedObjects = selectedObjects
        //        .Where(obj => obj != null && !(obj is Param_RemoteReceiver))
        //        .ToList();
        //    return true;
        //}

        public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
        {
            if (this.m_stateTags != null)
            {
                this.m_stateTags.TooltipSetup(point, e);
                if (e.Valid)
                {
                    return;
                }
            }
            base.SetupTooltip(point, e);
        }

        protected override void Layout()
        {
            base.Layout();

            float textWidth = (float)System.Math.Max(GH_FontServer.MeasureString(this.Owner.NickName, GH_FontServer.StandardBold).Width + 10, 50);
            System.Drawing.RectangleF bounds = new System.Drawing.RectangleF(this.Pivot.X - 0.5f * textWidth, this.Pivot.Y - 10f, textWidth, 20f);
            this.Bounds = bounds;
            this.Bounds = GH_Convert.ToRectangle(this.Bounds);

            this.m_textBounds = GH_Convert.ToRectangle(this.Bounds);

            // make space for the state tags, if any
            this.m_stateTags = this.Owner.StateTags;
            if (this.m_stateTags.Count == 0)
            {
                this.m_stateTags = null;
            }
            if (this.m_stateTags != null)
            {
                this.m_stateTags.Layout(GH_Convert.ToRectangle(this.Bounds), GH_StateTagLayoutDirection.Left);
                System.Drawing.Rectangle tag_box = this.m_stateTags.BoundingBox;
                if (!tag_box.IsEmpty)
                {
                    tag_box.Inflate(3, 0);
                    this.Bounds = System.Drawing.RectangleF.Union(this.Bounds, tag_box);
                }
            }

            // make space for the arrow
            if (Owner is MotionSender)
            {
                RectangleF arrowRect = new RectangleF(this.Bounds.Right, this.Bounds.Bottom, 10, 1);
                this.Bounds = RectangleF.Union(this.Bounds, arrowRect);
            }
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Viewport viewport = canvas.Viewport;
                RectangleF bounds = this.Bounds;
                if (!viewport.IsVisible(ref bounds, 10f)) return;
                this.Bounds = bounds;

                RenderCapsuleAndArrow(canvas, graphics, Bounds);
                RenderStateTagsIfNeeded(graphics);
                if ((MotionSenderSettings.IsSecondsInputMode()))
                {
                    DrawRangeLength(canvas, graphics);
                }
            }
        }
        private void RenderCapsuleAndArrow(GH_Canvas canvas, Graphics graphics, RectangleF bounds)
        {
            using (GH_Capsule capsule = GH_Capsule.CreateTextCapsule(bounds, m_textBounds, GH_Palette.Black, Owner.NickName))
            {
                capsule.AddInputGrip(this.InputGrip.Y);
                capsule.AddOutputGrip(this.OutputGrip.Y);
                bool hidden = (Owner as IGH_PreviewObject)?.Hidden ?? false;
                capsule.Render(graphics, Selected, Owner.Locked, hidden);
            }

            PointF arrowLocation = GetArrowLocation(bounds);
            if (arrowLocation != PointF.Empty)
            {
                renderArrow(canvas, graphics, arrowLocation);
            }
        }

        private PointF GetArrowLocation(RectangleF bounds)
        {
            if (Owner is MotionSender)
                return new PointF(bounds.Right - 10, this.OutputGrip.Y + 2);
            return PointF.Empty;
        }

        private void RenderStateTagsIfNeeded(Graphics graphics)
        {
            if (this.m_stateTags != null)
            {
                this.m_stateTags.RenderStateTags(graphics);
            }
        }
        // 添加一个新的方法来绘制区间长度
        private void DrawRangeLength(GH_Canvas canvas, Graphics graphics)
        {
            if (!this.Selected) return;
            var _sender = Owner as MotionSender;
            Interval _senderRange = new Interval(
                double.Parse(_sender.NickName.Split('-')[0]),
                double.Parse(_sender.NickName.Split('-')[1]));

            if (_senderRange == Interval.Unset || !_senderRange.IsValid) return;

            if (graphics == null) return;

            // 计算区间长度
            double length = Math.Abs(_senderRange.Max - _senderRange.Min + 1);
            string message = $"[{length}f | {Math.Round(length / MotionSenderSettings.FramesPerSecond, 2)}s]";

            PointF location = new PointF(
                this.Bounds.Left - 80,
                this.Bounds.Top + 5
            );
            var labelFont = new Font(GH_FontServer.StandardBold.FontFamily, 8);
            // 计算文本大小
            SizeF textSize = GH_FontServer.MeasureString(message, GH_FontServer.Standard);
            RectangleF textBounds = new RectangleF(location, textSize);
            textBounds.Inflate(6, 3);  // 添加一些内边距

            using (var brush = new SolidBrush(Color.DeepSkyBlue))
            {
                graphics.DrawString(message, labelFont, brush, textBounds);
            }
        }

        private void renderArrow(GH_Canvas canvas, Graphics graphics, PointF loc)
        {
            Color arrowColor = Color.LightSkyBlue;

            //if (Owner is Param_RemoteReceiver)
            //{
            //    arrowColor = Color.Orange;
            //}
            if (Owner is MotionSender)
            {
                arrowColor = Color.LightSkyBlue;
            }

            GH_GraphicsUtil.RenderCenteredText(graphics, "\u27aa", new Font("Arial", 10F), arrowColor, new PointF(loc.X, loc.Y));
        }
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            System.Drawing.Point point = GH_Convert.ToPoint(e.CanvasLocation);
            if (e.Button != 0)
            {
                return base.RespondToMouseMove(sender, e);
            }
            RectangleF unionButtonBounds = RectangleF.Union(HideButtonBounds, LockButtonBounds);
            if (unionButtonBounds.Contains(point))
            {
                if (!mouseOver)
                {
                    mouseOver = true;
                    sender.Invalidate();
                }
                return GH_ObjectResponse.Capture;
            }
            if (mouseOver)
            {
                mouseOver = false;
                sender.Invalidate();
            }
            return GH_ObjectResponse.Release;
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var ghDoc = Owner.OnPingDocument();
            if (ghDoc == null) return GH_ObjectResponse.Ignore;

            if (Owner is MotionSender senderParam)
            {
                var eventComp = new EventComponent();
                eventComp.CreateAttributes();

                var pivot = Owner.Attributes.Pivot;
                eventComp.Attributes.Pivot = new PointF(pivot.X + 300, pivot.Y + 10);

                ghDoc.AddObject(eventComp, false);

                var timeParam = eventComp.Params.Input[0];
                timeParam.AddSource(senderParam);
                timeParam.WireDisplay = GH_ParamWireDisplay.hidden;

                eventComp.LinkToSender(senderParam);


                ghDoc.ScheduleSolution(5, doc =>
                {
                    switch (MotionSenderSettings.DoubleClickGraphType)
                    { 
                        case "Graph Mapper":
                            var graphMapperGuid = new Guid("bc984576-7aa6-491f-a91d-e444c33675a7");
                            var graphMapper = Grasshopper.Instances.ComponentServer.EmitObject(graphMapperGuid) as GH_GraphMapper;
                            if (graphMapper == null) return;

                            graphMapper.CreateAttributes();
                            graphMapper.Attributes.Pivot = new PointF(
                                eventComp.Attributes.Pivot.X + 100,
                                eventComp.Attributes.Pivot.Y - 75
                            );

                            doc.AddObject(graphMapper, false);

                            graphMapper.AddSource(eventComp.Params.Output[0]);

                            var bezierGraph = Grasshopper.Instances.ComponentServer.EmitGraph(new GH_BezierGraph().GraphTypeID);
                            if (bezierGraph != null)
                            {
                                bezierGraph.PrepareForUse();
                                var container = graphMapper.Container;
                                graphMapper.Container = null;

                                if (container == null)
                                {
                                    container = new GH_GraphContainer(bezierGraph);
                                }
                                else
                                {
                                    container.Graph = bezierGraph;
                                }

                                container.X0 = 0;
                                container.X1 = 1;
                                container.Y0 = 0;
                                container.Y1 = 1;

                                graphMapper.Container = container;
                            }

                            graphMapper.WireDisplay = GH_ParamWireDisplay.faint;

                            senderParam.Attributes.Selected = false;
                            graphMapper.Attributes.Selected = true;
                            eventComp.Attributes.Selected = true;

                            doc.ScheduleSolution(10, d =>
                            {
                                graphMapper.ExpireSolution(true);
                                eventComp.ExpireSolution(true);
                            });
                            break;
                        case "V-Ray Graph":
                            var vrayGraphGuid = new Guid("6b30c365-2690-4d61-b2ca-8ec5f2118665");
                            var vrayGraph = Grasshopper.Instances.ComponentServer.EmitObject(vrayGraphGuid) as GH_Component;
                            if (vrayGraph == null) return;

                            vrayGraph.CreateAttributes();
                            vrayGraph.Attributes.Pivot = new PointF(
                                eventComp.Attributes.Pivot.X + 100,
                                eventComp.Attributes.Pivot.Y - 57
                            );

                            doc.AddObject(vrayGraph, false);

                            vrayGraph.Params.Input[0].AddSource(eventComp.Params.Output[0]);

                            vrayGraph.Params.Input[0].WireDisplay = GH_ParamWireDisplay.faint;

                            senderParam.Attributes.Selected = false;
                            vrayGraph.Attributes.Selected = true;
                            eventComp.Attributes.Selected = true;

                            doc.ScheduleSolution(10, d =>
                            {
                                vrayGraph.ExpireSolution(true);
                                eventComp.ExpireSolution(true);
                            });
                            break;
                        case "Graph-Mapper +":
                            var graphMapperPlusGuid = new Guid("310f9597-267e-4471-a7d7-048725557528");
                            var graphMapperPlus = Grasshopper.Instances.ComponentServer.EmitObject(graphMapperPlusGuid) as GH_Component;
                            if (graphMapperPlus == null) return;

                            graphMapperPlus.CreateAttributes();
                            graphMapperPlus.Attributes.Pivot = new PointF(
                                eventComp.Attributes.Pivot.X + 200,
                                eventComp.Attributes.Pivot.Y - 25
                            );

                            doc.AddObject(graphMapperPlus, false);

                            graphMapperPlus.Params.Input[2].AddSource(eventComp.Params.Output[0]);

                            graphMapperPlus.Params.Input[0].WireDisplay = GH_ParamWireDisplay.faint;

                            senderParam.Attributes.Selected = false;
                            graphMapperPlus.Attributes.Selected = true;
                            eventComp.Attributes.Selected = true;

                            doc.ScheduleSolution(10, d =>
                            {
                                graphMapperPlus.ExpireSolution(true);
                                eventComp.ExpireSolution(true);
                            });
                            break;
                        case "Rich Graph Mapper":
                            var richGraphMapperGuid = new Guid("e2996e6c-e067-42fa-8f44-2192c6763262");
                            var richGraphMapper = Grasshopper.Instances.ComponentServer.EmitObject(richGraphMapperGuid) as GH_Component;
                            if (richGraphMapper == null) return;

                            richGraphMapper.CreateAttributes();
                            richGraphMapper.Attributes.Pivot = new PointF(
                                eventComp.Attributes.Pivot.X + 100,
                                eventComp.Attributes.Pivot.Y - 15
                            );

                            doc.AddObject(richGraphMapper, false);

                            richGraphMapper.Params.Input[0].AddSource(eventComp.Params.Output[0]);

                            richGraphMapper.Params.Input[0].WireDisplay = GH_ParamWireDisplay.faint;

                            senderParam.Attributes.Selected = false;
                            richGraphMapper.Attributes.Selected = true;
                            eventComp.Attributes.Selected = true;

                            doc.ScheduleSolution(10, d =>
                            {
                                richGraphMapper.ExpireSolution(true);
                                eventComp.ExpireSolution(true);
                            });
                            break;
                    }
                });

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseDoubleClick(sender, e);
        }

        //private void ShowTemporaryMessage(GH_Canvas canvas, string message)
        //{
        //    GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
        //    canvasRepaint = (sender) =>
        //    {
        //        Graphics g = canvas.Graphics;
        //        if (g == null) return;

        //        // 保存当前的变换矩阵
        //        var originalTransform = g.Transform;

        //        // 重置变换，确保文字大小不受画布缩放影响
        //        g.ResetTransform();

        //        // 计算文本大小
        //        SizeF textSize = new SizeF(30, 30);

        //        // 设置消息位置在画布顶部居中
        //        float padding = 20;
        //        float x = textSize.Width + 300;
        //        float y = padding + 30;

        //        RectangleF textBounds = new RectangleF(x, y, textSize.Width + 300, textSize.Height + 30);
        //        textBounds.Inflate(6, 3);  // 添加一些内边距

        //        // 绘制消息
        //        GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
        //            textBounds,
        //            textBounds,
        //            GH_Palette.Pink,
        //            message);

        //        capsule.Render(g, Color.LightSkyBlue);
        //        capsule.Dispose();

        //        // 恢复原始变换
        //        g.Transform = originalTransform;
        //    };

        //    // 添加临时事件处理器
        //    canvas.CanvasPostPaintObjects += canvasRepaint;

        //    // 立即刷新画布以显示消息
        //    canvas.Refresh();

        //    // 设置定时器移除事件处理器
        //    Timer timer = new Timer();
        //    timer.Interval = 1500;
        //    timer.Tick += (sender, e) =>
        //    {
        //        canvas.CanvasPostPaintObjects -= canvasRepaint;
        //        canvas.Refresh();
        //        timer.Stop();
        //        timer.Dispose();
        //    };
        //    timer.Start();
        //}
    }
}