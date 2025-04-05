using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Motion.Toolbar
{
    public class NamedViewSwitchData
    {
        public bool IsActive { get; set; }  // 保存按钮的active状态

        public NamedViewSwitchData()
        {
            IsActive = false; // 默认按钮状态为非激活
        }

        public NamedViewSwitchData(bool isActive)
        {
            IsActive = isActive;
        }
    }

    [Serializable]
    public class NamedViewSwitch : MotionToolbarButton
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Motion", "NamedViewSwitch.xml"); 
        protected override int ToolbarOrder => 100;
        private ToolStripButton button;
        private List<string> namedViews = new List<string>();
        private int currentViewIndex = 0;
        private bool isActive = false; // 实例变量用于UI状态

        // 静态变量存储全局状态
        private static bool _isActiveState = false;
        public static bool IsActiveState
        {
            get { return _isActiveState; }
            private set
            {
                _isActiveState = value;
                SaveSettings();
            }
        }

        public NamedViewSwitch()
        {
            LoadSettings(); // 加载设置
            isActive = _isActiveState; // 同步实例变量与静态变量
        }

        private void AddNamedViewSwitchButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            LoadNamedViews();
            AddButtonToToolbars(button);
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
            if (editor == null) return;
            AddNamedViewSwitchButton();
        }

        private void Instantiate()
        {
            button.Name = "View Switch";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.NamedViewSwitch2;
            button.ToolTipText = "打开状态下，可按 Ctrl + [+]/[-] 键在Named View之间切换";
            button.Click += ClickedButton;

            // 设置按钮初始状态
            isActive = _isActiveState; // 确保与加载的设置同步
            button.BackColor = isActive ? Color.Orange : Color.FromArgb(255, 255, 255); // 同步UI状态
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
            _isActiveState = isActive; // 同步静态变量
            UpdateButtonState(); // 更新按钮UI状态
        }

        private void UpdateButtonState()
        {
            if (isActive)
            {
                button.BackColor = Color.Orange;
                Instances.DocumentEditor.KeyDown += KeyDownEventHandler;
            }
            else
            {
                button.BackColor = Color.FromArgb(255, 255, 255);
                Instances.DocumentEditor.KeyDown -= KeyDownEventHandler;
            }
            SaveSettings(); // 保存状态
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {
            LoadNamedViews();
            if (!isActive) return;
            if (namedViews.Count <= 0)
            {
                var doc = Grasshopper.Instances.ActiveCanvas.Document;
                var canvas = Grasshopper.Instances.ActiveCanvas;
                bool isPressed = e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add);
                if (canvas != null && isPressed)
                {
                    ShowTemporaryMessage(canvas, $"请创建一个Grasshopper Named View");
                }
                return;
            }

            if (e.Control)
            {
                if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                {
                    currentViewIndex = (currentViewIndex + 1) % namedViews.Count;
                    SwitchToView(currentViewIndex);
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    currentViewIndex = (currentViewIndex - 1 + namedViews.Count) % namedViews.Count;
                    SwitchToView(currentViewIndex);
                }
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
                    GH_NamedView namedView = views[index];
                    namedView.SetToViewport(Instances.ActiveCanvas, 200);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error switching view: {ex.Message}");
            }
        }

        private void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            GH_Canvas.CanvasPrePaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                var originalTransform = g.Transform;
                g.ResetTransform();

                SizeF textSize = new SizeF(30, 30);
                float padding = 20;
                float x = textSize.Width + 300;
                float y = padding + 30;

                RectangleF textBounds = new RectangleF(x, y, textSize.Width + 300, textSize.Height + 30);
                textBounds.Inflate(6, 3);

                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();

                g.Transform = originalTransform;
            };

            canvas.CanvasPrePaintObjects += canvasRepaint;
            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPrePaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private static void SaveSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                NamedViewSwitchData settings = new NamedViewSwitchData(_isActiveState);
                XmlSerializer serializer = new XmlSerializer(typeof(NamedViewSwitchData));
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    serializer.Serialize(writer, settings);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"保存Named View Switch设置失败: {ex.Message}");
            }
        }

        private static void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return; // 使用默认设置
                }

                XmlSerializer serializer = new XmlSerializer(typeof(NamedViewSwitchData));
                NamedViewSwitchData settings;
                using (StreamReader reader = new StreamReader(SettingsFilePath))
                {
                    settings = (Motion.Toolbar.NamedViewSwitchData)serializer.Deserialize(reader);
                }

                if (settings != null)
                {
                    _isActiveState = settings.IsActive;
                    //Rhino.RhinoApp.WriteLine($"已加载Named View Switch设置: IsActive={_isActiveState}");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"加载Named View Switch设置失败: {ex.Message}");
            }
        }
    }
}