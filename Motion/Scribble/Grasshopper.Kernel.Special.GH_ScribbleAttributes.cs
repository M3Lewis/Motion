// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// Grasshopper.Kernel.Special.GH_ScribbleAttributes
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

/// <exclude />
public class GH_ScribbleAttributes : GH_Attributes<GH_Scribble>
{
	private enum GH_DragMode
	{
		None,
		DragStart,
		Dragging
	}

	private GH_DragMode m_drag_mode;

	private PointF m_drag_start;

	private GraphicsPath m_text_path;

	public override bool HasInputGrip => false;

	public override bool HasOutputGrip => false;

	public override bool TooltipEnabled => false;

	public override PointF Pivot
	{
		get
		{
			return base.Owner.A;
		}
		set
		{
			base.Pivot = value;
			float num = value.X - base.Owner.A.X;
			float num2 = value.Y - base.Owner.A.Y;
			int num3 = base.Owner.Corners.Length - 1;
			for (int i = 0; i <= num3; i++)
			{
				PointF pointF = base.Owner.Corners[i];
				base.Owner.Corners[i] = new PointF(pointF.X + num, pointF.Y + num2);
			}
			ExpireLayout();
		}
	}

	/// <summary>
	/// Gets the path of the scribble object. Might be null.
	/// </summary>
	public GraphicsPath ScribblePath => m_text_path;

	public GH_ScribbleAttributes(GH_Scribble owner)
		: base(owner)
	{
		m_drag_mode = GH_DragMode.None;
	}

	public override bool IsPickRegion(PointF point)
	{
		RectangleF bounds = Bounds;
		bounds.Inflate(5f, 5f);
		if (!bounds.Contains(point))
		{
			return false;
		}
		if (m_text_path == null)
		{
			return false;
		}
		if (m_text_path.IsVisible(point))
		{
			return true;
		}
		Pen pen = new Pen(Color.Black, 10f);
		if (m_text_path.IsOutlineVisible(point, pen))
		{
			return true;
		}
		return false;
	}

	public override bool IsPickRegion(RectangleF box, GH_PickBox method)
	{
		return GH_Attributes<GH_Scribble>.SolvePathBoxPick(m_text_path, box, 2f, method);
	}

