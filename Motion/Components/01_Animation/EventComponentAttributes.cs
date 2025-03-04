using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using Motion.General;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class EventComponentAttributes : GH_ComponentAttributes
    {
        private bool mouseOver = false;
        private bool _isCollapsed = false;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            private set => _isCollapsed = value;
        }

        // 按钮相关字段
        private RectangleF hideButtonBounds;
        private RectangleF lockButtonBounds;

        public bool HideButtonDown;
        public bool LockButtonDown;

        private readonly int ButtonWidth = 18;
        private readonly int ButtonHeight = 18;
        private readonly int ButtonSpacing = 4;

        private readonly string EmptyModeText = "Empty Mode";

        public EventComponentAttributes(EventComponent owner) : base(owner)
        {
        }

        protected override void Layout()
        {
            base.Layout();

            var eventComponent = Owner as EventComponent;
            if (eventComponent == null) return;

            float buttonHeight = 20.0f;
            float spacing = 1f;

            // 只在未折叠时添加按钮布局
            if (!eventComponent.IsCollapsed)
            {
                hideButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + spacing,
                    Bounds.Width,
                    buttonHeight);
                hideButtonBounds.Inflate(-1.0f, -1.0f);

                lockButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + buttonHeight + spacing,
                    Bounds.Width,
                    buttonHeight);
                lockButtonBounds.Inflate(-1.0f, -1.0f);

                // 扩展边界以包含所有按钮
                var buttonArea = RectangleF.Union(hideButtonBounds, lockButtonBounds);
                buttonArea.Inflate(2.0f, 2.0f);
                Bounds = RectangleF.Union(Bounds, buttonArea);
            }
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var owner = Owner as EventComponent;
                if (owner == null) return;

                // 只在未折叠时渲染按钮
                if (!owner.IsCollapsed)
                {
                    // 如果开启了空值模式菜单，显示提示文字
                    if (owner.UseEmptyValueMode)
                    {
                        // 计算文字位置（在组件上方）
                        var textBounds = new RectangleF(
                            Bounds.X,
                            Bounds.Y - 20, // 在组件上方20个像素
                            Bounds.Width,
                            20
                        );

                        // 使用半透明的背景色
                        using (var brush = new SolidBrush(Color.FromArgb(80, Color.Black)))
                        {
                            graphics.FillRectangle(brush, textBounds);
                        }

                        // 绘制文字
                        graphics.DrawString(
                            EmptyModeText,
                            GH_FontServer.Standard,
                            Brushes.White,
                            textBounds,
                            new StringFormat()
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            }
                        );
                    }

                    // 只在鼠标悬停时绘制范围框
                    if (mouseOver && owner?.affectedObjects != null && owner.affectedObjects.Any())
                    {
                        // 根据状态决定边框颜色
                        Color boundaryColor;

                        // 获取原始颜色
                        Color orange = Color.Orange;
                        Color dodgerBlue = Color.DodgerBlue;
                        Color limeGreen = Color.LimeGreen;

                        // 创建 Alpha 值为一半的新颜色
                        Color orangeWithLessAlpha = Color.FromArgb(180, orange.R, orange.G, orange.B);
                        Color dodgerBlueWithLessAlpha = Color.FromArgb(180, dodgerBlue.R, dodgerBlue.G, dodgerBlue.B);
                        Color limeGreenWithLessAlpha = Color.FromArgb(180, limeGreen.R, limeGreen.G, limeGreen.B);

                        if (owner.HideWhenEmpty && owner.LockWhenEmpty)
                        {
                            boundaryColor = orangeWithLessAlpha;
                        }
                        else if (owner.HideWhenEmpty)
                        {
                            boundaryColor = dodgerBlueWithLessAlpha;
                        }
                        else if (owner.LockWhenEmpty)
                        {
                            boundaryColor = limeGreenWithLessAlpha;
                        }
                        else
                        {
                            boundaryColor = Color.Transparent;
                        }

                        // 绘制范围框
                        foreach (var obj in owner.affectedObjects)
                        {
                            if (obj?.Attributes != null)
                            {
                                var objBounds = obj.Attributes.Bounds;
                                objBounds.Inflate(5f, 5f);
                                DrawBoundary(graphics, objBounds, boundaryColor);
                                DrawGuideLine(graphics, Owner.Attributes.Bounds, objBounds, boundaryColor);
                            }
                        }
                    }

                    // Hide 按钮
                    using (GH_Capsule capsule = GH_Capsule.CreateCapsule(hideButtonBounds,
                        owner.HideWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
                    {
                        capsule.Render(graphics, Selected, Owner.Locked, false);
                        graphics.DrawString(
                            "Hide",
                            GH_FontServer.StandardBold,
                            Brushes.White,
                            hideButtonBounds,
                            new StringFormat()
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            });
                    }

                    // Lock 按钮
                    using (GH_Capsule capsule = GH_Capsule.CreateCapsule(lockButtonBounds,
                        owner.LockWhenEmpty ? GH_Palette.Blue : GH_Palette.Black))
                    {
                        capsule.Render(graphics, Selected, Owner.Locked, false);
                        graphics.DrawString(
                            "Lock",
                            GH_FontServer.StandardBold,
                            Brushes.White,
                            lockButtonBounds,
                            new StringFormat()
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            });
                    }
                }
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

        private void DrawGuideLine(Graphics graphics, RectangleF eventComponentBounds, RectangleF affectComponentBounds, Color color)
        {
            using (var pen = new Pen(color, 1f))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                pen.Width = 1;

                // 计算起点（事件组件的中心点）
                PointF startPoint = new PointF(
                    eventComponentBounds.Left + eventComponentBounds.Width / 2,
                    eventComponentBounds.Top + eventComponentBounds.Height / 2
                );


                // 计算终点（受影响组件的中心点）
                PointF endPoint = new PointF(
                    affectComponentBounds.Left + affectComponentBounds.Width / 2,
                    affectComponentBounds.Top + affectComponentBounds.Height / 2
                );

                // 绘制连接线
                graphics.DrawLine(pen, startPoint, endPoint);

                // 可选：添加一个小圆点在线的起点和终点
                float dotSize = 4f;
                graphics.FillEllipse(new SolidBrush(color),
                    startPoint.X - dotSize / 2,
                    startPoint.Y - dotSize / 2,
                    dotSize, dotSize);
                graphics.FillEllipse(new SolidBrush(color),
                    endPoint.X - dotSize / 2,
                    endPoint.Y - dotSize / 2,
                    dotSize, dotSize);
            }
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is EventComponent)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (hideButtonBounds.Contains(e.CanvasLocation))
                    {
                        HideButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                    if (lockButtonBounds.Contains(e.CanvasLocation))
                    {
                        LockButtonDown = true;
                        sender.Refresh();
                        return GH_ObjectResponse.Capture;
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private bool UpdateAffectedObjects(GH_Canvas sender, EventComponent eventComp)
        {
            var selectedObjects = sender.Document.SelectedObjects()?.ToList() ?? new List<IGH_DocumentObject>();
            if (!selectedObjects.Any())
                return false;

            eventComp.affectedObjects = selectedObjects
                .Where(obj => obj != null && !(obj is EventComponent))
                .ToList();
            return true;
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            Point point = GH_Convert.ToPoint(e.CanvasLocation);
            if (e.Button != 0)
            {
                return base.RespondToMouseMove(sender, e);
            }
            RectangleF unionButtonBounds = RectangleF.Union(hideButtonBounds, lockButtonBounds);
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

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var owner = Owner as EventComponent;
            if (owner == null)
                return base.RespondToMouseUp(sender, e);

            if (HideButtonDown)
            {
                HideButtonDown = false;
                sender.Refresh();

                if (!hideButtonBounds.Contains(e.CanvasLocation))
                    return GH_ObjectResponse.Release;

                UpdateAffectedObjects(sender, owner);
                owner.HideWhenEmpty = !owner.HideWhenEmpty;
                owner.UpdateGroupVisibilityAndLock();
                sender.Refresh();
                return GH_ObjectResponse.Release;
            }

            if (LockButtonDown)
            {
                LockButtonDown = false;
                sender.Refresh();

                if (!lockButtonBounds.Contains(e.CanvasLocation))
                    return GH_ObjectResponse.Release;

                if (!UpdateAffectedObjects(sender, owner) && !owner.affectedObjects.Any())
                    return GH_ObjectResponse.Release;

                owner.LockWhenEmpty = !owner.LockWhenEmpty;
                owner.UpdateGroupVisibilityAndLock();
                sender.Refresh();
                return GH_ObjectResponse.Release;
            }

            return base.RespondToMouseUp(sender, e);
        }

        // 添加双击事件处理
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                var owner = Owner as EventComponent;
                if (owner == null) return GH_ObjectResponse.Ignore;

                // 执行跳转到 EventOperation 的逻辑
                if (owner.Params.Output[0].Recipients.Count == 0) return GH_ObjectResponse.Ignore;

                foreach (var recipient in owner.Params.Output[0].Recipients)
                {
                    var topLevelObj = recipient.Attributes.GetTopLevel.DocObject;
                    IGH_DocumentObject eventOperation = null;
                    GH_Component mapperComp = null;
                    // 处理 GraphMapper 的情况
                    var graphMapper = topLevelObj as GH_GraphMapper;
                    if (graphMapper != null)
                    {
                        if (graphMapper.Recipients.Count > 0)
                        {
                            eventOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                            MotionGeneralMethods.GoComponent(eventOperation);
                            return GH_ObjectResponse.Handled;
                        }
                    }
                    // 处理 Component 的情况
                    else
                    {
                        var component = topLevelObj as GH_Component;
                        if (component == null) return GH_ObjectResponse.Ignore;

                        mapperComp = component;

                        if (mapperComp == null) return GH_ObjectResponse.Ignore;
                        eventOperation = mapperComp.Params.Output[0].Recipients[0].Attributes.GetTopLevel.DocObject;
                        MotionGeneralMethods.GoComponent(eventOperation);
                        return GH_ObjectResponse.Handled;
                    }
                }

            }
            return base.RespondToMouseDoubleClick(sender, e);
        }

        public void SetCollapsedState(bool state)
        {
            _isCollapsed = state;
            ExpireLayout();
        }
    }
}