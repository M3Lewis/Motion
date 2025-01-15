using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.My.Resources;
using Microsoft.VisualBasic.CompilerServices;

namespace Grasshopper.GUI.Widgets;

/// <summary>
/// Markov chain widget.
/// </summary>
/// <exclude />
public class GH_MarkovWidget : GH_Widget
{
	private enum GH_DragMode
	{
		none,
		grip_drag_start,
		grip_drag,
		item_drag_start,
		item_drag
	}

	public delegate void IconLimitChangedEventHandler();

	public delegate void DockCornerChangedEventHandler();

	public delegate void WidgetVisibleChangedEventHandler();

	private static readonly int GripSize = Global_Proc.UiAdjust(6);

	private static readonly int IconSize = Global_Proc.UiAdjust(28);

	private GH_DragMode m_drag_mode;

	private IGH_ObjectProxy m_drag_proxy;

	private static int m_iconLimit = Instances.Settings.GetValue("Widget.Markov.Suggestions", 5);

	[CompilerGenerated]
	private static IconLimitChangedEventHandler IconLimitChangedEvent;

	private static GH_MarkovWidgetDock m_dockCorner = (GH_MarkovWidgetDock)Instances.Settings.GetValue("Widget.Markov.Corner", 1);

	[CompilerGenerated]
	private static DockCornerChangedEventHandler DockCornerChangedEvent;

	[CompilerGenerated]
	private static WidgetVisibleChangedEventHandler WidgetVisibleChangedEvent;

	private static bool m_showWidget = Instances.Settings.GetValue("Widget.Markov.Show", @default: true);

	public override string Name => "Markov";

	public override string Description => "What's next?";

	public override Bitmap Icon_24x24 => Res_ObjectIcons.Widget_Markov_24x24;

	/// <summary>
	/// Gets the area of the entire markov strip (widget minus grip) 
	/// as limited by suggestion count and screen space.
	/// </summary>
	private Rectangle IconArea
	{
		get
		{
			List<Rectangle> iconAreas = IconAreas;
			if (iconAreas == null || iconAreas.Count == 0)
			{
				Rectangle clientRectangle = base.Owner.ClientRectangle;
				Rectangle result;
				switch (m_dockCorner)
				{
				case GH_MarkovWidgetDock.TopLeft:
					result = new Rectangle(clientRectangle.Left, clientRectangle.Top, 0, IconSize);
					goto IL_00dd;
				case GH_MarkovWidgetDock.BottomLeft:
					result = new Rectangle(clientRectangle.Left, clientRectangle.Bottom - IconSize, 0, IconSize);
					goto IL_00dd;
				case GH_MarkovWidgetDock.TopRight:
					result = new Rectangle(clientRectangle.Right, clientRectangle.Top, 0, IconSize);
					goto IL_00dd;
				case GH_MarkovWidgetDock.BottomRight:
					{
						result = new Rectangle(clientRectangle.Right, clientRectangle.Bottom - IconSize, 0, IconSize);
						goto IL_00dd;
					}
					IL_00dd:
					return result;
				}
			}
			return Rectangle.Union(iconAreas[0], iconAreas[iconAreas.Count - 1]);
		}
	}

	/// <summary>
	/// Gets the area taken up by the grip.
	/// </summary>
	private Rectangle GripArea
	{
		get
		{
			Rectangle result = IconArea;
			if (result.IsEmpty)
			{
				return Rectangle.Empty;
			}
			switch (m_dockCorner)
			{
			case GH_MarkovWidgetDock.TopLeft:
			case GH_MarkovWidgetDock.BottomLeft:
				result = new Rectangle(result.Right, result.Top, GripSize, result.Height);
				break;
			case GH_MarkovWidgetDock.TopRight:
			case GH_MarkovWidgetDock.BottomRight:
				result = new Rectangle(result.Left - GripSize, result.Top, GripSize, result.Height);
				break;
			}
			return result;
		}
	}

	/// <summary>
	/// Gets the area taken up by the entire widget.
	/// </summary>
	private Rectangle WidgetArea => Rectangle.Union(IconArea, GripArea);

	/// <summary>
	/// Gets the rectangles for all pods that can presently be displayed.
	/// </summary>
	private List<Rectangle> IconAreas => CreateIconAreas(Math.Min(base.Owner.MarkovSuggestions.Count, m_iconLimit));

