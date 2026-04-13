using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Motion.UI
{
    public abstract class MotionButtonTemplate : GH_ComponentAttributes
    {
        public bool PressedOpen { get; set; }
        public bool PressedExport { get; set; }
        public bool Active { get; set; }
        
        protected MotionButtonTemplate(IGH_Component component,  string button1Text, string button2Text,
            Action<object, GH_CanvasMouseEvent, bool> buttonClickHandler) : base(component)
        {
            PressedOpen = false;
            PressedExport = false;
            Active = false;
            Button1Text = button1Text;
            Button2Text = button2Text;
            ButtonClickHandler = buttonClickHandler;
        }
        
        public string Button2Text { get; set; }
        public string Button1Text { get; set; }
        public Action<object, GH_CanvasMouseEvent, bool> ButtonClickHandler { get; set; }

        protected override void Layout()
        {
            base.Layout();
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 40); // 增加高度以容纳两个按钮
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel == GH_CanvasChannel.Objects)
            {
                // Open 按钮 (放在底部)
                RectangleF button2Rect = new RectangleF(Bounds.X, Bounds.Bottom - 20, Bounds.Width, 20.0f);
                button2Rect.Inflate(-2.0f, -2.0f);

                using (GH_Capsule capsule = GH_Capsule.CreateCapsule(button2Rect, (PressedOpen) ? GH_Palette.Grey : GH_Palette.Black))
                {
                    capsule.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
                }

                graphics.DrawString(
                    Button2Text,
                    new Font(GH_FontServer.ConsoleSmall, FontStyle.Regular),
                    Brushes.Azure,
                    button2Rect,
                    new StringFormat()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });

                // Export 按钮 (放在 Open 按钮上面)
                RectangleF button1Rect = new RectangleF(Bounds.X, Bounds.Bottom - 40, Bounds.Width, 20.0f);
                button1Rect.Inflate(-2.0f, -2.0f);

                using (GH_Capsule capsule = GH_Capsule.CreateCapsule(button1Rect, (PressedExport) ? GH_Palette.Grey : GH_Palette.Black))
                {
                    capsule.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
                }

                graphics.DrawString(
                    Button1Text,
                    new Font(GH_FontServer.ConsoleSmall, FontStyle.Regular),
                    Brushes.Azure,
                    button1Rect,
                    new StringFormat()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            // 确保按钮区域计算与渲染一致
            RectangleF button2Rect = new RectangleF(Bounds.X, Bounds.Bottom - 20, Bounds.Width, 20.0f);
            RectangleF button1Rect = new RectangleF(Bounds.X, Bounds.Bottom - 40, Bounds.Width, 20.0f);

            if (e.Button == MouseButtons.Left)
            {
                if (button1Rect.Contains(e.CanvasLocation))  // 先检查上面的按钮
                {
                    PressedExport = true;
                    Active = true;
                    ButtonClickHandler(sender, e, true);
                    sender.Refresh();
                    return GH_ObjectResponse.Handled;
                }
                else if (button2Rect.Contains(e.CanvasLocation))  // 再检查下面的按钮
                {
                    PressedOpen = true;
                    Active = true;
                    ButtonClickHandler(sender, e, false);
                    sender.Refresh();
                    return GH_ObjectResponse.Handled;
                }
            }
            return GH_ObjectResponse.Ignore;
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            PressedOpen = false;
            PressedExport = false;
            sender.Refresh();
            return GH_ObjectResponse.Ignore;
        }
    }
}
