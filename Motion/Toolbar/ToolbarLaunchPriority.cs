using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.RemotePanel;
using Grasshopper.Kernel;
using Motion.Toolbar;

namespace ExtraButtons
{

    public class ToolbarLaunchPriority : GH_AssemblyPriority
    {
        private static bool _pdePatched = false;

        public override GH_LoadingInstruction PriorityLoad()
        {
            TryPatchPersistentDataEditor();
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            try
            {
                if (args.LoadedAssembly.GetName().Name.Equals("PersistentDataEditor", StringComparison.OrdinalIgnoreCase))
                {
                    TryPatchPersistentDataEditor();
                }
            }
            catch
            {
                // Silently ignore to avoid crashing during assembly load events
            }
        }

        private static void TryPatchPersistentDataEditor()
        {
            if (_pdePatched) return;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var pdeAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Equals("PersistentDataEditor", StringComparison.OrdinalIgnoreCase));
                if (pdeAssembly != null)
                {
                    var type = pdeAssembly.GetType("PersistentDataEditor.SimpleAssemblyPriority");
                    if (type != null)
                    {
                        var field = type.GetField("_paramException", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                        {
                            var original = (string[])field.GetValue(null);
                            if (original != null && original.Length > 0)
                            {
                                original[0] = "Motion.Animation.RemoteParamAttributes";
                                _pdePatched = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Motion] Failed to patch PersistentDataEditor: " + ex.Message);
            }
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;
            GH_DocumentEditor documentEditor = Instances.DocumentEditor;
            if (documentEditor == null)
            {
                Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
            }
            else
            {
                DoingSomethingFirst(documentEditor);
            }
        }

        private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
        {
            Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
            GH_DocumentEditor documentEditor = Instances.DocumentEditor;
            if (documentEditor != null)
            {
                DoingSomethingFirst(documentEditor);
            }
        }

        private void DoingSomethingFirst(GH_DocumentEditor editor)
        {
            CustomMotionToolbar motionToolbar = new CustomMotionToolbar();

            CustomMotionToolbar.SetPosition(ToolbarPosition.Left);

            editor.Controls[0].Controls.Add(CustomMotionToolbar.customMotionToolbar);
        }
    }
}