	public static int IconLimit
	{
		get
		{
			return m_iconLimit;
		}
		set
		{
			value = Math.Min(value, 10);
			value = Math.Max(value, 0);
			if (m_iconLimit != value)
			{
				m_iconLimit = value;
				Instances.Settings.SetValue("Widget.Markov.Suggestions", value);
				IconLimitChangedEvent?.Invoke();
			}
		}
	}

	public static GH_MarkovWidgetDock DockCorner
	{
		get
		{
			return m_dockCorner;
		}
		set
		{
			if (m_dockCorner != value)
			{
				m_dockCorner = value;
				Instances.Settings.SetValue("Widget.Markov.Corner", (int)value);
				DockCornerChangedEvent?.Invoke();
			}
		}
	}

	public static bool SharedVisible
	{
		get
		{
			return m_showWidget;
		}
		set
		{
			if (m_showWidget != value)
			{
				m_showWidget = value;
				Instances.Settings.SetValue("Widget.Markov.Show", value);
				WidgetVisibleChangedEvent?.Invoke();
			}
		}
	}

	public override bool Visible
	{
		get
		{
			return SharedVisible;
		}
		set
		{
			SharedVisible = value;
		}
	}

	public static event IconLimitChangedEventHandler IconLimitChanged
	{
		[CompilerGenerated]
		add
		{
			IconLimitChangedEventHandler iconLimitChangedEventHandler = IconLimitChangedEvent;
			IconLimitChangedEventHandler iconLimitChangedEventHandler2;
			do
			{
				iconLimitChangedEventHandler2 = iconLimitChangedEventHandler;
				IconLimitChangedEventHandler value2 = (IconLimitChangedEventHandler)Delegate.Combine(iconLimitChangedEventHandler2, value);
				iconLimitChangedEventHandler = Interlocked.CompareExchange(ref IconLimitChangedEvent, value2, iconLimitChangedEventHandler2);
			}
			while ((object)iconLimitChangedEventHandler != iconLimitChangedEventHandler2);
		}
		[CompilerGenerated]
		remove
		{
			IconLimitChangedEventHandler iconLimitChangedEventHandler = IconLimitChangedEvent;
			IconLimitChangedEventHandler iconLimitChangedEventHandler2;
			do
			{
				iconLimitChangedEventHandler2 = iconLimitChangedEventHandler;
				IconLimitChangedEventHandler value2 = (IconLimitChangedEventHandler)Delegate.Remove(iconLimitChangedEventHandler2, value);
				iconLimitChangedEventHandler = Interlocked.CompareExchange(ref IconLimitChangedEvent, value2, iconLimitChangedEventHandler2);
			}
			while ((object)iconLimitChangedEventHandler != iconLimitChangedEventHandler2);
		}
	}

	public static event DockCornerChangedEventHandler DockCornerChanged
	{
		[CompilerGenerated]
		add
		{
			DockCornerChangedEventHandler dockCornerChangedEventHandler = DockCornerChangedEvent;
			DockCornerChangedEventHandler dockCornerChangedEventHandler2;
			do
			{
				dockCornerChangedEventHandler2 = dockCornerChangedEventHandler;
				DockCornerChangedEventHandler value2 = (DockCornerChangedEventHandler)Delegate.Combine(dockCornerChangedEventHandler2, value);
				dockCornerChangedEventHandler = Interlocked.CompareExchange(ref DockCornerChangedEvent, value2, dockCornerChangedEventHandler2);
			}
			while ((object)dockCornerChangedEventHandler != dockCornerChangedEventHandler2);
		}
		[CompilerGenerated]
		remove
		{
			DockCornerChangedEventHandler dockCornerChangedEventHandler = DockCornerChangedEvent;
			DockCornerChangedEventHandler dockCornerChangedEventHandler2;
			do
			{
				dockCornerChangedEventHandler2 = dockCornerChangedEventHandler;
				DockCornerChangedEventHandler value2 = (DockCornerChangedEventHandler)Delegate.Remove(dockCornerChangedEventHandler2, value);
				dockCornerChangedEventHandler = Interlocked.CompareExchange(ref DockCornerChangedEvent, value2, dockCornerChangedEventHandler2);
			}
			while ((object)dockCornerChangedEventHandler != dockCornerChangedEventHandler2);
		}
	}

