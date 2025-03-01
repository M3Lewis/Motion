using System;
using System.Drawing;
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
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
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
