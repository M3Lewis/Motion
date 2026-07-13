using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Ribbon;
using Grasshopper.Kernel;
using Motion.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Motion.Toolbar
{
    [Serializable]
    public class MotionSenderSettingsData
    {
        public int FramesPerSecond { get; set; }
        public string DoubleClickGraphType { get; set; }
        public bool IsActive { get; set; }  // 新增属性，用于保存按钮的active状态

        public MotionSenderSettingsData()
        {
            FramesPerSecond = 60; // 默认每秒60帧
            DoubleClickGraphType = "Graph Mapper"; // 默认图表类型
            IsActive = false; // 默认按钮状态为非激活
        }

        public MotionSenderSettingsData(int fps, string graphType, bool isActive)
        {
            FramesPerSecond = fps;
            DoubleClickGraphType = graphType;
            IsActive = isActive;
        }
    }

    public class MotionSenderSettings : MotionToolbarButton
    {
        public override int ToolbarOrder => 101;
        private ToolStripButton button;

        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Motion", "MotionSenderSettings.xml");

        private static int _framesPerSecond = 60;
        public static int FramesPerSecond
        {
            get { return _framesPerSecond; }
            private set
            {
                _framesPerSecond = value;
                SaveSettings();
            }
        }

        private bool isActive = false; // 保留实例变量用于UI状态
        // 添加静态变量存储全局状态
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

        private static string _doubleClickGraphType = "Graph Mapper";
        public static string DoubleClickGraphType
        {
            get { return _doubleClickGraphType; }
            private set
            {
                _doubleClickGraphType = value;
                SaveSettings();
            }
        }

        public MotionSenderSettings()
        {
            LoadSettings();
            isActive = _isActiveState; // 加载后同步到实例变量
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
            button.ToolTipText = General.LanguageManager.GetString("Button.MotionSenderSettings.Tooltip", "鼠标左键：显示Slider帧数对应的时间\n鼠标右键：设置帧数及Graph组件类型");
            button.Click += LeftClickButton;
            button.MouseDown += RightClickButton;

            // 设置按钮初始状态
            isActive = _isActiveState; // 确保同步
            button.BackColor = isActive ? Color.Orange : Color.FromArgb(255, 255, 255);
        }

        public override void UpdateLanguage()
        {
            if (button != null)
            {
                button.ToolTipText = General.LanguageManager.GetString("Button.MotionSenderSettings.Tooltip", "鼠标左键：显示Slider帧数对应的时间\n鼠标右键：设置帧数及Graph组件类型");
            }
        }


        private void LeftClickButton(object sender, EventArgs e)
        {
            OpenMotionSenderTimeTextShowing();
            ChangeButtonBackgroundColor();
        }

        private void OpenMotionSenderTimeTextShowing()
        {
            // 同时更新实例变量和静态变量
            isActive = !isActive;
            _isActiveState = isActive;
            SaveSettings(); // 保存当前状态
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
                _framesPerSecond = fps; // 更新值
                SaveSettings(); // 保存设置
            };
        }

        private void ChangeGraphType(MotionSenderSettingsWindow settingsWindow)
        {
            settingsWindow.CurrentGraphType = DoubleClickGraphType;
            settingsWindow.GraphTypeChanged += (graphType) =>
            {
                _doubleClickGraphType = graphType; // 更新值
                SaveSettings(); // 保存设置
            };
        }

        // 保存设置到文件
        private static void SaveSettings()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建设置对象
                MotionSenderSettingsData settings = new MotionSenderSettingsData(_framesPerSecond, _doubleClickGraphType, _isActiveState);

                // 序列化到XML
                XmlSerializer serializer = new XmlSerializer(typeof(MotionSenderSettingsData));
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    serializer.Serialize(writer, settings);
                }

                //Rhino.RhinoApp.WriteLine($"已保存Motion Sender设置: FPS={_framesPerSecond}, GraphType={_doubleClickGraphType}, IsActive={_isActiveState}");
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"保存Motion Sender设置失败: {ex.Message}");
            }
        }

        // 加载设置并应用
        private static void LoadSettings()
        {
            try
            {
                // 检查设置文件是否存在
                if (!File.Exists(SettingsFilePath))
                {
                    return; // 如果不存在，使用默认设置
                }

                // 反序列化XML
                XmlSerializer serializer = new XmlSerializer(typeof(MotionSenderSettingsData));
                MotionSenderSettingsData settings;
                using (StreamReader reader = new StreamReader(SettingsFilePath))
                {
                    settings = (MotionSenderSettingsData)serializer.Deserialize(reader);
                }

                // 应用加载的设置
                if (settings != null)
                {
                    _framesPerSecond = settings.FramesPerSecond;
                    _doubleClickGraphType = settings.DoubleClickGraphType;
                    _isActiveState = settings.IsActive;
                    //Rhino.RhinoApp.WriteLine($"已加载Motion Sender设置: FPS={_framesPerSecond}, GraphType={_doubleClickGraphType}, IsActive={_isActiveState}");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"加载Motion Sender设置失败: {ex.Message}");
            }
        }

        private List<string> InitializeMotionSenderDoubleClickGraph()
        {
            // 如果 Grasshopper 的组件服务尚未初始化，直接返回空列表
            if (Instances.ComponentServer == null) 
                return new List<string>();

            var loadedGraphPluginNameList = new List<string>();

            // 使用 HashSet 提高匹配效率（Tab 和 Panel 建议忽略大小写，名称建议精确匹配）
            var targetCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "V-Ray", "Maths", "Heteroptera", "Params" 
            };
    
            var targetSubCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "Render", "Input", "Util", "Maths" 
            };
    
            var targetGraphNames = new HashSet<string>(StringComparer.Ordinal) 
            { 
                "V-Ray Graph", 
                "Rich Graph Mapper", 
                "Graph-Mapper +", 
                "Graph Mapper" 
            };

            // 直接在内存的组件缓存中查找
            foreach (var proxy in Instances.ComponentServer.ObjectProxies)
            {
                if (proxy?.Desc == null) continue;
                if (proxy.Obsolete) continue; // 可选：过滤掉过期的老版本组件

                if (targetCategories.Contains(proxy.Desc.Category) &&
                    targetSubCategories.Contains(proxy.Desc.SubCategory) &&
                    targetGraphNames.Contains(proxy.Desc.Name))
                {
                    loadedGraphPluginNameList.Add(proxy.Desc.Name);
                }
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

            if (targetToolbar == null) return _isActiveState; // 如果找不到工具栏，返回保存的状态

            // 优先使用找到的按钮状态
            foreach (ToolStripItem item in targetToolbar.Items)
            {
                if (item.Name == "Motion Sender Settings" && item is ToolStripButton button)
                {
                    var settings = button.Tag as MotionSenderSettings;
                    if (settings != null)
                        return settings.isActive;
                }
            }

            // 如果没有找到按钮，则使用保存的状态
            return _isActiveState;
        }
    }
}