	private float Distance(PointF A, PointF B)
	{
		return Convert.ToSingle(Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y)));
	}

	public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (e.Button == MouseButtons.Left && IsPickRegion(e.CanvasLocation))
		{
			m_drag_mode = GH_DragMode.DragStart;
			m_drag_start = e.CanvasLocation;
			if (sender.IsDocument)
			{
				bool flag = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
				bool flag2 = (Control.ModifierKeys & Keys.Control) == Keys.Control;
				if (!flag && !flag2)
				{
					sender.Document.DeselectAll();
					Selected = true;
				}
				else if (flag && !flag2)
				{
					Selected = true;
				}
				else if (!flag && flag2)
				{
					Selected = false;
				}
				else
				{
					Selected = !Selected;
				}
			}
			else
			{
				Selected = true;
			}
			Instances.RedrawAll();
			return GH_ObjectResponse.Capture;
		}
		return base.RespondToMouseDown(sender, e);
	}

	public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_drag_mode == GH_DragMode.DragStart)
			{
				base.Owner.RecordUndoEvent("Scribbe Drag");
				m_drag_mode = GH_DragMode.Dragging;
			}
			if (m_drag_mode == GH_DragMode.Dragging)
			{
				switch (Control.ModifierKeys)
				{
				case Keys.Shift:
					DragFree(e);
					break;
				case Keys.Control:
					DragRotate(e);
					break;
				default:
					DragConstrain(e);
					break;
				}
				ExpireLayout();
				sender.Refresh();
				return GH_ObjectResponse.Handled;
			}
		}
		return base.RespondToMouseMove(sender, e);
	}

	public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (m_drag_mode != 0)
		{
			m_drag_mode = GH_DragMode.None;
			return GH_ObjectResponse.Release;
		}
		return base.RespondToMouseUp(sender, e);
	}

	public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (IsPickRegion(e.CanvasLocation))
		{
			base.Owner.DisplayProperties();
			return GH_ObjectResponse.Handled;
		}
		return base.RespondToMouseDoubleClick(sender, e);
	}

	private void DragFree(GH_CanvasMouseEvent e)
	{
		PointF a = new PointF(0.5f * (base.Owner.A.X + base.Owner.C.X), 0.5f * (base.Owner.A.Y + base.Owner.C.Y));
		double num = Math.Atan2(m_drag_start.Y - a.Y, m_drag_start.X - a.X);
		double num2 = Math.Atan2(e.CanvasY - a.Y, e.CanvasX - a.X);
		double num3 = 180.0 * ((num2 - num) / Math.PI);
		if (num3 > 180.0)
		{
			num3 -= 360.0;
		}
		else if (num3 < -180.0)
		{
			num3 += 360.0;
		}
		float offsetX = e.CanvasX - m_drag_start.X;
		float offsetY = e.CanvasY - m_drag_start.Y;
		float num4 = Distance(a, m_drag_start);
		if (num4 < 10f)
		{
			num3 = 0.0;
		}
		else
		{
			float num5 = 0.5f * Distance(base.Owner.A, base.Owner.C);
			num4 = num4 * num4 / (num5 * num5);
			num3 *= (double)num4;
		}
		Matrix matrix = new Matrix();
		matrix.Translate(offsetX, offsetY);
		matrix.TransformPoints(base.Owner.Corners);
		Matrix matrix2 = new Matrix();
		matrix2.RotateAt(Convert.ToSingle(num3), e.CanvasLocation);
		matrix2.TransformPoints(base.Owner.Corners);
		base.Pivot = base.Owner.A;
		m_drag_start = e.CanvasLocation;
	}

	private void DragRotate(GH_CanvasMouseEvent e)
	{
		PointF point = new PointF(0.5f * (base.Owner.A.X + base.Owner.C.X), 0.5f * (base.Owner.A.Y + base.Owner.C.Y));
		double num = Math.Atan2(m_drag_start.Y - point.Y, m_drag_start.X - point.X);
		double num2 = Math.Atan2(e.CanvasY - point.Y, e.CanvasX - point.X);
		double value = 180.0 * ((num2 - num) / Math.PI);
		Matrix matrix = new Matrix();
		matrix.RotateAt(Convert.ToSingle(value), point);
		matrix.TransformPoints(base.Owner.Corners);
		base.Pivot = base.Owner.A;
		m_drag_start = e.CanvasLocation;
	}

	private void DragConstrain(GH_CanvasMouseEvent e)
	{
		new PointF(0.5f * (base.Owner.A.X + base.Owner.C.X), 0.5f * (base.Owner.A.Y + base.Owner.C.Y));
		float offsetX = e.CanvasX - m_drag_start.X;
		float offsetY = e.CanvasY - m_drag_start.Y;
		Matrix matrix = new Matrix();
		matrix.Translate(offsetX, offsetY);
		matrix.TransformPoints(base.Owner.Corners);
		base.Pivot = base.Owner.A;
		m_drag_start = e.CanvasLocation;
	}

	protected override void Layout()
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		int num5 = 0;
		do
		{
			num = Math.Min(num, base.Owner.Corners[num5].X);
			num2 = Math.Max(num2, base.Owner.Corners[num5].X);
			num3 = Math.Min(num3, base.Owner.Corners[num5].Y);
			num4 = Math.Max(num4, base.Owner.Corners[num5].Y);
			num5++;
		}
		while (num5 <= 3);
		Bounds = RectangleF.FromLTRB(num - 5f, num3 - 5f, num2 + 5f, num4 + 5f);
	}

	public override void ExpireLayout()
	{
		base.ExpireLayout();
		if (m_text_path != null)
		{
			m_text_path.Dispose();
			m_text_path = null;
		}
	}

	private void CreateTextPath()
	{
		m_text_path = null;
		if (!string.IsNullOrEmpty(base.Owner.Text))
		{
			GraphicsPath graphicsPath = new GraphicsPath();
			graphicsPath.AddString(base.Owner.Text, base.Owner.Font.FontFamily, (int)base.Owner.Font.Style, base.Owner.Font.Size, default(Point), new StringFormat());
			RectangleF bounds = graphicsPath.GetBounds();
			Matrix matrix = new Matrix();
			matrix.Translate(base.Owner.A.X - bounds.X, base.Owner.A.Y - bounds.Y);
			graphicsPath.Transform(matrix);
			double num = Math.Atan2(base.Owner.B.Y - base.Owner.A.Y, base.Owner.B.X - base.Owner.A.X);
			num = 180.0 * (num / Math.PI);
			matrix = new Matrix();
			matrix.RotateAt(Convert.ToSingle(num), base.Owner.A);
			graphicsPath.Transform(matrix);
			m_text_path = graphicsPath;
		}
	}

	protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
	{
		if (channel != GH_CanvasChannel.Objects)
		{
			return;
		}
		GH_Viewport viewport = canvas.Viewport;
		RectangleF rec = Bounds;
		bool num = viewport.IsVisible(ref rec, 10f);
		Bounds = rec;
		if (!num)
		{
			return;
		}
		float num2 = canvas.Viewport.Zoom * base.Owner.Font.Size;
		if (!(num2 < 2f))
		{
			if (num2 < 8f)
			{
				DrawScribbleBox(graphics, num2);
			}
			if (num2 > 5f)
			{
				DrawScribbleText(graphics, num2);
			}
		}
	}

	private void DrawScribbleBox(Graphics graphics, float visibleFontSize)
	{
		int val = GH_GraphicsUtil.BlendInteger(2.0, 5.0, 0, 100, visibleFontSize);
		int val2 = GH_GraphicsUtil.BlendInteger(5.0, 8.0, 100, 0, visibleFontSize);
		int alpha = Math.Min(val, val2);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, Selected ? Color.DarkGreen : Color.Black));
		graphics.FillPolygon(brush, new PointF[4]
		{
			base.Owner.A,
			base.Owner.B,
			base.Owner.C,
			base.Owner.D
		});
	}

	private void DrawScribbleText(Graphics graphics, float visibleFontSize)
	{
		int alpha = GH_GraphicsUtil.BlendInteger(5.0, 8.0, 0, 255, visibleFontSize);
		if (m_text_path == null)
		{
			CreateTextPath();
		}
		if (m_text_path != null)
		{
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, Selected ? Color.DarkGreen : Color.Black)))
			{
				graphics.FillPath(brush, m_text_path);
			}
		}
	}
}
