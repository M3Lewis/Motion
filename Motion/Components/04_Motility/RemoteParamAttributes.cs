using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Motility
{
    // This class overrides the default display behavior of the params, to get the blue capsule
    // appearance and the little "Arrow" icons on the comp.

    public class RemoteParamAttributes : GH_FloatingParamAttributes
    {

        private Rectangle m_textBounds; //maintain a rectangle of the text bounds
        private GH_StateTagList m_stateTags; //state tags are like flatten/graft etc.

        // 添加锁定按钮相关字段
        private RectangleF HideButtonBounds;
        private RectangleF LockButtonBounds;
        private bool HideButtonDown;
        private bool LockButtonDown;
        private readonly int ButtonWidth = 18;
        private readonly int ButtonHeight = 18;
        private readonly int ButtonSpacing = 4;

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
        // 添加 Data 按钮相关字段
        private RectangleF DataButtonBounds;
        private bool DataButtonDown;

        // 添加折叠按钮相关字段
        private RectangleF CollapseButtonBounds;
        private bool CollapseButtonDown;
        private bool IsCollapsed = false;

        //handles state tag tooltips
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


        //This method figures out the size and shape of elements.
        protected override void Layout()
        {
            base.Layout();
            
            //establish the size based on the text content
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

                // 添加 Data 按钮布局
                DataButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + (buttonHeight) * 2 + spacing,
                    Bounds.Width,
                    buttonHeight);
                DataButtonBounds.Inflate(-1.0f, -1.0f);

                // 添加折叠按钮布局（在右上角）
                CollapseButtonBounds = new RectangleF(
                    Bounds.Right - 14,
                    Bounds.Y,
                    13,
                    13);

                if (!IsCollapsed)
                {
                    // 扩展边界以包含所有按钮
                    var buttonArea = RectangleF.Union(HideButtonBounds, LockButtonBounds);
                    buttonArea = RectangleF.Union(buttonArea, DataButtonBounds);
                    buttonArea.Inflate(2.0f,2.0f);
                    Bounds = RectangleF.Union(Bounds, buttonArea);
                }
                else
                {
                    // 折叠状态下不需要为按钮预留空间
                    return;
                }
            }
        }

        //empty constructor 
        public RemoteParamAttributes(Param_GenericObject owner)
            : base(owner)
        {

        }

        //This method actually draws the parts
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var receiver = Owner as Param_RemoteReceiver;
                
                // 只在 receiver 被选中时绘制范围框
                if (Selected && receiver != null && receiver.affectedObjects != null && receiver.affectedObjects.Any())
                {
                    // 根据状态决定边框颜色
                    Color boundaryColor;
                    if (receiver._hideWhenEmpty && receiver._lockWhenEmpty)
                    {
                        boundaryColor = Color.Orange;  // 两个功能都开启时显示橙色
                    }
                    else if (receiver._hideWhenEmpty)
                    {
                        boundaryColor = Color.DodgerBlue;  // 只开启 Hide 时显示蓝色
                    }
                    else if (receiver._lockWhenEmpty)
                    {
                        boundaryColor = Color.LimeGreen;  // 只开启 Lock 时显示绿色
                    }
                    else
                    {
                        boundaryColor = Color.DodgerBlue;  // 默认颜色
                    }

                    foreach (var obj in receiver.affectedObjects)
                    {
                        if (obj?.Attributes != null)
                        {
                            var objBounds = obj.Attributes.Bounds;
                            objBounds.Inflate(5f, 5f);
                            DrawBoundary(graphics, objBounds, boundaryColor);
                        }
                    }
                }

                // 如果不是对象通道，直接返回
                if (channel != GH_CanvasChannel.Objects) return;

                // 检查可见性
                GH_Viewport viewport = canvas.Viewport;
                RectangleF bounds = this.Bounds;
                if (!viewport.IsVisible(ref bounds, 10f)) return;
                this.Bounds = bounds;

                // 首先渲染主要的胶囊和箭头
                RenderCapsuleAndArrow(canvas, graphics, Bounds);
                RenderStateTagsIfNeeded(graphics);

                // 如果是 Receiver，渲染按钮
                if (receiver != null)
                {
                    // 渲染折叠按钮
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

                    // 只在未折叠时渲染其他按钮
                    if (!IsCollapsed)
                    {
                        // Hide 按钮
                        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(HideButtonBounds, 
                            receiver._hideWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
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

                        // Lock 按钮
                        using (GH_Capsule capsule = GH_Capsule.CreateCapsule(LockButtonBounds,
                            receiver._lockWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
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

                        // Data 按钮
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
            }
        }

        private void RenderCapsuleAndArrow(GH_Canvas canvas, Graphics graphics, RectangleF bounds)
        {
            // 渲染主要capsule
            using (GH_Capsule capsule = GH_Capsule.CreateTextCapsule(bounds, m_textBounds, GH_Palette.Black, Owner.NickName))
            {
                capsule.AddInputGrip(this.InputGrip.Y);
                capsule.AddOutputGrip(this.OutputGrip.Y);
                bool hidden = (Owner as IGH_PreviewObject)?.Hidden ?? false;
                capsule.Render(graphics, Selected, Owner.Locked, hidden);
            }

            // 渲染箭头
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
                // 根据折叠状态调整箭头位置
                if (IsCollapsed)
                {
                    // 折叠状态下，箭头位置在组件底部中间
                    return new PointF(bounds.Left + 9, bounds.Bottom-10);
                }
                else
                {
                    // 展开状态下，保持原有位置
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
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                pen.DashPattern = new float[] { 5, 5 };  // 设置虚线样式
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        //Draws the arrow as a wingding text. Using text means the icon can be vector and look good zoomed in.
        private static void renderArrow(GH_Canvas canvas, Graphics graphics, PointF loc)
        {
            GH_GraphicsUtil.RenderCenteredText(graphics, "\u27aa", new Font("Arial", 10F), Color.LightSkyBlue, new PointF(loc.X, loc.Y));
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var ghDoc = Owner.OnPingDocument();
            if (ghDoc == null) return GH_ObjectResponse.Ignore;

            if (Owner is Param_RemoteData dataParam)
            {
                // 查找同一位置 Receiver
                var receivers = ghDoc.Objects
                    .OfType<Param_RemoteReceiver>()
                    .Where(r => Math.Abs(r.Attributes.Pivot.Y - dataParam.Attributes.Pivot.Y) < 10)
                    .ToList();

                if (receivers.Any())
                {
                    var originalReceiver = receivers.First();
                    // 重新建立连接
                    dataParam.LinkToReceiver(originalReceiver);
                    // 强制更新
                    dataParam.ExpireSolution(true);
                    dataParam.OnDisplayExpired(true);
                    ghDoc.ScheduleSolution(5);
                }
            }
            return GH_ObjectResponse.Handled;
        }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var ghDoc = Owner.OnPingDocument();
            if (ghDoc == null) return GH_ObjectResponse.Ignore;

            if (Owner is Param_RemoteSender senderParam)
            {
                // 创建 Receiver
                var newReceiver = new Param_RemoteReceiver();
                
                // 重要：使用 true 作为第二个参数，表示立即记录到画布历史并刷新
                ghDoc.AddObject(newReceiver, true, ghDoc.ObjectCount);
                
                // 设置 NickName
                newReceiver.NickName = senderParam.NickName;
                
                // 设置位置
                newReceiver.Attributes.Pivot = new PointF(Pivot.X + 100, Pivot.Y);
                
                // 确保布局更新
                newReceiver.Attributes.ExpireLayout();
                
                // 建立连接
                newReceiver.AddSource(Owner);
                
                // 强制刷新画布
                Grasshopper.Instances.ActiveCanvas.Refresh();
            }
            
            return GH_ObjectResponse.Handled;
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is Param_RemoteReceiver)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (CollapseButtonBounds.Contains(e.CanvasLocation))
                    {
                        CollapseButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                    if (HideButtonBounds.Contains(e.CanvasLocation))
                    {
                        HideButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                    if (LockButtonBounds.Contains(e.CanvasLocation))
                    {
                        LockButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                    if (DataButtonBounds.Contains(e.CanvasLocation))
                    {
                        DataButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (!(Owner is Param_RemoteReceiver receiver)) 
                return base.RespondToMouseUp(sender, e);

            // 处理折叠按钮
            if (CollapseButtonDown)
            {
                return HandleCollapseButton(sender, e);
            }

            // 处理 Hide 按钮
            if (HideButtonDown)
            {
                return HandleHideButton(sender, e, receiver);
            }

            // 处理 Lock 按钮
            if (LockButtonDown)
            {
                return HandleLockButton(sender, e, receiver);
            }

            // 处理 Data 按钮
            if (DataButtonDown)
            {
                return HandleDataButton(sender, e, receiver);
            }

            return base.RespondToMouseUp(sender, e);
        }

        private GH_ObjectResponse HandleCollapseButton(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            CollapseButtonDown = false;
            if (!CollapseButtonBounds.Contains(e.CanvasLocation))
                return GH_ObjectResponse.Release;

            IsCollapsed = !IsCollapsed;
            ExpireLayout();
            sender.Refresh();
            return GH_ObjectResponse.Release;
        }

        private GH_ObjectResponse HandleHideButton(GH_Canvas sender, GH_CanvasMouseEvent e, Param_RemoteReceiver receiver)
        {
            HideButtonDown = false;
            sender.Refresh();

            if (!HideButtonBounds.Contains(e.CanvasLocation))
                return GH_ObjectResponse.Release;

            UpdateAffectedObjects(sender, receiver);
            receiver._hideWhenEmpty = !receiver._hideWhenEmpty;
            receiver.UpdateGroupVisibilityAndLock();
            sender.Refresh();
            return GH_ObjectResponse.Release;
        }

        private GH_ObjectResponse HandleLockButton(GH_Canvas sender, GH_CanvasMouseEvent e, Param_RemoteReceiver receiver)
        {
            LockButtonDown = false;
            sender.Refresh();

            if (!LockButtonBounds.Contains(e.CanvasLocation))
                return GH_ObjectResponse.Release;

            if (!UpdateAffectedObjects(sender, receiver) && !receiver.affectedObjects.Any())
                return GH_ObjectResponse.Release;

            receiver._lockWhenEmpty = !receiver._lockWhenEmpty;
            receiver.UpdateGroupVisibilityAndLock();
            sender.Refresh();
            return GH_ObjectResponse.Release;
        }

        private GH_ObjectResponse HandleDataButton(GH_Canvas sender, GH_CanvasMouseEvent e, Param_RemoteReceiver receiver)
        {
            DataButtonDown = false;
            sender.Refresh();

            if (!DataButtonBounds.Contains(e.CanvasLocation))
                return GH_ObjectResponse.Release;

            var ghDoc = Owner.OnPingDocument();
            if (ghDoc == null)
                return GH_ObjectResponse.Release;

            CreateDataParams(ghDoc, receiver);
            return GH_ObjectResponse.Release;
        }


        private void CreateDataParams(GH_Document ghDoc, Param_RemoteReceiver receiver)
        {
            for (int i = 0; i < 2; i++)
            {
                var dataParam = new Param_RemoteData();
                ghDoc.AddObject(dataParam, true, ghDoc.ObjectCount);
                dataParam.LinkToReceiver(receiver);
                
                dataParam.Attributes.Pivot = new PointF(
                    Pivot.X, 
                    Pivot.Y + (i + 1) * 100
                );
                dataParam.Attributes.ExpireLayout();

                AddToReceiverGroup(ghDoc, receiver, dataParam);
            }

            ghDoc.ScheduleSolution(5);
            Grasshopper.Instances.ActiveCanvas.Refresh();
        }

        private void AddToReceiverGroup(GH_Document ghDoc, Param_RemoteReceiver receiver, Param_RemoteData dataParam)
        {
            var group = ghDoc.Objects
                .OfType<GH_Group>()
                .FirstOrDefault(g => g.ObjectIDs.Contains(receiver.InstanceGuid));
            
            if (group != null)
            {
                group.AddObject(dataParam.InstanceGuid);
            }
        }
    }
}
