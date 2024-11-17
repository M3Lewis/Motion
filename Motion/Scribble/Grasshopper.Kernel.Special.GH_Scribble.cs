// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// Grasshopper.Kernel.Special.GH_Scribble
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.My.Resources;
using Microsoft.VisualBasic.CompilerServices;
using Rhino.Geometry;

/// <exclude />
public class GH_Scribble : GH_DocumentObject, IGH_InitCodeAware
{
	protected string m_text;

	protected Font m_font;

	protected SizeF m_size;

	protected PointF[] m_corners;

	private const string defaultText = "Doubleclick Me!";

	public static Guid ScribbleGuid => new Guid("{7F5C6C55-F846-4a08-9C9A-CFDC285CC6FE}");

	public override Guid ComponentGuid => ScribbleGuid;

	public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.dropdown;

	protected override Bitmap Icon => Res_ObjectIcons.Obj_Scribble_24x24;

	public PointF[] Corners => m_corners;

	public PointF A => m_corners[0];

	public PointF B => m_corners[1];

	public PointF C => m_corners[2];

	public PointF D => m_corners[3];

	public string Text
	{
		get
		{
			return m_text;
		}
		set
		{
			if (Operators.CompareString(m_text, value, TextCompare: false) != 0)
			{
				m_text = value;
				RecomputeLayout();
			}
		}
	}

	public Font Font
	{
		get
		{
			return m_font;
		}
		set
		{
			m_font = value;
			RecomputeLayout();
		}
	}

	public GH_Scribble()
		: base("Scribble", "Scribble", "A quick note", "Params", "Util")
	{
		m_font = GH_FontServer.NewFont(GH_FontServer.Script.FontFamily, 25f, FontStyle.Bold);
		m_corners = new PointF[4]
		{
			new PointF(0f, 0f),
			new PointF(100f, 0f),
			new PointF(100f, 100f),
			new PointF(0f, 100f)
		};
		Text = "Doubleclick Me!";
	}

	public void SetInitCode(string code)
	{
		if (code != null && code.Length != 0)
		{
			code = code.Trim();
			if (code.Length != 0)
			{
				Text = code;
			}
		}
	}

	void IGH_InitCodeAware.SetInitCode(string code)
	{
		//ILSpy generated this explicit interface implementation from .override directive in SetInitCode
		this.SetInitCode(code);
	}

	public override void CreateAttributes()
	{
		m_attributes = new GH_ScribbleAttributes(this);
	}

	private void RecomputeLayout()
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddString(m_text, Font.FontFamily, (int)Font.Style, Font.SizeInPoints, default(PointF), new StringFormat());
		RectangleF bounds = graphicsPath.GetBounds();
		graphicsPath.Dispose();
		m_size = bounds.Size;
		Vector3d vector3d = new Vector3d(B.X - A.X, B.Y - A.Y, 0.0);
		Vector3d vector3d2 = new Vector3d(D.X - A.X, D.Y - A.Y, 0.0);
		vector3d.Unitize();
		vector3d2.Unitize();
		vector3d *= (double)m_size.Width;
		vector3d2 *= (double)m_size.Height;
		m_corners[1].X = Convert.ToSingle((double)m_corners[0].X + vector3d.X);
		m_corners[1].Y = Convert.ToSingle((double)m_corners[0].Y + vector3d.Y);
		m_corners[3].X = Convert.ToSingle((double)m_corners[0].X + vector3d2.X);
		m_corners[3].Y = Convert.ToSingle((double)m_corners[0].Y + vector3d2.Y);
		m_corners[2].X = Convert.ToSingle((double)m_corners[0].X + vector3d2.X + vector3d.X);
		m_corners[2].Y = Convert.ToSingle((double)m_corners[0].Y + vector3d2.Y + vector3d.Y);
	}

	public void DisplayProperties()
	{
		GH_ScribblePropertiesDialog gH_ScribblePropertiesDialog = new GH_ScribblePropertiesDialog();
		gH_ScribblePropertiesDialog.SetUserFont(m_font);
		gH_ScribblePropertiesDialog.txtText.Text = m_text;
		GH_WindowsFormUtil.CenterFormOnCursor((Form)gH_ScribblePropertiesDialog, limitToScreen: true);
		if (gH_ScribblePropertiesDialog.ShowDialog(Instances.DocumentEditor) == DialogResult.OK)
		{
			RecordUndoEvent("Scribble Properties");
			m_text = gH_ScribblePropertiesDialog.txtText.Text;
			if (string.IsNullOrEmpty(m_text))
			{
				m_text = "Doubleclick Me!";
			}
			m_font = gH_ScribblePropertiesDialog.GetUserFont();
			RecomputeLayout();
			base.Attributes.ExpireLayout();
			OnDisplayExpired(redraw: true);
		}
	}

	public override bool Write(GH_IWriter writer)
	{
		writer.SetDrawingPointF("Ca", A);
		writer.SetDrawingPointF("Cb", B);
		writer.SetDrawingPointF("Cc", C);
		writer.SetDrawingPointF("Cd", D);
		writer.SetString("Text", m_text);
		writer.SetSingle("Size", m_font.Size);
		writer.SetString("Font", m_font.FontFamily.Name);
		writer.SetBoolean("Bold", m_font.Bold);
		writer.SetBoolean("Italic", m_font.Italic);
		return base.Write(writer);
	}

	public override bool Read(GH_IReader reader)
	{
		m_corners[0] = reader.GetDrawingPointF("Ca");
		m_corners[1] = reader.GetDrawingPointF("Cb");
		m_corners[2] = reader.GetDrawingPointF("Cc");
		m_corners[3] = reader.GetDrawingPointF("Cd");
		m_text = reader.GetString("Text");
		string value = m_font.FontFamily.Name;
		float value2 = m_font.Size;
		bool value3 = m_font.Bold;
		bool value4 = m_font.Italic;
		reader.TryGetString("Font", ref value);
		reader.TryGetSingle("Size", ref value2);
		reader.TryGetBoolean("Bold", ref value3);
		reader.TryGetBoolean("Italic", ref value4);
		FontStyle fontStyle = FontStyle.Regular;
		if (value3)
		{
			fontStyle |= FontStyle.Bold;
		}
		if (value4)
		{
			fontStyle |= FontStyle.Italic;
		}
		m_font = GH_FontServer.NewFont(value, value2, fontStyle);
		RecomputeLayout();
		return base.Read(reader);
	}
}