	public static event WidgetVisibleChangedEventHandler WidgetVisibleChanged
	{
		[CompilerGenerated]
		add
		{
			WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
			WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
			do
			{
				widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
				WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Combine(widgetVisibleChangedEventHandler2, value);
				widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
			}
			while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
		}
		[CompilerGenerated]
		remove
		{
			WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler = WidgetVisibleChangedEvent;
			WidgetVisibleChangedEventHandler widgetVisibleChangedEventHandler2;
			do
			{
				widgetVisibleChangedEventHandler2 = widgetVisibleChangedEventHandler;
				WidgetVisibleChangedEventHandler value2 = (WidgetVisibleChangedEventHandler)Delegate.Remove(widgetVisibleChangedEventHandler2, value);
				widgetVisibleChangedEventHandler = Interlocked.CompareExchange(ref WidgetVisibleChangedEvent, value2, widgetVisibleChangedEventHandler2);
			}
			while ((object)widgetVisibleChangedEventHandler != widgetVisibleChangedEventHandler2);
		}
	}

	public GH_MarkovWidget()
	{
		m_drag_mode = GH_DragMode.none;
		m_drag_proxy = null;
	}

	public override bool Contains(Point pt_control, PointF pt_canvas)
	{
		if (base.Owner.Document == null)
		{
			return false;
		}
		return WidgetArea.Contains(pt_control);
	}

	/// <summary>
	/// Create N icon areas using the current docking style.
	/// </summary>
	/// <param name="N">Number of icon boxes to create.</param>
	/// <returns>A list of icon boxes on success, null on failure.</returns>
	private List<Rectangle> CreateIconAreas(int N)
	{
		if (N == 0)
		{
			return null;
		}
		List<Rectangle> list = new List<Rectangle>();
		Rectangle rectangle = new Rectangle(0, 0, IconSize, IconSize);
		if (m_dockCorner == GH_MarkovWidgetDock.BottomLeft || m_dockCorner == GH_MarkovWidgetDock.BottomRight)
		{
			rectangle.Y = base.Owner.ClientRectangle.Bottom - IconSize;
		}
		if (m_dockCorner == GH_MarkovWidgetDock.TopRight || m_dockCorner == GH_MarkovWidgetDock.BottomRight)
		{
			rectangle.X = base.Owner.ClientRectangle.Right - IconSize;
		}
		list.Add(rectangle);
		int num = N - 1;
		for (int i = 1; i <= num; i++)
		{
			Rectangle item = rectangle;
			if (m_dockCorner == GH_MarkovWidgetDock.BottomLeft || m_dockCorner == GH_MarkovWidgetDock.TopLeft)
			{
				item.X += i * IconSize;
			}
			else
			{
				item.X -= i * IconSize;
			}
			list.Add(item);
		}
		return list;
	}

