using System;
using System.ComponentModel;
using System.Drawing;
using Grasshopper.Kernel.Expressions;
using Grasshopper.Kernel.Types;
using Microsoft.VisualBasic.CompilerServices;

namespace Motion.Animation
{
    public class Motion_ValueListItem
    {
        private IGH_Goo m_value;

        private const int BoxWidth = 22;

        public bool Selected { get; set; }

        public string Name { get; set; }

        public string Expression { get; set; }

        public RectangleF BoxName { get; set; }

        public RectangleF BoxLeft { get; set; }

        public RectangleF BoxRight { get; set; }

        [Browsable(false)]
        public IGH_Goo Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Expression))
                {
                    return null;
                }
                if (m_value == null)
                {
                    try
                    {
                        GH_ExpressionParser gH_ExpressionParser = new GH_ExpressionParser();
                        string expression = GH_ExpressionSyntaxWriter.RewriteAll(Expression);
                        GH_Variant gH_Variant = gH_ExpressionParser.Evaluate(expression);
                        if (gH_Variant != null)
                        {
                            m_value = gH_Variant.ToGoo();
                        }
                    }
                    catch (Exception projectError)
                    {
                        ProjectData.SetProjectError(projectError);
                        ProjectData.ClearProjectError();
                    }
                }
                return m_value;
            }
        }

        public bool IsVisible => BoxName.Height > 0f;

        public Motion_ValueListItem()
        {
            Name = string.Empty;
            Expression = string.Empty;
        }

        public Motion_ValueListItem(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }

        public Motion_ValueListItem(string name, string value, IGH_Goo gooIn)
        {
            Name = name;
            Expression = value;
            m_value = gooIn;
        }

        public void ExpireValue()
        {
            m_value = null;
        }

        internal void SetCheckListBounds(RectangleF bounds)
        {
            RectangleF rectangleF2 = (BoxLeft = new RectangleF(bounds.X, bounds.Y, 22f, bounds.Height));
            rectangleF2 = (BoxName = new RectangleF(bounds.X + 22f, bounds.Y, bounds.Width - 22f, bounds.Height));
            rectangleF2 = (BoxRight = new RectangleF(bounds.Right, bounds.Y, 0f, bounds.Height));
        }

        internal void SetDropdownBounds(RectangleF bounds)
        {
            RectangleF rectangleF2 = (BoxLeft = new RectangleF(bounds.X, bounds.Y, 0f, bounds.Height));
            rectangleF2 = (BoxName = new RectangleF(bounds.X, bounds.Y, bounds.Width - 22f, bounds.Height));
            rectangleF2 = (BoxRight = new RectangleF(bounds.Right - 22f, bounds.Y, 22f, bounds.Height));
        }

        internal void SetSequenceBounds(RectangleF bounds)
        {
            RectangleF rectangleF2 = (BoxLeft = new RectangleF(bounds.X, bounds.Y, 22f, bounds.Height));
            rectangleF2 = (BoxName = new RectangleF(bounds.X + 22f, bounds.Y, bounds.Width - 44f, bounds.Height));
            rectangleF2 = (BoxRight = new RectangleF(bounds.Right - 22f, bounds.Y, 22f, bounds.Height));
        }

        internal void SetEmptyBounds(RectangleF bounds)
        {
            RectangleF rectangleF2 = (BoxLeft = new RectangleF(bounds.X, bounds.Y, 0f, 0f));
            rectangleF2 = (BoxName = new RectangleF(bounds.X, bounds.Y, 0f, 0f));
            rectangleF2 = (BoxRight = new RectangleF(bounds.X, bounds.Y, 0f, 0f));
        }
    }
}