using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Ribbon;
using Grasshopper.Kernel;
using Motion.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Motion.Toolbar
{
    public class MotionSenderSettings : MotionToolbarButton
    {
        protected override int ToolbarOrder => 101;
        private ToolStripButton button;
        private bool isActive = false;
        public static int FramesPerSecond { get; private set; } = 60; // 默认每秒60帧
        public static string DoubleClickGraphType { get; private set; } = "Graph Mapper";

        public MotionSenderSettings()
        {
        }

        private void AddMotionSliderSettingsButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
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
            AddMotionSliderSettingsButton();
        }

        private void Instantiate()
        {
            button.Name = "Motion Sender Settings";
            button.Size = new System.Drawing.Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Resources.MotionSliderSettingsButton; // 需要添加对应的图标
            button.ToolTipText = "鼠标左键：显示Slider帧数对应的时间\n鼠标右键：设置帧数及Graph组件类型";
            button.Click += LeftClickButton;
            button.MouseDown += RightClickButton;
        }

        private void LeftClickButton(object sender, EventArgs e)
        {
            OpenMotionSenderTimeTextShowing();
            ChangeButtonBackgroundColor();
        }

        private void OpenMotionSenderTimeTextShowing()
        {
            isActive = !isActive;
        }

        private void ChangeButtonBackgroundColor()
        {
            button.BackColor = isActive ? Color.Orange : Color.FromArgb(255, 255, 255);
        }

        private void RightClickButton(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                List<string> loadedGraph = new List<string>();
                loadedGraph = InitializeMotionSenderDoubleClickGraph();
                MotionSenderSettingsWindow settingsWindow = new MotionSenderSettingsWindow(loadedGraph);

                ChangeFps(settingsWindow);
                ChangeGraphType(settingsWindow);
                SetWindowPositionNearButton(settingsWindow);

                settingsWindow.ShowDialog();
            }
        }

        private void SetWindowPositionNearButton(MotionSenderSettingsWindow settingsWindow)
        {
            // 将窗口位置设置在按钮附近
            var screenPoint = button.Owner.PointToScreen(button.Bounds.Location);
            settingsWindow.Left = screenPoint.X;
            settingsWindow.Top = screenPoint.Y + button.Height;
        }

        private void ChangeFps(MotionSenderSettingsWindow settingsWindow)
        {
            settingsWindow.CurrentFPS = FramesPerSecond;
            settingsWindow.FPSChanged += (fps) =>
            {
                FramesPerSecond = fps;
            };
        }

        private void ChangeGraphType(MotionSenderSettingsWindow settingsWindow)
        {
            settingsWindow.CurrentGraphType = DoubleClickGraphType;
            settingsWindow.GraphTypeChanged += (graphType) =>
            {
                DoubleClickGraphType = graphType;
            };
        }

        private List<string> InitializeMotionSenderDoubleClickGraph()
        {
            var doc = Instances.ActiveCanvas.Document;
            if (doc == null) return null;
            List<string> loadedGraphPluginNameList = new List<string>();

            var values = new HashSet<string>();
            GH_DocumentEditor editor = Instances.DocumentEditor;
            Type editorType = editor.GetType();
            PropertyInfo ribbonProperty = editorType.GetProperty("Ribbon", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            object ribbonObj = null;
            if (ribbonProperty == null) return null;
            try
            {
                ribbonObj = ribbonProperty.GetValue(editor);
                GH_Ribbon ghRibbon = (GH_Ribbon)ribbonObj;


                string vrayGraphName = "V-Ray Graph";
                string richedGraphName = "Rich Graph Mapper";
                string graphMapperPlusName = "Graph-Mapper +";
                string defaultGraphName = "Graph Mapper";

                foreach (var tab in ghRibbon.Tabs)
                {
                    if (tab.NameFull != "V-Ray" && tab.NameFull != "Maths" && tab.NameFull != "Heteroptera" && tab.NameFull != "Params") continue;
                    foreach (var panel in tab.Panels)
                    {
                        if (panel.Name != "Render" && panel.Name != "Input" && panel.Name != "Util" && panel.Name != "Maths") continue;
                        foreach (var item in panel.AllItems)
                        {
                            bool isGraphPlugin = item.Proxy.Desc.Name == vrayGraphName
                                || item.Proxy.Desc.Name == richedGraphName
                                || item.Proxy.Desc.Name == graphMapperPlusName
                                || item.Proxy.Desc.Name == defaultGraphName;
                            if (!isGraphPlugin) continue;
                            loadedGraphPluginNameList.Add(item.Proxy.Desc.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"访问GH_Ribbon时出错:{ex.Message}");
            }
            return loadedGraphPluginNameList;
        }
        public static double ConvertSecondsToFrames(double seconds)
        {
            return seconds * FramesPerSecond;
        }

        public static bool IsSecondsInputMode()
        {
            ToolStrip customToolbar = CustomMotionToolbar.customMotionToolbar;
            ToolStrip targetToolbar;

            if (customToolbar.Items.Count == 0)
            {
                // 如果位置是 OnToolbar，检查 Grasshopper 的工具栏
                Type typeFromHandle = typeof(GH_DocumentEditor);
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                FieldInfo field = typeFromHandle.GetField("_CanvasToolbar", bindingAttr);
                object objectValue = RuntimeHelpers.GetObjectValue(field.GetValue(Instances.DocumentEditor));
                targetToolbar = objectValue as ToolStrip;
            }
            else
            {
                // 否则，检查 CustomMotionToolbar
                targetToolbar = customToolbar;
            }

            if (targetToolbar == null) return false;

            foreach (ToolStripItem item in targetToolbar.Items)
            {
                if (item.Name == "Motion Sender Settings" && item is ToolStripButton button)
                {
                    var settings = button.Tag as MotionSenderSettings;
                    return settings?.isActive ?? false;
                }
            }

            return false;
        }
    }
}