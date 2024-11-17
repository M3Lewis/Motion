// 警告：某些程序集引用无法自动解析。这可能会导致某些部分反编译错误，
// 例如属性 getter/setter 访问。要获得最佳反编译结果，请手动将缺少的引用添加到加载的程序集列表中。
// Grasshopper.GUI.GH_ScribblePropertiesDialog
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using Microsoft.VisualBasic.CompilerServices;
using Rhino;

/// <exclude />
[DesignerGenerated]
public class GH_ScribblePropertiesDialog : Form
{
	private IContainer components;

	private FontStyle m_style;

	[field: AccessedThroughProperty("pnlButtons")]
	internal virtual Panel pnlButtons
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("btnOK")]
	internal virtual Button btnOK
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("btnCancel")]
	internal virtual Button btnCancel
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("tblControls")]
	internal virtual TableLayoutPanel tblControls
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("lblText")]
	internal virtual Label lblText
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("lblSize")]
	internal virtual Label lblSize
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("txtText")]
	internal virtual TextBox txtText
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	[field: AccessedThroughProperty("lblFont")]
	internal virtual Label lblFont
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	internal virtual LinkLabel lnkFont
	{
		[CompilerGenerated]
		get
		{
			return _lnkFont;
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		[CompilerGenerated]
		set
		{
			LinkLabelLinkClickedEventHandler value2 = lnkFont_LinkClicked;
			LinkLabel linkLabel = _lnkFont;
			if (linkLabel != null)
			{
				linkLabel.LinkClicked -= value2;
			}
			_lnkFont = value;
			linkLabel = _lnkFont;
			if (linkLabel != null)
			{
				linkLabel.LinkClicked += value2;
			}
		}
	}

	[field: AccessedThroughProperty("SliderSize")]
	internal virtual GH_Slider SliderSize
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	public GH_ScribblePropertiesDialog()
	{
		base.Load += GH_ScribblePropertiesDialog_Load;
		base.KeyDown += GH_ScribblePropertiesDialog_KeyDown;
		m_style = FontStyle.Regular;
		InitializeComponent();
	}

	[DebuggerNonUserCode]
	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	[System.Diagnostics.DebuggerStepThrough]
	private void InitializeComponent()
	{
		this.pnlButtons = new System.Windows.Forms.Panel();
		this.btnOK = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		this.tblControls = new System.Windows.Forms.TableLayoutPanel();
		this.lblFont = new System.Windows.Forms.Label();
		this.lblSize = new System.Windows.Forms.Label();
		this.lblText = new System.Windows.Forms.Label();
		this.txtText = new System.Windows.Forms.TextBox();
		this.lnkFont = new System.Windows.Forms.LinkLabel();
		this.SliderSize = new Grasshopper.GUI.GH_Slider();
		this.pnlButtons.SuspendLayout();
		this.tblControls.SuspendLayout();
		base.SuspendLayout();
		this.pnlButtons.Controls.Add(this.btnOK);
		this.pnlButtons.Controls.Add(this.btnCancel);
		this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.pnlButtons.Location = new System.Drawing.Point(3, 202);
		this.pnlButtons.Name = "pnlButtons";
		this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
		this.pnlButtons.Size = new System.Drawing.Size(333, 32);
		this.pnlButtons.TabIndex = 0;
		this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.btnOK.Dock = System.Windows.Forms.DockStyle.Right;
		this.btnOK.Location = new System.Drawing.Point(173, 8);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(80, 24);
		this.btnOK.TabIndex = 1;
		this.btnOK.Text = "OK";
		this.btnOK.UseVisualStyleBackColor = true;
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
		this.btnCancel.Location = new System.Drawing.Point(253, 8);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(80, 24);
		this.btnCancel.TabIndex = 0;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.tblControls.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
		this.tblControls.ColumnCount = 2;
		this.tblControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50f));
		this.tblControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tblControls.Controls.Add(this.lblFont, 0, 2);
		this.tblControls.Controls.Add(this.lblSize, 0, 1);
		this.tblControls.Controls.Add(this.lblText, 0, 0);
		this.tblControls.Controls.Add(this.txtText, 1, 0);
		this.tblControls.Controls.Add(this.lnkFont, 1, 2);
		this.tblControls.Controls.Add(this.SliderSize, 1, 1);
		this.tblControls.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tblControls.Location = new System.Drawing.Point(3, 3);
		this.tblControls.Name = "tblControls";
		this.tblControls.RowCount = 3;
		this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26f));
		this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26f));
		this.tblControls.Size = new System.Drawing.Size(333, 199);
		this.tblControls.TabIndex = 1;
		this.lblFont.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lblFont.Location = new System.Drawing.Point(1, 172);
		this.lblFont.Margin = new System.Windows.Forms.Padding(0);
		this.lblFont.Name = "lblFont";
		this.lblFont.Size = new System.Drawing.Size(50, 26);
		this.lblFont.TabIndex = 4;
		this.lblFont.Text = "Font";
		this.lblFont.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.lblSize.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lblSize.Location = new System.Drawing.Point(1, 145);
		this.lblSize.Margin = new System.Windows.Forms.Padding(0);
		this.lblSize.Name = "lblSize";
		this.lblSize.Size = new System.Drawing.Size(50, 26);
		this.lblSize.TabIndex = 2;
		this.lblSize.Text = "Size";
		this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.lblText.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lblText.Location = new System.Drawing.Point(1, 1);
		this.lblText.Margin = new System.Windows.Forms.Padding(0);
		this.lblText.Name = "lblText";
		this.lblText.Size = new System.Drawing.Size(50, 143);
		this.lblText.TabIndex = 0;
		this.lblText.Text = "Text";
		this.lblText.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.txtText.Dock = System.Windows.Forms.DockStyle.Fill;
		this.txtText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.txtText.Location = new System.Drawing.Point(55, 4);
		this.txtText.Multiline = true;
		this.txtText.Name = "txtText";
		this.txtText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		this.txtText.Size = new System.Drawing.Size(274, 137);
		this.txtText.TabIndex = 1;
		this.lnkFont.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lnkFont.Location = new System.Drawing.Point(57, 172);
		this.lnkFont.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
		this.lnkFont.Name = "lnkFont";
		this.lnkFont.Size = new System.Drawing.Size(275, 26);
		this.lnkFont.TabIndex = 5;
		this.lnkFont.TabStop = true;
		this.lnkFont.Text = "Font";
		this.lnkFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.SliderSize.ControlEdgeColour = System.Drawing.SystemColors.ControlText;
		this.SliderSize.ControlShadowColour = System.Drawing.Color.FromArgb(30, 0, 0, 0);
		this.SliderSize.DecimalPlaces = 2;
		this.SliderSize.Dock = System.Windows.Forms.DockStyle.Fill;
		this.SliderSize.DrawControlBorder = false;
		this.SliderSize.DrawControlShadows = false;
		this.SliderSize.FormatMask = "{0}";
		this.SliderSize.GripBottomColour = System.Drawing.SystemColors.Window;
		this.SliderSize.GripDisplay = Grasshopper.GUI.Base.GH_SliderGripDisplay.Numeric;
		this.SliderSize.GripEdgeColour = System.Drawing.SystemColors.ControlText;
		this.SliderSize.GripTopColour = System.Drawing.SystemColors.Window;
		this.SliderSize.Location = new System.Drawing.Point(52, 145);
		this.SliderSize.Margin = new System.Windows.Forms.Padding(0);
		this.SliderSize.Maximum = new decimal(new int[4] { 100, 0, 0, 0 });
		this.SliderSize.Minimum = new decimal(new int[4] { 5, 0, 0, 0 });
		this.SliderSize.Name = "SliderSize";
		this.SliderSize.Padding = new System.Windows.Forms.Padding(2);
		this.SliderSize.RailBrightColour = System.Drawing.SystemColors.ControlLightLight;
		this.SliderSize.RailDarkColour = System.Drawing.SystemColors.ControlDark;
		this.SliderSize.RailDisplay = Grasshopper.GUI.Base.GH_SliderRailDisplay.Simple;
		this.SliderSize.RailEmptyColour = System.Drawing.SystemColors.Control;
		this.SliderSize.RailFullColour = System.Drawing.SystemColors.Highlight;
		this.SliderSize.ShadowSize = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.SliderSize.ShowTextInputOnDoubleClick = true;
		this.SliderSize.ShowTextInputOnKeyDown = true;
		this.SliderSize.Size = new System.Drawing.Size(280, 26);
		this.SliderSize.TabIndex = 6;
		this.SliderSize.TextColour = System.Drawing.SystemColors.ControlText;
		this.SliderSize.TickCount = 8;
		this.SliderSize.TickDisplay = Grasshopper.GUI.Base.GH_SliderTickDisplay.Simple;
		this.SliderSize.TickFrequency = 5;
		this.SliderSize.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Integer;
		this.SliderSize.Value = new decimal(new int[4] { 20, 0, 0, 0 });
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(339, 237);
		base.Controls.Add(this.tblControls);
		base.Controls.Add(this.pnlButtons);
		base.KeyPreview = true;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "GH_ScribblePropertiesDialog";
		base.Padding = new System.Windows.Forms.Padding(3);
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Scribble Properties";
		this.pnlButtons.ResumeLayout(false);
		this.tblControls.ResumeLayout(false);
		this.tblControls.PerformLayout();
		base.ResumeLayout(false);
	}

	private void GH_ScribblePropertiesDialog_Load(object sender, EventArgs e)
	{
		GH_WindowsControlUtil.FixTextRenderingDefault(base.Controls);
		txtText.Select();
		txtText.SelectAll();
	}

	public void SetUserFont(Font f)
	{
		SliderSize.Value = Convert.ToDecimal(f.Size);
		lnkFont.Text = f.FontFamily.Name;
		m_style = f.Style;
	}

	public Font GetUserFont()
	{
		return GH_FontServer.NewFont(lnkFont.Text, Convert.ToSingle(SliderSize.Value), m_style);
	}

	private void GH_ScribblePropertiesDialog_KeyDown(object sender, KeyEventArgs e)
	{
		switch (e.KeyCode)
		{
		case Keys.Cancel:
		case Keys.Escape:
			base.DialogResult = DialogResult.Cancel;
			break;
		case Keys.Return:
			if (Control.ModifierKeys == Keys.Shift)
			{
				base.DialogResult = DialogResult.OK;
			}
			break;
		}
	}

	private void lnkFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		FontDialog fontDialog = new FontDialog
		{
			AllowVerticalFonts = false,
			AllowSimulations = false,
			FontMustExist = true,
			ShowApply = false,
			ShowColor = false,
			ShowEffects = false,
			ShowHelp = false
		};
		try
		{
			if (fontDialog.ShowDialog(this) == DialogResult.OK)
			{
				lnkFont.Text = fontDialog.Font.FontFamily.Name;
				m_style = fontDialog.Font.Style;
			}
		}
		catch (Exception ex)
		{
			ProjectData.SetProjectError(ex);
			Exception ex2 = ex;
			RhinoApp.WriteLine("Something went wrong during font selection:");
			RhinoApp.WriteLine("  " + ex2.Message);
			ProjectData.ClearProjectError();
		}
	}
}
