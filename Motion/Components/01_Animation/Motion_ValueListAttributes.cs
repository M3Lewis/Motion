using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class Motion_ValueListAttributes : GH_Attributes<RangeSelector>
    {
        public const int ItemHeight = 22;

        private const int ArrowRadius = 6;

        public override bool AllowMessageBalloon => false;

        public override bool HasInputGrip => true;

        public override bool HasOutputGrip => true;

        private RectangleF ItemBounds { get; set; }

        private RectangleF NameBounds { get; set; }

        public Motion_ValueListAttributes(RangeSelector owner)
            : base(owner)
        {
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var arrowBounds = new RectangleF(
                    Bounds.Right - ItemHeight, Bounds.Y,
                    ItemHeight, ItemHeight);

                if (arrowBounds.Contains(e.CanvasLocation))
                {
                    var menu = new ToolStripDropDownMenu();
                    menu.Closing += Menu_Closing;
                    
                    // 添加区间选项
                    foreach (var item in Owner.ListItems)
                    {
                        var menuItem = new ToolStripMenuItem(item.Name)
                        {
                            CheckOnClick = true,
                            Checked = item.Selected,
                            Tag = item
                        };
                        menuItem.Click += (s, args) =>
                        {
                            if (s is ToolStripMenuItem clickedItem && clickedItem.Tag is Motion_ValueListItem valueItem)
                            {
                                valueItem.Selected = clickedItem.Checked;
                                Owner.ExpireSolution(true);
                            }
                        };
                        menu.Items.Add(menuItem);
                    }

                    if (Owner.ListItems.Count > 0)
                    {
                        menu.Items.Add(new ToolStripSeparator());
                        
                        // 添加全选按钮
                        var selectAllItem = new ToolStripMenuItem("Select All", null, (s, args) =>
                        {
                            foreach (var item in Owner.ListItems)
                            {
                                item.Selected = true;
                            }
                            Owner.ExpireSolution(true);
                            
                            // 更新所有菜单项的选中状态
                            foreach (object menuObj in menu.Items)
                            {
                                if (menuObj is ToolStripMenuItem menuItem && 
                                    menuItem.Tag is Motion_ValueListItem)
                                {
                                    menuItem.Checked = true;
                                }
                            }
                        });
                        menu.Items.Add(selectAllItem);

                        // 添加取消全选按钮
                        var deselectAllItem = new ToolStripMenuItem("Deselect All", null, (s, args) =>
                        {
                            foreach (var item in Owner.ListItems)
                            {
                                item.Selected = false;
                            }
                            Owner.ExpireSolution(true);
                            
                            // 更新所有菜单项的选中状态
                            foreach (object menuObj in menu.Items)
                            {
                                if (menuObj is ToolStripMenuItem menuItem && 
                                    menuItem.Tag is Motion_ValueListItem)
                                {
                                    menuItem.Checked = false;
                                }
                            }
                        });
                        menu.Items.Add(deselectAllItem);
                    }

                    menu.Show(sender, e.ControlLocation);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private void Menu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            // 如果是鼠标点击菜单项导致的关闭，则取消关闭
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
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
            int totalWidth = ItemMaximumWidth() + ItemHeight;
            var selectedItems = Owner.SelectedItems;
            // 根据选中项目数量调整高度
            int totalHeight = Math.Max(1, selectedItems.Count) * ItemHeight;
            
            Pivot = GH_Convert.ToPoint(Pivot);
            Bounds = new RectangleF(Pivot.X, Pivot.Y, totalWidth, totalHeight);

            ItemBounds = Bounds;
            if (Owner.DisplayName != null)
            {
                int nameWidth = GH_FontServer.StringWidth(Owner.DisplayName, GH_FontServer.Standard) + 10;
                NameBounds = new RectangleF(Bounds.X - nameWidth, Bounds.Y, nameWidth, ItemHeight);
                Bounds = RectangleF.Union(NameBounds, ItemBounds);
            }

            // 为每个选中的项目设置显示区域
            float currentY = Bounds.Y;
            foreach (var item in Owner.ListItems)
            {
                if (item.Selected)
                {
                    item.SetDropdownBounds(new RectangleF(Bounds.X, currentY, Bounds.Width, ItemHeight));
                    currentY += ItemHeight;
                }
                else
                {
                    item.SetEmptyBounds(Bounds);
                }
            }
        }

        private int ItemMaximumWidth()
        {
            int width = 20;
            foreach (var item in Owner.ListItems)
            {
                width = Math.Max(width, GH_FontServer.StringWidth(item.Name, GH_FontServer.Standard));
            }
            return width + 10;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
                return;
            }

            if (channel != GH_CanvasChannel.Objects)
                return;

            var capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Black);
            capsule.AddOutputGrip(OutputGrip.Y);
            capsule.AddInputGrip(InputGrip.Y);
            capsule.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
            capsule.Dispose();

            int fade = GH_Canvas.ZoomFadeLow;
            if (fade <= 0) return;

            canvas.SetSmartTextRenderingHint();
            var style = GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.White, this);
            var textColor = Color.FromArgb(fade, style.Text);

            var selectedItems = Owner.SelectedItems;
            if (selectedItems.Count > 0)
            {
                // 渲染所有选中的项目
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    var item = selectedItems[i];
                    var itemBounds = new RectangleF(
                        Bounds.X, 
                        Bounds.Y + (i * ItemHeight), 
                        Bounds.Width - ItemHeight, 
                        ItemHeight);

                    graphics.DrawString(item.Name, GH_FontServer.Standard, 
                        Brushes.White, itemBounds, GH_TextRenderingConstants.CenterCenter);
                }

                // 始终在第一行右侧渲染下拉箭头
                RenderDownArrow(canvas, graphics, new RectangleF(
                    Bounds.Right - ItemHeight, Bounds.Y,
                    ItemHeight, ItemHeight), textColor);
            }
            else
            {
                // 如果没有选中项，显示默认文本
                graphics.DrawString("Select...", GH_FontServer.Standard, 
                    Brushes.White, ItemBounds, GH_TextRenderingConstants.CenterCenter);
                RenderDownArrow(canvas, graphics, new RectangleF(
                    Bounds.Right - ItemHeight, Bounds.Y,
                    ItemHeight, ItemHeight), textColor);
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
