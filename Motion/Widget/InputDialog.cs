using System;
using System.Windows.Forms;

namespace Motion.Widget
{
    internal class InputDialog : Form
    {
        private TextBox textBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;

        public string InputText => textBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponents(title, prompt, defaultValue);
        }

        private void InitializeComponents(string title, string prompt, string defaultValue)
        {
            this.Text = title;
            this.Size = new System.Drawing.Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = prompt,
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };

            textBox = new TextBox
            {
                Text = defaultValue,
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(260, 20)
            };

            okButton = new System.Windows.Forms.Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(110, 80),
                Size = new System.Drawing.Size(75, 25)
            };

            cancelButton = new System.Windows.Forms.Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(195, 80),
                Size = new System.Drawing.Size(75, 25)
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
        }
    }
}