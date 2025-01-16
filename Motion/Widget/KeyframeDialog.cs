using System.Drawing;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal class KeyframeDialog : Form
    {
        public double Value { get; private set; }
        private TextBox valueTextBox;

        public KeyframeDialog(double initialValue)
        {
            this.Text = "Set Keyframe Value";
            this.Size = new Size(200, 120);
            this.StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                Text = "Value:",
                Location = new Point(10, 20),
                Size = new Size(50, 20)
            };

            valueTextBox = new TextBox
            {
                Text = initialValue.ToString(),
                Location = new Point(70, 20),
                Size = new Size(100, 20)
            };

            var okButton = new System.Windows.Forms.Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 50),
                Size = new Size(70, 25)
            };

            var cancelButton = new System.Windows.Forms.Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(100, 50),
                Size = new Size(70, 25)
            };

            this.Controls.AddRange(new Control[] { label, valueTextBox, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (double.TryParse(valueTextBox.Text, out double result))
                {
                    Value = result;
                }
                else
                {
                    e.Cancel = true;
                    MessageBox.Show("Please enter a valid number.");
                }
            }
            base.OnFormClosing(e);
        }
    }
}