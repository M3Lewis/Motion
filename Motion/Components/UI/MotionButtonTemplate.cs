using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;

namespace Motion.UI
{
    public abstract class MotionButtonTemplate : GH_ComponentAttributes
    {
        public bool Pressed { get; set; }
        public bool Active { get; set; }
        protected MotionButtonTemplate(IGH_Component component,string buttonText,Action<object,GH_CanvasMouseEvent> buttonClickHandler) :base(component)
        {
            Pressed = false;
            Active = false;
            ButtonText = buttonText;
            ButtonClickHandler = buttonClickHandler;
        }
        
        public string ButtonText { get; set; }
        public Action<object, GH_CanvasMouseEvent> ButtonClickHandler { get; set; }

       
        protected override void Layout()
        {
            base.Layout();
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height+20);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel == GH_CanvasChannel.Objects)
            {
                RectangleF buttonRect = new RectangleF(Bounds.X, Bounds.Bottom-20, Bounds.Width, 20.0f);
                buttonRect.Inflate(-2.0f, -2.0f);

                using (GH_Capsule capsule = GH_Capsule.CreateCapsule(buttonRect, (Pressed) ? GH_Palette.Grey:GH_Palette.Black))
                {
                    capsule.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
                }

                graphics.DrawString(
                    ButtonText,
                    new Font(GH_FontServer.ConsoleSmall, FontStyle.Regular),
                    Brushes.Azure,
                    buttonRect,
                    new StringFormat()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
            }
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            RectangleF buttonRect = new RectangleF(Bounds.X, Bounds.Bottom - 20, Bounds.Width, 20.0f);
            if (e.Button == MouseButtons.Left && buttonRect.Contains(e.CanvasLocation))//如果按下了鼠标左键且按钮范围内包含鼠标点击时的位置
            {
                Pressed = true;
                Active = true;
                //在做事件处理时，调用abstract方法来做真正的事件处理
                ButtonClickHandler(sender, e);//触发事件
                sender.Refresh();
                
                return GH_ObjectResponse.Handled;//返回处理结果（已处理）
            }
            return GH_ObjectResponse.Ignore;//返回处理结果（忽略）
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            RectangleF buttonRect = new RectangleF(Bounds.X, Bounds.Bottom - 20, Bounds.Width, 20.0f);

            Pressed = false;
            
            sender.Refresh();
            return GH_ObjectResponse.Ignore;
        }
    }
}
