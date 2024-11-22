using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino.Geometry;
using System.Drawing;

namespace Motion.Animation
{
    public class EventOperationAttributes : GH_ComponentAttributes
    {
        public EventOperationAttributes(EventOperation owner) : base(owner)
        {
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var eventOperation = Owner as EventOperation;
                if (eventOperation != null)
                {
                    var labelFont = new Font(GH_FontServer.StandardBold.FontFamily, 8);

                    double eventValue = eventOperation.CurrentEventValue;
                    string valueLabel = $"{eventValue:F2}";
                    
                    var valueLabelBounds = new RectangleF(
                        Bounds.Left - 30,
                        Bounds.Top- 5,
                        30,
                        labelFont.Height
                    );

                    using (var brush = new SolidBrush(Color.LightSkyBlue))
                    {
                        graphics.DrawString(valueLabel, labelFont, brush, valueLabelBounds);
                    }

                    double currentMappedEventValue = eventOperation.CurrentMappedEventValue;
                    string currentMappedEventValueLabel = $"{currentMappedEventValue:F2}";

                    var currentMappedEventValueBounds = new RectangleF(
                        Bounds.Right + 5,
                        Bounds.Top + 5,
                        30,
                        labelFont.Height
                    );

                    using (var brush = new SolidBrush(Color.Orange))
                    {
                        graphics.DrawString(currentMappedEventValueLabel, labelFont, brush, currentMappedEventValueBounds);
                    }
                }
            }
        }
    }
}