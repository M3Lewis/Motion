using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Motion.Motility
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

        private bool UpdateAffectedObjects(GH_Canvas sender, Param_RemoteReceiver receiver)
        {
            var selectedObjects = sender.Document.SelectedObjects()?.ToList() ?? new List<IGH_DocumentObject>();
            if (!selectedObjects.Any())
                return false;

            receiver.affectedObjects = selectedObjects
                .Where(obj => obj != null && !(obj is Param_RemoteReceiver))
                .ToList();
            return true;
        }

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
            if (Owner is Param_RemoteSender)
            {
                RectangleF arrowRect = new RectangleF(this.Bounds.Right, this.Bounds.Bottom, 10, 1);
                this.Bounds = RectangleF.Union(this.Bounds, arrowRect);
            }
            if (Owner is Param_RemoteReceiver)
            {
                RectangleF arrowRect = new RectangleF(this.Bounds.Left - 15, this.Bounds.Bottom, 15, 1);
                this.Bounds = RectangleF.Union(this.Bounds, arrowRect);
            }

            if (Owner is Param_RemoteReceiver)
            {
                float buttonHeight = 20.0f;
                float spacing = 1f;

                HideButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + spacing,
                    Bounds.Width,
                    buttonHeight);
                HideButtonBounds.Inflate(-1.0f, -1.0f);

                LockButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + buttonHeight + spacing,
                    Bounds.Width,
                    buttonHeight);
                LockButtonBounds.Inflate(-1.0f, -1.0f);

                DataButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + (buttonHeight) * 2 + spacing,
                    Bounds.Width,
                    buttonHeight);
                DataButtonBounds.Inflate(-1.0f, -1.0f);

                CollapseButtonBounds = new RectangleF(
                    Bounds.Right - 14,
                    Bounds.Y,
                    13,
                    13);

                if (!IsCollapsed)
                {
                    var buttonArea = RectangleF.Union(HideButtonBounds, LockButtonBounds);
                    buttonArea = RectangleF.Union(buttonArea, DataButtonBounds);
                    buttonArea.Inflate(2.0f, 2.0f);
                    Bounds = RectangleF.Union(Bounds, buttonArea);
                }
                else
                {
                    return;
                }
            }
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var remoteReceiver = Owner as Param_RemoteReceiver;

                if (mouseOver && remoteReceiver?.affectedObjects != null && remoteReceiver.affectedObjects.Any())
                {
                    Color boundaryColor;

                    Color orange = Color.Orange;
                    Color dodgerBlue = Color.DodgerBlue;
                    Color limeGreen = Color.LimeGreen;

                    Color orangeWithLessAlpha = Color.FromArgb(180, orange.R, orange.G, orange.B);
                    Color dodgerBlueWithLessAlpha = Color.FromArgb(180, dodgerBlue.R, dodgerBlue.G, dodgerBlue.B);
                    Color limeGreenWithLessAlpha = Color.FromArgb(180, limeGreen.R, limeGreen.G, limeGreen.B);

                    if (remoteReceiver.HideWhenEmpty && remoteReceiver.LockWhenEmpty)
                    {
                        boundaryColor = orangeWithLessAlpha;
                    }
                    else if (remoteReceiver.HideWhenEmpty)
                    {
                        boundaryColor = dodgerBlueWithLessAlpha;
                    }
                    else if (remoteReceiver.LockWhenEmpty)
                    {
                        boundaryColor = limeGreenWithLessAlpha;
                    }
                    else
                    {
                        boundaryColor = Color.Transparent;
                    }

                    foreach (var obj in remoteReceiver.affectedObjects)
                    {
                        if (obj?.Attributes != null)
                        {
                            var objBounds = obj.Attributes.Bounds;
                            objBounds.Inflate(5f, 5f);
                            DrawBoundary(graphics, objBounds, boundaryColor);
                        }
                    }
                }

                GH_Viewport viewport = canvas.Viewport;
                RectangleF bounds = this.Bounds;
                if (!viewport.IsVisible(ref bounds, 10f)) return;
                this.Bounds = bounds;

                RenderCapsuleAndArrow(canvas, graphics, Bounds);
                RenderStateTagsIfNeeded(graphics);

                if (remoteReceiver != null)
                {
                    graphics.DrawString(
                        IsCollapsed ? "▾" : "▴",
                        GH_FontServer.Standard,
                        Brushes.LightSkyBlue,
                        CollapseButtonBounds,
                        new StringFormat()
                        {
                            Alignment = StringAlignment.Far,
                            LineAlignment = StringAlignment.Far
                        });

                    if (!IsCollapsed)
                    {
                        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(HideButtonBounds,
                            remoteReceiver.HideWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
                        {
                            capsule.Render(graphics, Selected, Owner.Locked, false);
                            graphics.DrawString(
                                "Hide",
                                GH_FontServer.StandardBold,
                                Brushes.White,
                                HideButtonBounds,
                                new StringFormat()
                                {
                                    Alignment = StringAlignment.Center,
                                    LineAlignment = StringAlignment.Center
                                });
                        }

                        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(LockButtonBounds,
                            remoteReceiver.LockWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
                        {
                            capsule.Render(graphics, Selected, Owner.Locked, false);
                            graphics.DrawString(
                                "Lock",
                                GH_FontServer.StandardBold,
                                Brushes.White,
                                LockButtonBounds,
                                new StringFormat()
                                {
                                    Alignment = StringAlignment.Center,
                                    LineAlignment = StringAlignment.Center
                                });
                        }

                        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(DataButtonBounds, GH_Palette.Black))
                        {
                            capsule.Render(graphics, Selected, Owner.Locked, false);
                            graphics.DrawString(
                                "Data",
                                GH_FontServer.StandardBold,
                                Brushes.White,
                                DataButtonBounds,
                                new StringFormat()
                                {
                                    Alignment = StringAlignment.Center,
                                    LineAlignment = StringAlignment.Center
                                });
                        }
                    }
                }

                //string label = "";
                //if (Owner is Param_RemoteLocation)
                //{
                //    label = "L";
                //}
                //else if (Owner is Param_RemoteTarget)
                //{
                //    label = "T";
                //}

                //if (!string.IsNullOrEmpty(label))
                //{
                //    var labelFont = new Font(GH_FontServer.StandardBold.FontFamily, 7);
                //    var labelBounds = new RectangleF(
                //        Bounds.Left - 12,
                //        Bounds.Top + (Bounds.Height - labelFont.Height) / 2,
                //        15,
                //        labelFont.Height
                //    );

                //    graphics.DrawString(label, labelFont, Brushes.DarkGray, labelBounds);
                //}
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
            if (Owner is Param_RemoteReceiver)
            {
                if (IsCollapsed)
                {
                    return new PointF(bounds.Left + 9, bounds.Bottom - 10);
                }
                else
                {
                    return new PointF(bounds.Left + 10, this.OutputGrip.Y - 30);
                }
            }
            if (Owner is Param_RemoteSender)
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

        private void DrawBoundary(Graphics graphics, RectangleF bounds, Color color)
        {
            using (var pen = new Pen(color, 2f))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                pen.Width = 2;
                graphics.DrawRectangle(pen, bounds.X - 3, bounds.Y - 3, bounds.Width + 6, bounds.Height + 6);
            }
        }

        private void renderArrow(GH_Canvas canvas, Graphics graphics, PointF loc)
        {
            Color arrowColor = Color.LightSkyBlue;

            if (Owner is Param_RemoteReceiver)
            {
                arrowColor = Color.Orange;
            }
            else if (Owner is Param_RemoteSender)
            {
                arrowColor = Color.LightSkyBlue;
            }

            GH_GraphicsUtil.RenderCenteredText(graphics, "\u27aa", new Font("Arial", 10F), arrowColor, new PointF(loc.X, loc.Y));
        }
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            Point point = GH_Convert.ToPoint(e.CanvasLocation);
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

            if (Owner is Param_RemoteSender senderParam)
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

                    doc.ScheduleSolution(10, d => {
                        graphMapper.ExpireSolution(true);
                        eventComp.ExpireSolution(true);
                    });
                });

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseDoubleClick(sender, e);
        }

        
    }
}