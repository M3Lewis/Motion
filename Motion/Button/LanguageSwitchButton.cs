using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using Motion.General;

namespace Motion.Toolbar
{
    public class LanguageSwitchButton : MotionToolbarButton
    {
        public override int ToolbarOrder => 200; // Place it at the end of the toolbar
        private ToolStripButton button;

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;
            if (editor == null) return;
            AddLanguageSwitchButton();
        }

        private void AddLanguageSwitchButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToToolbars(button);

            // Subscribe to language change to update our own icon
            LanguageManager.LanguageChanged += UpdateIconAndTooltip;
        }

        private void Instantiate()
        {
            button.Name = "Language Switcher";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = CreateLanguageIcon();
            button.ToolTipText = LanguageManager.GetString("Button.LanguageSwitch.Tooltip", "切换语言 (English / 中文)");
            button.Click += ToggleLanguage;
        }

        private void ToggleLanguage(object sender, EventArgs e)
        {
            if (LanguageManager.CurrentLanguage == Language.ZH)
            {
                LanguageManager.CurrentLanguage = Language.EN;
            }
            else
            {
                LanguageManager.CurrentLanguage = Language.ZH;
            }
            
            // Trigger update of all UI elements
            LanguageManager.UpdateAllUI();
        }

        private void UpdateIconAndTooltip()
        {
            if (button != null)
            {
                button.Image = CreateLanguageIcon();
                button.ToolTipText = LanguageManager.GetString("Button.LanguageSwitch.Tooltip", "切换语言 (English / 中文)");
            }
        }

        public override void UpdateLanguage()
        {
            UpdateIconAndTooltip();
        }

        private Image CreateLanguageIcon()
        {
            Bitmap bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Draw a nice rounded rectangle border
                using (Pen pen = new Pen(Color.CornflowerBlue, 1.5f))
                {
                    g.DrawRectangle(pen, 2, 2, 20, 20);
                }

                string txt = LanguageManager.CurrentLanguage == Language.ZH ? "中" : "EN";
                float fontSize = LanguageManager.CurrentLanguage == Language.ZH ? 9.5f : 7.5f;
                using (Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                {
                    SizeF size = g.MeasureString(txt, font);
                    g.DrawString(txt, font, brush, (24 - size.Width) / 2f, (24 - size.Height) / 2f + 0.5f);
                }
            }
            return bmp;
        }
    }
}
