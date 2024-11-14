using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Motion.Toolbar
{
    public class NamedViewSwitch : GH_AssemblyPriority
    {
        private ToolStripButton button;
        private List<string> namedViews = new List<string>();
        private int currentViewIndex = 0;
        private bool isActive = true;

        public NamedViewSwitch()
        {
            
        }
        private void AddNamedViewSwitchButton()
        {
            ToolStrip toolbar = (ToolStrip)Grasshopper.Instances.DocumentEditor.Controls[0].Controls[1];

            button = new ToolStripButton();
            Instantiate();
            LoadNamedViews();
            toolbar.Items.Add(button);
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;

            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor editor = Instances.DocumentEditor;

            //Check if DocumentEditor was instantiated
            if (editor == null) return;
            AddNamedViewSwitchButton();
        }

        private void Instantiate()
        {
            // 配置按钮
            button.Name = "View Switch";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Image.FromFile("C:\\Users\\M3\\Nutstore\\1\\我的坚果云\\VisualStudioGHLibrary\\Motion\\Motion\\Icons\\NamedViewSwitch.png");
            button.ToolTipText = "Switch between named views using +/- keys";
            button.Click += ClickedButton;
        }

        private void LoadNamedViews()
        {
            namedViews.Clear();
            if (Instances.ActiveCanvas?.Document != null)
            {
                var viewList = Instances.ActiveCanvas.Document.Properties.ViewList;
                foreach (var view in viewList)
                {
                    namedViews.Add(view.Name);
                }
            }
        }

        private void ClickedButton(object sender, EventArgs e)
        {
            isActive = !isActive;

            if (isActive)
            {
                //button.Image = Properties.Resources.YourActiveIcon; // 激活状态图标
                button.BackColor = Color.FromArgb(41, 141, 173); // 激活状态背景色
                Instances.DocumentEditor.KeyDown += KeyDownEventHandler;
            }
            else
            {
                //button.Image = Properties.Resources.YourIcon; // 默认图标
                button.BackColor = Color.FromArgb(255, 255, 255); // 默认背景色
                Instances.DocumentEditor.KeyDown -= KeyDownEventHandler;
            }
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {
            LoadNamedViews();
            if (!isActive) return;

            if (e.KeyCode == Keys.Oemplus)
            {
                currentViewIndex = (currentViewIndex + 1) % namedViews.Count;
                SwitchToView(currentViewIndex);
            }
            else if (e.KeyCode == Keys.OemMinus)
            {
                currentViewIndex = (currentViewIndex - 1 + namedViews.Count) % namedViews.Count;
                SwitchToView(currentViewIndex);
            }
        }

        private void SwitchToView(int index)
        {
            try
            {
                if (Instances.ActiveCanvas?.Document == null) return;

                var views = Instances.ActiveCanvas.Document.Properties.ViewList;
                if (index >= 0 && index < views.Count)
                {
                    // 直接使用视图列表中的 GH_NamedView 对象
                    GH_NamedView namedView = views[index];

                    // 使用带动画效果的切换(length代表切换时间)
                    namedView.SetToViewport(Instances.ActiveCanvas, 200);

                    // 或者使用即时切换
                    // namedView.SetToViewport(Instances.ActiveCanvas);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error switching view: {ex.Message}");
            }
        }
    }
}
