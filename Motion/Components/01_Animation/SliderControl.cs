using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using Point = System.Drawing.Point;
public class SliderControlComponent : GH_Component
{
    private GH_NumberSlider connectedSlider;

    public override Guid ComponentGuid => new Guid("1e5547b0-8195-49ef-b0a4-00eb2f9beb60");

    public SliderControlComponent()
        : base("Slider Control", "SldCtrl",
            "Control a slider through a floating window",
            "Category", "Subcategory")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Slider", "S", "Connect a slider here", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        // No outputs needed
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Get the connected slider
        if (Params.Input[0].Sources.Count > 0)
        {
            connectedSlider = Params.Input[0].Sources[0] as GH_NumberSlider;
        }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendItem(menu, "Open Control Window", OpenControlWindow);
    }

    public override void CreateAttributes()
    {
        m_attributes = new CustomComponentAttributes(this);
    }

    public void OpenControlWindow(object sender, EventArgs e)
    {
        if (connectedSlider == null)
        {
            MessageBox.Show("Please connect a slider first!");
            return;
        }

        ControlForm form = new ControlForm(connectedSlider);
        form.Show();
    }
}

public class CustomComponentAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
{
    public CustomComponentAttributes(IGH_Component component)
        : base(component)
    {
    }

    public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        SliderControlComponent comp = Owner as SliderControlComponent;
        if (comp != null)
        {
            comp.OpenControlWindow(null, null);
        }
        return GH_ObjectResponse.Handled;
    }
}

public class ControlForm : Form
{
    private TrackBar trackBar;
    private NumericUpDown numericUpDown;
    private GH_NumberSlider targetSlider;
    private Button btnIncrement;
    private Button btnDecrement;
    private Button btnMax;
    private Button btnMin;

    public ControlForm(GH_NumberSlider slider)
    {
        targetSlider = slider;
        InitializeComponents();
        UpdateControlValues();
        this.TopMost = true;
    }

    private void InitializeComponents()
    {
        this.Size = new Size(300, 200); // Increased size to accommodate buttons
        this.Text = "Slider Control";

        trackBar = new TrackBar();
        trackBar.Dock = DockStyle.Top;
        trackBar.Minimum = (int)(targetSlider.Slider.Minimum * 100);
        trackBar.Maximum = (int)(targetSlider.Slider.Maximum * 100);
        trackBar.Value = (int)(targetSlider.Slider.Value * 100);
        trackBar.ValueChanged += TrackBar_ValueChanged;

        numericUpDown = new NumericUpDown();
        numericUpDown.Dock = DockStyle.Bottom;
        numericUpDown.Minimum = (decimal)targetSlider.Slider.Minimum;
        numericUpDown.Maximum = (decimal)targetSlider.Slider.Maximum;
        numericUpDown.Value = (decimal)targetSlider.Slider.Value;
        numericUpDown.DecimalPlaces = 2;
        numericUpDown.ValueChanged += NumericUpDown_ValueChanged;

        // Button Initialization
        btnIncrement = new Button();
        btnIncrement.Text = "+1";
        btnIncrement.Location = new Point(10, trackBar.Bottom + 10);
        btnIncrement.Click += BtnIncrement_Click;

        btnDecrement = new Button();
        btnDecrement.Text = "-1";
        btnDecrement.Location = new Point(btnIncrement.Right + 10, trackBar.Bottom + 10);
        btnDecrement.Click += BtnDecrement_Click;

        btnMax = new Button();
        btnMax.Text = "Max";
        btnMax.Location = new Point(btnDecrement.Right + 10, trackBar.Bottom + 10);
        btnMax.Click += BtnMax_Click;

        btnMin = new Button();
        btnMin.Text = "Min";
        btnMin.Location = new Point(btnMax.Right + 10, trackBar.Bottom + 10);
        btnMin.Click += BtnMin_Click;

        this.Controls.Add(trackBar);
        this.Controls.Add(numericUpDown);
        this.Controls.Add(trackBar);
        this.Controls.Add(numericUpDown);
        this.Controls.Add(btnIncrement);
        this.Controls.Add(btnDecrement);
        this.Controls.Add(btnMax);
        this.Controls.Add(btnMin);
    }

    // Button Click Event Handlers
    private void BtnIncrement_Click(object sender, EventArgs e)
    {
        SetValue(targetSlider.Slider.Value + 1);
    }

    private void BtnDecrement_Click(object sender, EventArgs e)
    {
        SetValue(targetSlider.Slider.Value - 1);
    }

    private void BtnMax_Click(object sender, EventArgs e)
    {
        SetValue(targetSlider.Slider.Maximum);
    }

    private void BtnMin_Click(object sender, EventArgs e)
    {
        SetValue(targetSlider.Slider.Minimum);
    }


    private void SetValue(decimal newValue)
    {
        // Ensure value stays within slider bounds
        newValue = Math.Max(targetSlider.Slider.Minimum, Math.Min(newValue, targetSlider.Slider.Maximum));
        targetSlider.Slider.Value = newValue;
        trackBar.Value = (int)(newValue * 100);
        numericUpDown.Value = newValue;
        targetSlider.ExpireSolution(true);
    }
    private void TrackBar_ValueChanged(object sender, EventArgs e)
    {
        double value = trackBar.Value / 100.0;
        targetSlider.Slider.Value = (decimal)value;
        numericUpDown.Value = (decimal)value;
        targetSlider.ExpireSolution(true);
    }

    private void NumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        double value = (double)numericUpDown.Value;
        targetSlider.Slider.Value = (decimal)value;
        trackBar.Value = (int)(value * 100);
        targetSlider.ExpireSolution(true);
    }

    private void UpdateControlValues()
    {
        trackBar.Value = (int)(targetSlider.Slider.Value * 100);
        numericUpDown.Value = (decimal)targetSlider.Slider.Value;
    }
}