using Grasshopper;
using Grasshopper.GUI;
using Motion.Widgets;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Motion.SettingsControls
{
    public class TimelineWidgetSettingFrontEnd : UserControl
    {
        private IContainer components;
        private CheckBox _checkShow;
        private ComboBox _comboboxDock;

        internal virtual CheckBox checkShow
        {
            get
            {
                return _checkShow;
            }
            set
            {
                EventHandler value2 = checkShow_CheckedChanged;
                CheckBox checkBox = _checkShow;
                if (checkBox != null)
                {
                    checkBox.CheckedChanged -= value2;
                }
                _checkShow = value;
                checkBox = _checkShow;
                if (checkBox != null)
                {
                    checkBox.CheckedChanged += value2;
                }
            }
        }

        internal virtual TableLayoutPanel TableLayoutPanel1 { get; set; }

        internal virtual Label labelSide { get; set; }

        internal virtual ComboBox comboboxDock
        {
            get
            {
                return _comboboxDock;
            }
            set
            {
                EventHandler value2 = comboboxDock_SelectedIndexChanged;
                ComboBox comboBox = _comboboxDock;
                if (comboBox != null)
                {
                    comboBox.SelectedIndexChanged -= value2;
                }
                _comboboxDock = value;
                comboBox = _comboboxDock;
                if (comboBox != null)
                {
                    comboBox.SelectedIndexChanged += value2;
                }
            }
        }

        internal virtual ToolTip ToolTip { get; set; }

        internal virtual GH_Label GH_Label1 { get; set; }

        public TimelineWidgetSettingFrontEnd()
        {
            base.Load += TimelineWidgetSettingFrontEnd_Load;
            base.HandleDestroyed += TimelineWidgetSettingFrontEnd_HandleDestroyed;
            InitializeComponent();
        }

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

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.checkShow = new System.Windows.Forms.CheckBox();
            this.TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.GH_Label1 = new Grasshopper.GUI.GH_Label();
            this.labelSide = new System.Windows.Forms.Label();
            this.comboboxDock = new System.Windows.Forms.ComboBox();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.TableLayoutPanel1.SuspendLayout();
            base.SuspendLayout();
            this.TableLayoutPanel1.SetColumnSpan(this.checkShow, 2);
            this.checkShow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkShow.Location = new System.Drawing.Point(52, 0);
            this.checkShow.Margin = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.checkShow.Name = "checkShow";
            this.checkShow.Size = new System.Drawing.Size(327, 32);
            this.checkShow.TabIndex = 0;
            this.checkShow.Text = "Show Timeline widget";
            this.checkShow.UseVisualStyleBackColor = true;
            this.TableLayoutPanel1.ColumnCount = 3;
            this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
            this.TableLayoutPanel1.Controls.Add(this.GH_Label1, 0, 0);
            this.TableLayoutPanel1.Controls.Add(this.labelSide, 1, 1);
            this.TableLayoutPanel1.Controls.Add(this.comboboxDock, 2, 1);
            this.TableLayoutPanel1.Controls.Add(this.checkShow, 1, 0);
            this.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.TableLayoutPanel1.Margin = new System.Windows.Forms.Padding(12, 12, 12, 12);
            this.TableLayoutPanel1.Name = "TableLayoutPanel1";
            this.TableLayoutPanel1.RowCount = 4;
            this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
            this.TableLayoutPanel1.Size = new System.Drawing.Size(379, 140);
            this.TableLayoutPanel1.TabIndex = 1;
            this.GH_Label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GH_Label1.Image = null;
            this.GH_Label1.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.GH_Label1.Location = new System.Drawing.Point(0, 0);
            this.GH_Label1.Margin = new System.Windows.Forms.Padding(0);
            this.GH_Label1.Name = "GH_Label1";
            this.GH_Label1.Size = new System.Drawing.Size(32, 32);
            this.GH_Label1.TabIndex = 7;
            this.GH_Label1.Text = null;
            this.GH_Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelSide.Location = new System.Drawing.Point(32, 32);
            this.labelSide.Margin = new System.Windows.Forms.Padding(0);
            this.labelSide.Name = "labelSide";
            this.labelSide.Size = new System.Drawing.Size(150, 32);
            this.labelSide.TabIndex = 1;
            this.labelSide.Text = "Docking Side";
            this.labelSide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.comboboxDock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboboxDock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboboxDock.FormattingEnabled = true;
            this.comboboxDock.Items.AddRange(new object[2] { "Top", "Bottom" });
            this.comboboxDock.Location = new System.Drawing.Point(182, 32);
            this.comboboxDock.Margin = new System.Windows.Forms.Padding(0);
            this.comboboxDock.Name = "comboboxDock";
            this.comboboxDock.Size = new System.Drawing.Size(197, 33);
            this.comboboxDock.TabIndex = 3;

            this.ToolTip.AutoPopDelay = 32000;
            this.ToolTip.InitialDelay = 500;
            this.ToolTip.ReshowDelay = 100;
            base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.Controls.Add(this.TableLayoutPanel1);
            base.Margin = new System.Windows.Forms.Padding(12, 12, 12, 12);
            base.Name = "TimelineWidgetSettingFrontEnd";
            base.Size = new System.Drawing.Size(379, 120);
            this.TableLayoutPanel1.ResumeLayout(false);
            base.ResumeLayout(false);
        }

        private void TimelineWidgetSettingFrontEnd_Load(object sender, EventArgs e)
        {
            TimelineSideChanged();
            TimelineVisibleChanged();
            TimelineWidget.WidgetVisibleChanged += TimelineVisibleChanged;
            TimelineWidget.DockSideChanged += TimelineSideChanged;
        }

        private void TimelineWidgetSettingFrontEnd_HandleDestroyed(object sender, EventArgs e)
        {
            TimelineWidget.WidgetVisibleChanged -= TimelineVisibleChanged;
            TimelineWidget.DockSideChanged -= TimelineSideChanged;
        }

        private void TimelineVisibleChanged()
        {
            checkShow.Checked = TimelineWidget.SharedVisible;
        }

        private void TimelineSideChanged()
        {
            switch (TimelineWidget.DockSide)
            {
                case TimelineWidgetDock.Top:
                    comboboxDock.SelectedIndex = 0;
                    break;

                case TimelineWidgetDock.Bottom:
                    comboboxDock.SelectedIndex = 1;
                    break;
            }
        }

        private void checkShow_CheckedChanged(object sender, EventArgs e)
        {
            TimelineWidget.SharedVisible = checkShow.Checked;
            Instances.RedrawCanvas();
        }

        private void comboboxDock_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboboxDock.SelectedIndex)
            {
                case 0:
                    TimelineWidget.DockSide = TimelineWidgetDock.Top;
                    break;

                case 1:
                    TimelineWidget.DockSide = TimelineWidgetDock.Bottom;
                    break;
            }
            Instances.RedrawCanvas();
        }
    }
}