using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Motion.Motility
{
    public class Motion_ValueListAttributes : GH_Attributes<Motion_ValueList>
    {
        public const int ItemHeight = 22;

        private const int ArrowRadius = 6;

        public override bool AllowMessageBalloon => false;

        public override bool HasInputGrip => true;

        public override bool HasOutputGrip => true;

        private RectangleF ItemBounds { get; set; }

        private RectangleF NameBounds { get; set; }

        public Motion_ValueListAttributes(Motion_ValueList owner)
            : base(owner)
        {
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {

                Motion_ValueListItem firstSelectedItem = base.Owner.FirstSelectedItem;
                if (firstSelectedItem == null || !firstSelectedItem.BoxRight.Contains(e.CanvasLocation))
                {
                    return GH_ObjectResponse.Ignore;
                }
                ToolStripDropDownMenu toolStripDropDownMenu = new ToolStripDropDownMenu();
                Motion_ValueListItem firstSelectedItem2 = base.Owner.FirstSelectedItem;
                foreach (Motion_ValueListItem listItem in base.Owner.ListItems)
                {
                    ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(listItem.Name);
                    toolStripMenuItem.Click += ValueMenuItem_Click;
                    if (listItem == firstSelectedItem2)
                    {
                        toolStripMenuItem.Checked = true;
                    }
                    toolStripMenuItem.Tag = listItem;
                    toolStripDropDownMenu.Items.Add(toolStripMenuItem);
                }
                toolStripDropDownMenu.Show(sender, e.ControlLocation);
                return GH_ObjectResponse.Handled;



            }
            return base.RespondToMouseDown(sender, e);
        }

        private void ValueMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            if (!toolStripMenuItem.Checked && toolStripMenuItem.Tag is Motion_ValueListItem item)
            {
                int index = base.Owner.ListItems.IndexOf(item);
                base.Owner.SelectItem(index);
            }
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                foreach (Motion_ValueListItem listItem in base.Owner.ListItems)
                {
                    if (listItem.IsVisible && listItem.BoxName.Contains(e.CanvasLocation))
                    {
                        return GH_ObjectResponse.Handled;
                    }
                }
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }

        protected override void Layout()
        {
            LayoutDropDown();

            ItemBounds = Bounds;
            RectangleF rectangleF = Bounds;
            if (base.Owner.DisplayName != null)
            {
                int num = GH_FontServer.StringWidth(base.Owner.DisplayName, GH_FontServer.Standard) + 10;
                rectangleF = (NameBounds = new RectangleF(Bounds.X - (float)num, Bounds.Y, num, Bounds.Height));
                Bounds = RectangleF.Union(NameBounds, ItemBounds);
            }
        }

        private void LayoutDropDown()
        {
            int totalWidth = ItemMaximumWidth() + ItemHeight;
            int totalHeight = ItemHeight;
            Pivot = GH_Convert.ToPoint(Pivot);
            Bounds = new RectangleF(Pivot.X, Pivot.Y, totalWidth, totalHeight);

            var selectedItem = Owner.FirstSelectedItem;
            foreach (var item in Owner.ListItems)
            {
                if (item == selectedItem)
                    item.SetDropdownBounds(Bounds);
                else
                    item.SetEmptyBounds(Bounds);
            }
        }

        private int ItemMaximumWidth()
        {
            int num = 20;
            foreach (Motion_ValueListItem listItem in base.Owner.ListItems)
            {
                int val = GH_FontServer.StringWidth(listItem.Name, GH_FontServer.Standard);
                num = Math.Max(num, val);
            }
            return num + 10;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                RenderIncomingWires(canvas.Painter, base.Owner.Sources, base.Owner.WireDisplay);
            }
            if (channel != GH_CanvasChannel.Objects)
            {
                return;
            }
            GH_Capsule gH_Capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Black);
            gH_Capsule.AddOutputGrip(OutputGrip.Y);
            gH_Capsule.AddInputGrip(InputGrip.Y);
            gH_Capsule.Render(canvas.Graphics, Selected, base.Owner.Locked, base.Owner.Hidden);
            gH_Capsule.Dispose();
            int zoomFadeLow = GH_Canvas.ZoomFadeLow;
            if (zoomFadeLow > 0)
            {
                canvas.SetSmartTextRenderingHint();
                GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.White, this);
                Color color = Color.FromArgb(zoomFadeLow, impliedStyle.Text);
                if (NameBounds.Width > 0f)
                {
                    SolidBrush solidBrush = new SolidBrush(color);
                    graphics.DrawString(base.Owner.NickName, GH_FontServer.Standard, solidBrush, NameBounds, GH_TextRenderingConstants.CenterCenter);
                    solidBrush.Dispose();
                    int x = Convert.ToInt32(NameBounds.Right);
                    int y = Convert.ToInt32(NameBounds.Top);
                    int y2 = Convert.ToInt32(NameBounds.Bottom);
                    GH_GraphicsUtil.EtchFadingVertical(graphics, y, y2, x, Convert.ToInt32(0.8 * (double)zoomFadeLow), Convert.ToInt32(0.3 * (double)zoomFadeLow));
                }

                RenderDropDown(canvas, graphics, color);

            }
        }

        private void RenderDropDown(GH_Canvas canvas, Graphics graphics, Color color)
        {
            Motion_ValueListItem firstSelectedItem = base.Owner.FirstSelectedItem;
            if (firstSelectedItem != null)
            {
                graphics.DrawString(firstSelectedItem.Name, GH_FontServer.Standard, Brushes.White, firstSelectedItem.BoxName, GH_TextRenderingConstants.CenterCenter);
                RenderDownArrow(canvas, graphics, firstSelectedItem.BoxRight, color);
            }
        }

        private static void RenderDownArrow(GH_Canvas canvas, Graphics graphics, RectangleF bounds, Color color)
        {
            int centerX = Convert.ToInt32(bounds.X + 0.5f * bounds.Width);
            int centerY = Convert.ToInt32(bounds.Y + 0.5f * bounds.Height);

            PointF[] arrowPoints = new PointF[]
            {
            new PointF(centerX, centerY + ArrowRadius),
            new PointF(centerX + ArrowRadius, centerY - ArrowRadius),
            new PointF(centerX - ArrowRadius, centerY - ArrowRadius)
            };

            RenderShape(canvas, graphics, arrowPoints, color);
        }

        private static void RenderShape(GH_Canvas canvas, Graphics graphics, PointF[] points, Color color)
        {
            int fadeLevel = GH_Canvas.ZoomFadeMedium;
            float minX = points[0].X;
            float maxX = points[0].X;
            float minY = points[0].Y;
            float maxY = points[0].Y;

            // 计算包围盒
            for (int i = 1; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                maxX = Math.Max(maxX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxY = Math.Max(maxY, points[i].Y);
            }

            // 创建渐变区域
            RectangleF gradientBounds = RectangleF.FromLTRB(minX, minY, maxX, maxY);
            gradientBounds.Inflate(1f, 1f);

            // 主渐变填充
            using (var fillGradient = new LinearGradientBrush(gradientBounds, color,
                GH_GraphicsUtil.OffsetColour(color, 50), LinearGradientMode.Vertical))
            {
                fillGradient.WrapMode = WrapMode.TileFlipXY;
                graphics.FillPolygon(fillGradient, points);
            }

            // 高光效果
            if (fadeLevel > 0)
            {
                Color highlightStart = Color.FromArgb(Convert.ToInt32(0.5 * fadeLevel), Color.White);
                Color highlightEnd = Color.FromArgb(0, Color.White);

                using (var highlightGradient = new LinearGradientBrush(gradientBounds,
                    highlightStart, highlightEnd, LinearGradientMode.Vertical))
                using (var highlightPen = new Pen(highlightGradient, 3f)
                {
                    LineJoin = LineJoin.Round,
                    CompoundArray = new float[] { 0f, 0.5f }
                })
                {
                    highlightGradient.WrapMode = WrapMode.TileFlipXY;
                    graphics.DrawPolygon(highlightPen, points);
                }
            }

            // 轮廓绘制
            using (var outlinePen = new Pen(color, 1f) { LineJoin = LineJoin.Round })
            {
                graphics.DrawPolygon(outlinePen, points);
            }
        }
    }
}
