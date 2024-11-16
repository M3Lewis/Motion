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

        private System.Drawing.Rectangle m_textBounds; //maintain a rectangle of the text bounds
        private GH_StateTagList m_stateTags; //state tags are like flatten/graft etc.

        // 添加锁定按钮相关字段
        private RectangleF HideButtonBounds;
        private RectangleF LockButtonBounds;
        private bool HideButtonDown;
        private bool LockButtonDown;
        private readonly int ButtonWidth = 18;
        private readonly int ButtonHeight = 18;
        private readonly int ButtonSpacing = 4;

        // 添加记忆列表
        private List<IGH_DocumentObject> hiddenObjects = new List<IGH_DocumentObject>();
        private List<IGH_DocumentObject> lockedObjects = new List<IGH_DocumentObject>();

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
                // 减小按钮间距
                float buttonHeight = 20.0f;
                float spacing = 0.5f;  // 减小间距
                
                HideButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + spacing,
                    Bounds.Width,
                    buttonHeight);
                HideButtonBounds.Inflate(-1.0f, -1.0f);

                LockButtonBounds = new RectangleF(
                    Bounds.X,
                    Bounds.Bottom + buttonHeight + spacing,  // 减小间距
                    Bounds.Width,
                    buttonHeight);
                LockButtonBounds.Inflate(-1.0f, -1.0f);

                // 扩展边界以包含按钮
                var buttonArea = RectangleF.Union(HideButtonBounds, LockButtonBounds);
                buttonArea.Inflate(2.0f, 2.0f);
                Bounds = RectangleF.Union(Bounds, buttonArea);
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
            // 确保基类的渲染被正确调用
            base.Render(canvas, graphics, channel);

            // 如果是连线通道且有源，渲染连线
            if (channel == GH_CanvasChannel.Wires && Owner.SourceCount > 0)
            {
                base.RenderIncomingWires(canvas.Painter, base.Owner.Sources, base.Owner.WireDisplay);
                return;
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

            // 如果是 Receiver，渲染状态文本和范围框
            if (Owner is RemoteParam remoteParam)
            {
                RenderStatusText(graphics, Bounds, remoteParam);
                RenderAffectedComponentBoundsIfSelected(canvas, graphics);
            }

            // 如果是 Receiver，渲染按钮
            if (Owner is Param_RemoteReceiver)
            {
                // 检查记忆列表中的状态，添加防御性检查
                bool anyHidden = false;
                bool anyLocked = false;

                if (hiddenObjects != null && hiddenObjects.Any())
                {
                    anyHidden = hiddenObjects
                        .Where(obj => obj != null)  // 过滤掉空对象
                        .Any(obj => obj is IGH_PreviewObject previewObj && previewObj.Hidden);
                }

                if (lockedObjects != null && lockedObjects.Any())
                {
                    anyLocked = lockedObjects
                        .Where(obj => obj != null)  // 过滤掉空对象
                        .Any(obj => obj is IGH_ActiveObject activeObj && activeObj.Locked);
                }

                // 绘制 Hide 按钮
                var hideButtonPalette = anyHidden ? GH_Palette.Blue : (HideButtonDown ? GH_Palette.Grey : GH_Palette.Black);
                using (GH_Capsule capsule = GH_Capsule.CreateCapsule(HideButtonBounds, hideButtonPalette))
                {
                    capsule.Render(graphics, Selected, Owner.Locked, false);
                    graphics.DrawString(
                        "Hide",
                        GH_FontServer.ConsoleSmall,
                        Brushes.Azure,
                        HideButtonBounds,
                        new StringFormat()
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        });
                }

                // 绘制 Lock 按钮
                var lockButtonPalette = anyLocked ? GH_Palette.Blue : (LockButtonDown ? GH_Palette.Grey : GH_Palette.Black);
                using (GH_Capsule capsule = GH_Capsule.CreateCapsule(LockButtonBounds, lockButtonPalette))
                {
                    capsule.Render(graphics, Selected, Owner.Locked, false);
                    graphics.DrawString(
                        "Lock",
                        GH_FontServer.ConsoleSmall,
                        Brushes.Azure,
                        LockButtonBounds,
                        new StringFormat()
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        });
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
                return new PointF(bounds.Left + 10, this.OutputGrip.Y -20);
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

        private void RenderStatusText(Graphics graphics, RectangleF bounds, RemoteParam remoteParam)
        {
            string statusText = GetStatusText(remoteParam);
            if (string.IsNullOrEmpty(statusText)) return;

            // 使用粗体字体
            var font = new Font("Arial", 8, FontStyle.Bold);
            var textSize = graphics.MeasureString(statusText, font);
            
            // 计算文字位置：在 Receiver 的正上方居中
            var textLocation = new PointF(
                bounds.Left + (bounds.Width - textSize.Width) / 2,  // 水平居中
                bounds.Top - textSize.Height - 2  // 在 Receiver 上方
            );

            // 绘制文字
            Color textColor = GetBoundaryColor(remoteParam);
            using (var brush = new SolidBrush(textColor))
            {
                graphics.DrawString(statusText, font, brush, textLocation);
            }
        }

        /*
        private void HandleScribbles(GH_Canvas canvas)
        {
            var doc = Owner.OnPingDocument();
            if (doc == null) return;

            // 查找包含当前组件的组
            var containingGroups = doc.Objects
                .OfType<GH_Group>()
                .Where(g => g.ObjectIDs.Contains(Owner.InstanceGuid))
                .ToList();

            foreach (var group in containingGroups)
            {
                UpdateScribble(doc, group);
            }
        }
        */
        private void RenderAffectedComponentBoundsIfSelected(GH_Canvas canvas, Graphics graphics)
        {
            // 只在 Receiver 被选中时显示范围框
            if (!(Owner is RemoteParam remoteParam) || !this.Selected) return;

            var doc = Owner.OnPingDocument();
            if (doc == null) return;

            // 创建已处理对象的集合，避免重复绘制
            HashSet<IGH_DocumentObject> processedObjects = new HashSet<IGH_DocumentObject>();

            // 查找同时被隐藏和锁定的对象
            if (hiddenObjects != null && lockedObjects != null)
            {
                var bothAffectedObjects = hiddenObjects.Intersect(lockedObjects)
                    .Where(obj => obj != null 
                        && obj is IGH_PreviewObject previewObj 
                        && obj is IGH_ActiveObject activeObj
                        && previewObj.Hidden 
                        && activeObj.Locked);

                foreach (var obj in bothAffectedObjects)
                {
                    if (processedObjects.Add(obj))  // 如果对象还未处理
                    {
                        var bounds = obj.Attributes.Bounds;
                        bounds.Inflate(5, 5);
                        DrawBoundary(graphics, bounds, Color.DarkOrange);  // 使用橙色表示同时被隐藏和锁定
                    }
                }
            }

            // 绘制仅被隐藏的组件边界
            if (hiddenObjects != null)
            {
                foreach (var obj in hiddenObjects.Where(o => o != null))
                {
                    if (processedObjects.Contains(obj)) continue;  // 跳过已处理的对象

                    if (obj is IGH_PreviewObject previewObj && previewObj.Hidden)
                    {
                        var bounds = obj.Attributes.Bounds;
                        bounds.Inflate(5, 5);
                        DrawBoundary(graphics, bounds, Color.DodgerBlue);
                        processedObjects.Add(obj);
                    }
                }
            }

            // 绘制仅被锁定的组件边界
            if (lockedObjects != null)
            {
                foreach (var obj in lockedObjects.Where(o => o != null))
                {
                    if (processedObjects.Contains(obj)) continue;  // 跳过已处理的对象

                    if (obj is IGH_ActiveObject activeObj && activeObj.Locked)
                    {
                        var bounds = obj.Attributes.Bounds;
                        bounds.Inflate(5, 5);
                        DrawBoundary(graphics, bounds, Color.ForestGreen);
                        processedObjects.Add(obj);
                    }
                }
            }
        }

        /*
        private IEnumerable<IGH_DocumentObject> GetAffectedObjects(GH_Group group, RemoteParam remoteParam)
        {
            var doc = Owner.OnPingDocument();
            if (doc == null) return Enumerable.Empty<IGH_DocumentObject>();

            try
            {
                // 获取所有组件对象，不考虑其 hide/lock 状态
                var objects = group.ObjectIDs
                    .Select(id => doc.FindObject(id, false))
                    .Where(obj => obj != null && obj != Owner && obj is GH_Component)
                    .Cast<GH_Component>()
                    .ToList();

                if (!objects.Any()) return Enumerable.Empty<IGH_DocumentObject>();

                // 直接返回所有对象，不检查 hide/lock 状态
                return objects;
            }
            catch
            {
                return Enumerable.Empty<IGH_DocumentObject>();
            }
        }
        */

        /*
        private void UpdateScribble(GH_Document doc, GH_Group group)
        {
            // 查找组内所有的 Scribble
            var existingScribbles = doc.Objects
                .OfType<GH_Scribble>()
                .Where(s => group.ObjectIDs.Contains(s.InstanceGuid))
                .ToList();

            foreach (var scribble in existingScribbles)
            {
                // 检查 Scribble 文本是否包含下划线
                if (scribble.Text.Contains("_"))
                {
                    // 分割文本
                    var parts = scribble.Text.Split('_');
                    if (parts.Length > 1)
                    {
                        // 更新前半部分为当前 Receiver 的 NickName
                        string newText = $"{Owner.NickName}_{parts[1]}";
                        if (scribble.Text != newText)
                        {
                            scribble.Text = newText;
                           
                        }
                    }
                }
            }

            // 如果没有找到包含下划线的 Scribble，创建新的
            if (!existingScribbles.Any(s => s.Text.Contains("_")))
            {
                var scribble = new GH_Scribble();
                scribble.Text = $"{Owner.NickName}_";
                scribble.Font = new System.Drawing.Font("微软雅黑", 100, FontStyle.Bold);
                
                var groupBounds = group.Attributes.Bounds;
                

                doc.AddObject(scribble, false);
                scribble.Attributes.Pivot = new PointF(
                    groupBounds.Left + 10,
                    groupBounds.Top + 10
                );
                group.AddObject(scribble.InstanceGuid);
            }
        }
        */
        private void DrawBoundary(Graphics graphics, RectangleF bounds, Color color)
        {
            using (var pen = new Pen(color, 2f))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                pen.DashPattern = new float[] { 5, 5 };  // 设置虚线样式
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        /*
        private RectangleF CalculateAffectedBounds(IEnumerable<IGH_DocumentObject> objects)
        {
            var firstObject = objects.First();
            var bounds = firstObject.Attributes.Bounds;
            
            foreach (var obj in objects.Skip(1))
            {
                bounds = RectangleF.Union(bounds, obj.Attributes.Bounds);
            }

            // 扩大边界使其更容易看见
            bounds.Inflate(10, 10);
            return bounds;
        }
        */

        private string GetStatusText(RemoteParam remoteParam)
        {
            if (remoteParam._hideWhenEmpty && remoteParam._lockWhenEmpty)
                return "HIDE LOCK";
            if (remoteParam._hideWhenEmpty)
                return "HIDE";
            if (remoteParam._lockWhenEmpty)
                return "LOCK";
            return string.Empty;
        }

        private Color GetBoundaryColor(RemoteParam remoteParam)
        {
            if (remoteParam._hideWhenEmpty && remoteParam._lockWhenEmpty)
                return Color.DarkOrange;
            if (remoteParam._hideWhenEmpty)
                return Color.DodgerBlue;
            if (remoteParam._lockWhenEmpty)
                return Color.ForestGreen;
            return Color.DarkGray;
        }

        //Draws the arrow as a wingding text. Using text means the icon can be vector and look good zoomed in.
        private static void renderArrow(GH_Canvas canvas, Graphics graphics, PointF loc)
        {
            //Wingdings 3 has a nice arrow in the "g" char.

            //  Font font = new Font("Wingdings 3", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            //render the text at specified location
            //Version for everyone:
            GH_GraphicsUtil.RenderCenteredText(graphics, "\u27aa", new Font("Arial", 10F), Color.LightSkyBlue, new PointF(loc.X, loc.Y));
            //Version for Marc:
            // GH_GraphicsUtil.RenderCenteredText(graphics, "*", new Font("Arial", 10F), Color.Black, loc);
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
            else if (Owner is Param_RemoteReceiver receiver)
            {
                // 创建第一个 Data Param
                var firstDataParam = new Param_RemoteData();
                ghDoc.AddObject(firstDataParam, true, ghDoc.ObjectCount);
                
                // 链接到 Receiver
                firstDataParam.LinkToReceiver(receiver);
                
                // 放置在 Receiver 的下方
                firstDataParam.Attributes.Pivot = new PointF(Pivot.X, Pivot.Y + 100);
                firstDataParam.Attributes.ExpireLayout();

                // 创建第二个 Data Param
                var secondDataParam = new Param_RemoteData();
                ghDoc.AddObject(secondDataParam, true, ghDoc.ObjectCount);
                
                // 链接到 Receiver
                secondDataParam.LinkToReceiver(receiver);
                
                // 放置在第一个 Data Param 的下方
                secondDataParam.Attributes.Pivot = new PointF(Pivot.X, Pivot.Y + 200);
                secondDataParam.Attributes.ExpireLayout();

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
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is Param_RemoteReceiver)
            {
                if (HideButtonDown)
                {
                    HideButtonDown = false;
                    sender.Refresh();

                    if (HideButtonBounds.Contains(e.CanvasLocation))
                    {
                        // 如果有选中的对象，更新记忆列表
                        var selectedObjects = sender.Document.SelectedObjects()?.ToList() ?? new List<IGH_DocumentObject>();
                        if (selectedObjects.Any())
                        {
                            hiddenObjects = selectedObjects
                                .Where(obj => obj != null && !(obj is Param_RemoteReceiver))
                                .ToList();
                        }

                        // 操作记忆列表中的对象
                        if (hiddenObjects != null)
                        {
                            foreach (var obj in hiddenObjects.ToList())
                            {
                                if (obj != null && obj is IGH_PreviewObject previewObj)
                                {
                                    previewObj.Hidden = !previewObj.Hidden;
                                    obj.Attributes.Selected = false;
                                }
                            }
                        }
                        sender.Document.ScheduleSolution(5);
                        return GH_ObjectResponse.Release;
                    }
                }
                if (LockButtonDown)
                {
                    LockButtonDown = false;
                    sender.Refresh();

                    if (LockButtonBounds.Contains(e.CanvasLocation))
                    {
                        // 如果有选中的对象，更新记忆列表
                        var selectedObjects = sender.Document.SelectedObjects()?.ToList() ?? new List<IGH_DocumentObject>();
                        if (selectedObjects.Any())
                        {
                            lockedObjects = selectedObjects
                                .Where(obj => obj != null && !(obj is Param_RemoteReceiver))
                                .ToList();
                        }

                        // 操作记忆列表中的对象
                        if (lockedObjects != null)
                        {
                            foreach (var obj in lockedObjects.ToList())
                            {
                                if (obj != null && obj is IGH_ActiveObject activeObject)
                                {
                                    activeObject.Locked = !activeObject.Locked;
                                    obj.Attributes.Selected = false;
                                }
                            }
                        }
                        sender.Document.ScheduleSolution(5);
                        return GH_ObjectResponse.Release;
                    }
                }
            }
            return base.RespondToMouseUp(sender, e);
        }
    }
}
