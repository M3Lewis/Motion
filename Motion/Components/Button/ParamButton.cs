using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Drawing.Drawing2D;
using System.Windows.Controls.Primitives;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.Button
{
    public class ParamButton : GH_Attributes<ComponentWithButtonOnly>
    {
        private RectangleF m_button;

        private RectangleF m_name;

        public bool isPressed;

        public override bool HasInputGrip => false;

        public override bool HasOutputGrip => false;

        public override bool TooltipEnabled => true;

        public string ButtonText { get; }

        public ParamButton(ComponentWithButtonOnly owner)
            : base(owner)
        {
            PerformLayout();
        }

        protected override void Layout()
        {
            Pivot = GH_Convert.ToPoint(Pivot);
            int num = GH_FontServer.StringWidth(base.Owner.NickName, GH_FontServer.Standard);
            int num2 = 50;
            num += 16;
            PointF pivot = Pivot;
            Size size = new Size(num, 22);
            m_name = new RectangleF(pivot, size);
            m_button = new RectangleF(m_name.Right, m_name.Top, num2, m_name.Height);
            Bounds = RectangleF.Union(m_name, m_button);
            m_name.Inflate(-2f, -2f);
            m_button.Inflate(-2f, -2f);
        }

        public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
        {
            if (m_button.Contains(point))
            {
                e.Icon = base.Owner.Icon_24x24;
                e.Title = base.Owner.NickName;
                e.Text = "Click here to activate.";
                e.Description = "All of the relevant components will be activated.";
            }
            else
            {
                e.Icon = base.Owner.Icon_24x24;
                e.Title = base.Owner.NickName;
                e.Text = base.Owner.Name;
                e.Description = base.Owner.Description;
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button != MouseButtons.Left || !(sender.Viewport.Zoom >= 0.5f) || !m_button.Contains(e.CanvasLocation))
            {
                return base.RespondToMouseDown(sender, e);
            }
            isPressed = true;
            base.Owner.ExpireSolution(recompute: true);
            return GH_ObjectResponse.Handled;
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (!base.Owner.ButtonPressed)
            {
                return base.RespondToMouseUp(sender, e);
            }
            isPressed = false;
            base.Owner.ExpireSolution(recompute: true);
            return GH_ObjectResponse.Release;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel != GH_CanvasChannel.Objects)
            {
                return;
            }
            GH_Viewport viewport = canvas.Viewport;
            RectangleF rec = Bounds;
            bool flag = viewport.IsVisible(ref rec, 10f);
            Bounds = rec;
            if (flag)
            {
                int zoomFadeLow = GH_Canvas.ZoomFadeLow;
                GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.Normal, Selected, base.Owner.Locked, hidden: true);
                GH_Capsule gH_Capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Normal);
                gH_Capsule.Font = GH_FontServer.Standard;
                gH_Capsule.Render(graphics, Selected, base.Owner.Locked, hidden: true);
                gH_Capsule.Dispose();
                if (base.Owner.NickName.Length > 0)
                {
                    canvas.SetSmartTextRenderingHint();
                    SolidBrush solidBrush = new SolidBrush(Color.FromArgb(zoomFadeLow, impliedStyle.Text));
                    graphics.DrawString(base.Owner.NickName, GH_FontServer.Standard, solidBrush, m_name, GH_TextRenderingConstants.CenterCenter);
                    solidBrush.Dispose();
                }
                GH_Capsule gH_Capsule2 = GH_Capsule.CreateCapsule(m_button, GH_Palette.Black, 1, 9);
                gH_Capsule2.RenderEngine.RenderGrips(graphics);
                impliedStyle = GH_Skin.palette_black_standard;
                if (isPressed)
                {
                    LinearGradientBrush linearGradientBrush = new LinearGradientBrush(gH_Capsule2.Box, GH_GraphicsUtil.OffsetColour(impliedStyle.Fill, 0), GH_GraphicsUtil.OffsetColour(impliedStyle.Fill, 100), LinearGradientMode.Vertical);
                    graphics.FillPath(linearGradientBrush, gH_Capsule2.OutlineShape);
                    linearGradientBrush.Dispose();
                }
                else
                {
                    gH_Capsule2.RenderEngine.RenderBackground(graphics, canvas.Viewport.Zoom, impliedStyle);
                    gH_Capsule2.RenderEngine.RenderHighlight(graphics);
                }
                gH_Capsule2.RenderEngine.RenderOutlines(graphics, canvas.Viewport.Zoom, impliedStyle);
                gH_Capsule2.Dispose();
            }
        }
    }
}