	private IGH_ObjectProxy ProxyAt(Point point)
	{
		List<Rectangle> iconAreas = IconAreas;
		if (iconAreas != null)
		{
			int num = iconAreas.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				if (iconAreas[i].Contains(point))
				{
					return m_owner.MarkovSuggestions[i];
				}
			}
		}
		return null;
	}

	public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (sender.Document == null)
		{
			return GH_ObjectResponse.Ignore;
		}
		if (e.Button == MouseButtons.Left)
		{
			if (GripArea.Contains(e.ControlLocation))
			{
				m_drag_mode = GH_DragMode.grip_drag_start;
				return GH_ObjectResponse.Capture;
			}
			m_drag_proxy = ProxyAt(e.ControlLocation);
			if (m_drag_proxy != null)
			{
				m_drag_mode = GH_DragMode.item_drag_start;
				return GH_ObjectResponse.Capture;
			}
		}
		return base.RespondToMouseDown(sender, e);
	}

	public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (sender.Document == null)
		{
			return GH_ObjectResponse.Ignore;
		}
		MouseButtons button = e.Button;
		if (button == MouseButtons.Left)
		{
			if (m_drag_mode == GH_DragMode.grip_drag_start)
			{
				m_drag_mode = GH_DragMode.grip_drag;
			}
			if (m_drag_mode == GH_DragMode.grip_drag)
			{
				int num = 0;
				switch (m_dockCorner)
				{
				case GH_MarkovWidgetDock.TopLeft:
				case GH_MarkovWidgetDock.BottomLeft:
					num = e.ControlX - 3;
					break;
				case GH_MarkovWidgetDock.TopRight:
				case GH_MarkovWidgetDock.BottomRight:
					num = sender.ClientRectangle.Right - e.ControlX - 3;
					break;
				}
				IconLimit = Convert.ToInt32((double)num / (double)IconSize);
				sender.Refresh();
				return GH_ObjectResponse.Handled;
			}
			if (m_drag_mode == GH_DragMode.item_drag_start)
			{
				m_drag_mode = GH_DragMode.none;
				if (m_drag_proxy != null)
				{
					m_owner.DoDragDrop($"{m_drag_proxy.Guid}", DragDropEffects.Copy);
				}
				return GH_ObjectResponse.Release;
			}
		}
		return base.RespondToMouseMove(sender, e);
	}

	public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
	{
		if (sender.Document == null)
		{
			return GH_ObjectResponse.Ignore;
		}
		if (m_drag_mode == GH_DragMode.grip_drag)
		{
			m_drag_mode = GH_DragMode.none;
			sender.Refresh();
		}
		m_drag_mode = GH_DragMode.none;
		m_drag_proxy = null;
		return GH_ObjectResponse.Release;
	}

	public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
	{
		IGH_ObjectProxy iGH_ObjectProxy = ProxyAt(e.Point);
		if (iGH_ObjectProxy == null)
		{
			e.Title = Name;
			e.Text = Description;
			e.Icon = Icon_24x24;
		}
		else
		{
			e.Title = iGH_ObjectProxy.Desc.Name;
			e.Icon = iGH_ObjectProxy.Icon;
		}
	}

	public override void AppendToMenu(ToolStripDropDownMenu menu)
	{
		base.AppendToMenu(menu);
		GH_DocumentObject.Menu_AppendSeparator(menu);
		GH_DocumentObject.Menu_AppendItem(menu, "Top Left", Menu_DockTopLeft, Res_ContextMenu.Markov_TopLeft_16x16, enabled: true, m_dockCorner == GH_MarkovWidgetDock.TopLeft);
		GH_DocumentObject.Menu_AppendItem(menu, "Top Right", Menu_DockTopRight, Res_ContextMenu.Markov_TopRight_16x16, enabled: true, m_dockCorner == GH_MarkovWidgetDock.TopRight);
		GH_DocumentObject.Menu_AppendItem(menu, "Bottom Left", Menu_DockBottomLeft, Res_ContextMenu.Markov_BottomLeft_16x16, enabled: true, m_dockCorner == GH_MarkovWidgetDock.BottomLeft);
		GH_DocumentObject.Menu_AppendItem(menu, "Bottom Right", Menu_DockBottomRight, Res_ContextMenu.Markov_BottomRight_16x16, enabled: true, m_dockCorner == GH_MarkovWidgetDock.BottomRight);
	}

	private void Menu_DockBottomLeft(object sender, EventArgs e)
	{
		DockCorner = GH_MarkovWidgetDock.BottomLeft;
		m_owner.Refresh();
	}

	private void Menu_DockBottomRight(object sender, EventArgs e)
	{
		DockCorner = GH_MarkovWidgetDock.BottomRight;
		m_owner.Refresh();
	}

	private void Menu_DockTopLeft(object sender, EventArgs e)
	{
		DockCorner = GH_MarkovWidgetDock.TopLeft;
		m_owner.Refresh();
	}

	private void Menu_DockTopRight(object sender, EventArgs e)
	{
		DockCorner = GH_MarkovWidgetDock.TopRight;
		m_owner.Refresh();
	}

	public override void Render(GH_Canvas Canvas)
	{
		try
		{
			if (Canvas.Document == null)
			{
				return;
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			Tracing.Assert(new Guid("{3BF3B3CC-2E2F-43f7-B9EA-368CDF99B986}"), "Render Overlay function on Markov Widget failed: " + ex2.Message);
			ProjectData.ClearProjectError();
		}
		try
		{
			Canvas.Graphics.ResetTransform();
			if (m_drag_mode == GH_DragMode.grip_drag)
			{
				RenderInternalTheoretical(Canvas.Graphics);
			}
			RenderBackGround(Canvas.Graphics);
			RenderHighlight(Canvas.Graphics);
			RenderInternal(Canvas.Graphics);
			RenderForeGround(Canvas.Graphics);
		}
		catch (Exception ex3)
		{
			ProjectData.SetProjectError(ex3);
			Exception ex4 = ex3;
			Tracing.Assert(new Guid("{EC71A022-C399-4e7f-B531-959660E9BFE4}"), "Render function on Markov Widget failed: " + ex4.Message);
			ProjectData.ClearProjectError();
		}
		finally
		{
			Canvas.Viewport.ApplyProjection(Canvas.Graphics);
		}
	}

	private void RenderBackGround(Graphics G)
	{
		Rectangle widgetArea = WidgetArea;
		if (!widgetArea.IsEmpty)
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush(widgetArea, Color.FromArgb(230, 230, 230), Color.FromArgb(150, 150, 150), LinearGradientMode.Vertical);
			linearGradientBrush.WrapMode = WrapMode.TileFlipXY;
			G.FillRectangle(linearGradientBrush, widgetArea);
			linearGradientBrush.Dispose();
		}
		Rectangle gripArea = GripArea;
		if (!gripArea.IsEmpty)
		{
			G.FillRectangle(Brushes.Gray, gripArea);
		}
	}

	private void RenderForeGround(Graphics G)
	{
		Rectangle widgetArea = WidgetArea;
		if (!widgetArea.IsEmpty)
		{
			switch (m_dockCorner)
			{
			case GH_MarkovWidgetDock.BottomLeft:
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.Right, widgetArea.Y);
				G.DrawLine(Pens.Black, widgetArea.Right, widgetArea.Y, widgetArea.Right, widgetArea.Bottom - 1);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Bottom - 1, widgetArea.Right, widgetArea.Bottom - 1);
				break;
			case GH_MarkovWidgetDock.TopLeft:
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.Right, widgetArea.Y);
				G.DrawLine(Pens.Black, widgetArea.Right, widgetArea.Y, widgetArea.Right, widgetArea.Bottom);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Bottom, widgetArea.Right, widgetArea.Bottom);
				break;
			case GH_MarkovWidgetDock.TopRight:
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.Right, widgetArea.Y);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.X, widgetArea.Bottom);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Bottom, widgetArea.Right, widgetArea.Bottom);
				break;
			case GH_MarkovWidgetDock.BottomRight:
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.Right, widgetArea.Y);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Y, widgetArea.X, widgetArea.Bottom - 1);
				G.DrawLine(Pens.Black, widgetArea.X, widgetArea.Bottom - 1, widgetArea.Right, widgetArea.Bottom - 1);
				break;
			}
		}
	}

	private void RenderHighlight(Graphics G)
	{
		Rectangle widgetArea = WidgetArea;
		if (!widgetArea.IsEmpty)
		{
			widgetArea.Height = Global_Proc.UiAdjust(10);
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush(widgetArea, Color.FromArgb(180, 255, 255, 255), Color.FromArgb(80, 255, 255, 255), LinearGradientMode.Vertical);
			linearGradientBrush.WrapMode = WrapMode.TileFlipXY;
			G.FillRectangle(linearGradientBrush, widgetArea);
			linearGradientBrush.Dispose();
		}
	}

	private void RenderInternal(Graphics G)
	{
		List<IGH_ObjectProxy> markovSuggestions = base.Owner.MarkovSuggestions;
		if (markovSuggestions == null || markovSuggestions.Count == 0)
		{
			return;
		}
		List<Rectangle> iconAreas = IconAreas;
		if (iconAreas == null || iconAreas.Count == 0)
		{
			return;
		}
		int num = Math.Min(markovSuggestions.Count, iconAreas.Count) - 1;
		for (int i = 0; i <= num; i++)
		{
			Bitmap bitmap = markovSuggestions[i].Icon;
			if (bitmap == null)
			{
				bitmap = Res_ObjectIcons.Obj_Unknown_24x24;
			}
			GH_GraphicsUtil.RenderIcon(G, iconAreas[i], bitmap);
		}
	}

	private void RenderInternalTheoretical(Graphics G)
	{
		if (m_iconLimit > base.Owner.MarkovSuggestions.Count)
		{
			List<Rectangle> list = CreateIconAreas(m_iconLimit);
			Rectangle rect = Rectangle.Union(list[0], list[list.Count - 1]);
			switch (m_dockCorner)
			{
			case GH_MarkovWidgetDock.TopLeft:
			case GH_MarkovWidgetDock.BottomLeft:
				rect.Width += GripSize;
				break;
			case GH_MarkovWidgetDock.TopRight:
			case GH_MarkovWidgetDock.BottomRight:
				rect.X -= GripSize;
				rect.Width += GripSize;
				break;
			}
			Pen pen = new Pen(Color.Black);
			pen.DashPattern = new float[2] { 2f, 2f };
			HatchBrush hatchBrush = new HatchBrush(HatchStyle.Percent50, Color.FromArgb(150, Color.White), Color.Transparent);
			G.FillRectangle(hatchBrush, rect);
			G.DrawRectangle(pen, rect);
			pen.Dispose();
			hatchBrush.Dispose();
		}
	}
}
