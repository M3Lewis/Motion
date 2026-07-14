using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI;

namespace Motion.General
{
    public enum Language
    {
        ZH, // Chinese
        EN  // English
    }

    public static partial class LanguageManager
    {
        private static Language _currentLanguage = Language.ZH;
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Motion", "LanguageSettings.xml"
        );

        public static event Action LanguageChanged;

        private static readonly List<WeakReference<System.Windows.Window>> ActiveWindows = new List<WeakReference<System.Windows.Window>>();

        private static readonly System.Windows.DependencyProperty OriginalTextProperty =
            System.Windows.DependencyProperty.RegisterAttached(
                "OriginalText",
                typeof(string),
                typeof(LanguageManager),
                new System.Windows.PropertyMetadata(null));

        private static string GetOrRegisterOriginalText(System.Windows.DependencyObject obj, string currentText)
        {
            if (obj == null) return currentText;
            string original = (string)obj.GetValue(OriginalTextProperty);
            if (original == null)
            {
                original = currentText ?? string.Empty;
                obj.SetValue(OriginalTextProperty, original);
            }
            return original;
        }

        public static void RegisterWindowForLocalization(System.Windows.Window window)
        {
            if (window == null) return;
            
            // Clean up dead references
            ActiveWindows.RemoveAll(wr => !wr.TryGetTarget(out _));
            
            // Add if not already registered
            if (!ActiveWindows.Exists(wr => wr.TryGetTarget(out var target) && target == window))
            {
                ActiveWindows.Add(new WeakReference<System.Windows.Window>(window));
            }
        }

        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    Save();
                    LanguageChanged?.Invoke();
                }
            }
        }

        static LanguageManager()
        {
            Load();
        }

        public static string GetString(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            if (Translations.TryGetValue(key, out var pair))
            {
                return _currentLanguage == Language.EN ? pair.EN : pair.ZH;
            }
            return fallback;
        }

        public static string GetParamString(string key, string fallback)
        {
            if (Translations.TryGetValue(key, out var pair))
            {
                return _currentLanguage == Language.EN ? pair.EN : pair.ZH;
            }
            string genericKey = "Param." + fallback;
            if (Translations.TryGetValue(genericKey, out pair))
            {
                return _currentLanguage == Language.EN ? pair.EN : pair.ZH;
            }
            return fallback;
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(SettingsPath);
                    XmlNode node = doc.SelectSingleNode("//Language");
                    if (node != null && Enum.TryParse(node.InnerText, out Language lang))
                    {
                        _currentLanguage = lang;
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to detection on failure
            }

            // Auto-detect system language
            DetectSystemLanguage();
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("Settings");
                doc.AppendChild(root);
                XmlElement langElem = doc.CreateElement("Language");
                langElem.InnerText = _currentLanguage.ToString();
                root.AppendChild(langElem);
                doc.Save(SettingsPath);
            }
            catch
            {
                // Suppress save errors
            }
        }

        private static void DetectSystemLanguage()
        {
            try
            {
                string cultureName = CultureInfo.CurrentUICulture.Name;
                if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    _currentLanguage = Language.ZH;
                }
                else
                {
                    _currentLanguage = Language.EN;
                }
            }
            catch
            {
                _currentLanguage = Language.EN;
            }
        }

        // Generic WPF tree localizer that handles both logical tree and visual tree (when loaded)
        public static void LocalizeWindow(System.Windows.Window window)
        {
            if (window == null) return;
            
            string windowName = window.GetType().Name;
            RegisterWindowForLocalization(window);
            
            // Localize title
            string origTitle = GetOrRegisterOriginalText(window, window.Title);
            string titleKey = $"UI.{windowName}.Title";
            window.Title = GetString(titleKey, origTitle);

            // Localize logical tree immediately in constructor (captures elements in XAML)
            LocalizeLogicalElement(window, windowName);

            // Also register to Loaded to localize the visual tree once template is fully generated
            window.Loaded += (s, e) =>
            {
                LocalizeVisualElement(window, windowName);
            };
        }

        private static void LocalizeLogicalElement(System.Windows.DependencyObject element, string windowName)
        {
            if (element == null) return;

            LocalizeElementNode(element, windowName);

            var children = new List<object>();
            var logicalChildren = System.Windows.LogicalTreeHelper.GetChildren(element);
            if (logicalChildren != null)
            {
                foreach (object child in logicalChildren)
                {
                    if (child != null)
                    {
                        children.Add(child);
                    }
                }
            }

            foreach (object child in children)
            {
                if (child is System.Windows.DependencyObject dobj)
                {
                    LocalizeLogicalElement(dobj, windowName);
                }
            }
        }

        private static void LocalizeVisualElement(System.Windows.DependencyObject element, string windowName)
        {
            if (element == null) return;

            LocalizeElementNode(element, windowName);

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
                LocalizeVisualElement(child, windowName);
            }
        }

        private static void LocalizeElementNode(System.Windows.DependencyObject element, string windowName)
        {
            if (element == null) return;

            // 1. HeaderedContentControl (like GroupBox)
            if (element is System.Windows.Controls.HeaderedContentControl hcc && hcc.Header is string headerStr)
            {
                string orig = GetOrRegisterOriginalText(hcc, headerStr);
                hcc.Header = GetLocalizedText(orig, windowName);
            }
            // 2. ContentControl (like Button, CheckBox, Label, ComboBoxItem)
            else if (element is System.Windows.Controls.ContentControl cc && cc.Content is string contentStr)
            {
                string orig = GetOrRegisterOriginalText(cc, contentStr);
                cc.Content = GetLocalizedText(orig, windowName);
            }
            // 3. TextBlock (plain text elements)
            else if (element is System.Windows.Controls.TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                string orig = GetOrRegisterOriginalText(tb, tb.Text);
                tb.Text = GetLocalizedText(orig, windowName);
            }
            // 4. Run (inline text inside TextBlock, e.g. within tooltip content)
            else if (element is System.Windows.Documents.Run run && !string.IsNullOrEmpty(run.Text))
            {
                string orig = GetOrRegisterOriginalText(run, run.Text);
                run.Text = GetLocalizedText(orig, windowName);
            }

            // 5. ToolTip
            if (element is System.Windows.FrameworkElement fe && fe.ToolTip != null)
            {
                if (fe.ToolTip is string tooltipStr)
                {
                    string orig = GetOrRegisterOriginalText(fe, tooltipStr);
                    fe.ToolTip = GetLocalizedTooltipText(orig, windowName);
                }
                else if (fe.ToolTip is System.Windows.DependencyObject tooltipDobj)
                {
                    LocalizeLogicalElement(tooltipDobj, windowName);
                }
            }
        }

        private static string GetLocalizedTooltipText(string text, string windowName)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Context-specific tooltip key first (e.g., "UI.ModifySliderWindow.ToolTip.例如: 0,100,200,350")
            string keyContext = $"UI.{windowName}.ToolTip.{text}";
            if (Translations.ContainsKey(keyContext))
            {
                return GetString(keyContext, text);
            }

            // Generic tooltip key (e.g., "UI.ToolTip.例如: 0,100,200,350")
            string keyGeneric = $"UI.ToolTip.{text}";
            if (Translations.ContainsKey(keyGeneric))
            {
                return GetString(keyGeneric, text);
            }

            return GetLocalizedText(text, windowName);
        }

        private static string GetLocalizedText(string text, string windowName)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Context-specific key first (e.g., "UI.ModifySliderWindow.新建")
            string keyContext = $"UI.{windowName}.{text}";
            if (Translations.ContainsKey(keyContext))
            {
                return GetString(keyContext, text);
            }

            // Generic key (e.g., "UI.新建")
            string keyGeneric = $"UI.{text}";
            if (Translations.ContainsKey(keyGeneric))
            {
                return GetString(keyGeneric, text);
            }

            return text;
        }

        public static void Initialize()
        {
            try
            {
                // Subscribe to future documents
                Instances.DocumentServer.DocumentAdded += (sender, doc) =>
                {
                    doc.ObjectsAdded += OnObjectsAdded;
                    LocalizeDocument(doc);
                };

                // Subscribe to existing documents
                foreach (GH_Document doc in Instances.DocumentServer)
                {
                    doc.ObjectsAdded += OnObjectsAdded;
                    LocalizeDocument(doc);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error initializing LanguageManager document listener: {ex.Message}");
            }
        }

        private static void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            foreach (var obj in e.Objects)
            {
                if (obj != null && obj.GetType().Namespace != null && obj.GetType().Namespace.StartsWith("Motion"))
                {
                    LocalizeObject(obj);
                }
            }
        }

        private static void LocalizeDocument(GH_Document doc)
        {
            if (doc == null) return;
            foreach (var obj in doc.Objects)
            {
                if (obj != null && obj.GetType().Namespace != null && obj.GetType().Namespace.StartsWith("Motion"))
                {
                    LocalizeObject(obj);
                }
            }
        }

        public static void LocalizeObject(IGH_DocumentObject obj)
        {
            if (obj == null) return;
            if (obj is GH_Component comp)
            {
                LocalizeComponentParams(comp);
            }
            else
            {
                string guidStr = obj.ComponentGuid.ToString();
                string nameKey = $"Component.{guidStr}.Name";
                string descKey = $"Component.{guidStr}.Desc";
                obj.Name = GetString(nameKey, obj.Name);
                obj.Description = GetString(descKey, obj.Description);
            }
        }

        // Dynamic parameter and port localizer
        public static void LocalizeComponentParams(GH_Component component)
        {
            if (component == null) return;
            string componentGuid = component.ComponentGuid.ToString();

            // Localize component's own name and description
            string nameKey = $"Component.{componentGuid}.Name";
            string descKey = $"Component.{componentGuid}.Desc";
            component.Name = GetString(nameKey, component.Name);
            component.Description = GetString(descKey, component.Description);

            for (int i = 0; i < component.Params.Input.Count; i++)
            {
                var param = component.Params.Input[i];
                string keyName = $"Param.{componentGuid}.In.{i}.Name";
                string keyDesc = $"Param.{componentGuid}.In.{i}.Desc";
                param.Name = GetParamString(keyName, param.Name);
                param.Description = GetParamString(keyDesc, param.Description);
            }

            for (int i = 0; i < component.Params.Output.Count; i++)
            {
                var param = component.Params.Output[i];
                string keyName = $"Param.{componentGuid}.Out.{i}.Name";
                string keyDesc = $"Param.{componentGuid}.Out.{i}.Desc";
                param.Name = GetParamString(keyName, param.Name);
                param.Description = GetParamString(keyDesc, param.Description);
            }
        }

        // Full UI refresh execution
        public static void UpdateAllUI()
        {
            // 1. Refresh Canvas components and parameters
            var doc = Instances.ActiveCanvas?.Document;
            if (doc != null)
            {
                foreach (var obj in doc.Objects)
                {
                    if (obj != null && obj.GetType().Namespace != null && obj.GetType().Namespace.StartsWith("Motion"))
                    {
                        LocalizeObject(obj);
                        obj.OnDisplayExpired(true);
                    }
                }
                Instances.ActiveCanvas.Refresh();
            }

            // 2. Refresh Ribbon components description and name
            var motionLibId = new Guid("c6b3341b-4fc6-4a41-8664-969472ee9100"); // Standard Motion library assembly id if any
            foreach (var proxy in Instances.ComponentServer.ObjectProxies)
            {
                if (proxy.Guid != Guid.Empty)
                {
                    string nameKey = $"Component.{proxy.Guid}.Name";
                    string descKey = $"Component.{proxy.Guid}.Desc";
                    proxy.Desc.Name = GetString(nameKey, proxy.Desc.Name);
                    proxy.Desc.Description = GetString(descKey, proxy.Desc.Description);
                }
            }
            RefreshRibbonUI();

            // 3. Re-run toolstrip/toolbar button localizations
            Toolbar.MotionToolbarManager.UpdateLanguageAll();

            // 4. Update all open/active WPF windows dynamically
            ActiveWindows.RemoveAll(wr => !wr.TryGetTarget(out _));
            foreach (var wr in ActiveWindows)
            {
                if (wr.TryGetTarget(out var win) && win.IsVisible)
                {
                    string windowName = win.GetType().Name;
                    
                    // Localize title
                    string origTitle = GetOrRegisterOriginalText(win, win.Title);
                    string titleKey = $"UI.{windowName}.Title";
                    win.Title = GetString(titleKey, origTitle);

                    LocalizeLogicalElement(win, windowName);
                    LocalizeVisualElement(win, windowName);
                }
            }
        }

        private static void RefreshRibbonUI()
        {
            try
            {
                var editor = Instances.DocumentEditor;
                if (editor == null) return;

                // Retrieve the Ribbon instance using reflection
                var ribbonProp = typeof(GH_DocumentEditor).GetProperty("Ribbon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (ribbonProp == null) return;

                var ribbon = ribbonProp.GetValue(editor);
                if (ribbon == null) return;

                // Retrieve and invoke the PopulateRibbon method
                var populateMethod = ribbon.GetType().GetMethod("PopulateRibbon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (populateMethod != null)
                {
                    populateMethod.Invoke(ribbon, null);
                }
                
                editor.Refresh();
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error refreshing Grasshopper ribbon: {ex.Message}");
            }
        }
    }
}

