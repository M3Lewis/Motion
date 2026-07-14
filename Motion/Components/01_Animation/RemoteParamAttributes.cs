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
using Motion.General;


namespace Motion.Animation
{
    public class RemoteParamAttributes : GH_FloatingParamAttributes
    {
        private Rectangle m_textBounds; //maintain a rectangle of the text bounds
        private GH_StateTagList m_stateTags;

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

            float textWidth =
                (float)System.Math.Max(
                    GH_FontServer.MeasureString(this.Owner.NickName, GH_FontServer.StandardBold).Width + 10, 50);
            System.Drawing.RectangleF bounds =
                new System.Drawing.RectangleF(this.Pivot.X - 0.5f * textWidth, this.Pivot.Y - 10f, textWidth, 20f);
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
            using (GH_Capsule capsule =
                   GH_Capsule.CreateTextCapsule(bounds, m_textBounds, GH_Palette.Black, Owner.NickName))
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
            Interval _senderRange = Interval.Unset;
            if (MotilityUtils.TryParseNickNameInterval(_sender.NickName, out double min, out double max))
            {
                _senderRange = new Interval(min, max);
            }

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
            textBounds.Inflate(6, 3); // 添加一些内边距

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

            GH_GraphicsUtil.RenderCenteredText(graphics, "\u27aa", new Font("Arial", 10F), arrowColor,
                new PointF(loc.X, loc.Y));
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var ghDoc = Owner.OnPingDocument();
            if (ghDoc == null) return GH_ObjectResponse.Ignore;

            if (Owner is MotionSender senderParam)
            {
                var pivot = new PointF(Owner.Attributes.Pivot.X + 300, Owner.Attributes.Pivot.Y + 10);
                var (eventComp, graphComp) = EventGraphFactory.CreateEventWithGraph(ghDoc, senderParam, pivot);

                senderParam.Attributes.Selected = false;
                eventComp.Attributes.Selected = true;
                if (graphComp != null) graphComp.Attributes.Selected = true;

                ghDoc.ScheduleSolution(10, d =>
                {
                    eventComp.ExpireSolution(true);
                    graphComp?.ExpireSolution(true);
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