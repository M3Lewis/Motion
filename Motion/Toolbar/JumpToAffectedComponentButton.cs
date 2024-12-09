using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using Motion.Animation;
using Grasshopper.GUI.Canvas;
using static Grasshopper.GUI.Canvas.GH_Canvas;

namespace Motion.Toolbar
{
    public class JumpToAffectedComponentButton : MotionToolbarButton
    {
        protected override int ToolbarOrder => 95;
        private ToolStripButton button;

        public JumpToAffectedComponentButton()
        {
        }

        private void AddJumpButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button);
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
            AddJumpButton();
        }

        private void Instantiate()
        {
            button.Name = "Jump To Affected Component";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.JumpToAffectedComponent;
            button.ToolTipText = "选中Event跳转到被HIDE/LOCK的组件，或是选中被HIDE/LOCK的组件跳转回Event";
            button.Click += Button_Click;
            button.Enabled = true;

            // 添加事件监听
            var canvas = Instances.ActiveCanvas;
            if (canvas != null)
            {
                canvas.DocumentChanged += ActiveCanvas_DocumentChanged;
                canvas.MouseDown += Canvas_MouseDown;
                
                if (canvas.Document != null)
                {
                    canvas.Document.ObjectsDeleted += Document_ObjectsChanged;
                    canvas.Document.ObjectsAdded += Document_ObjectsChanged;
                    canvas.Document.SolutionEnd += Document_SolutionEnd;
                }
            }

            UpdateButtonState();
        }

        private void Canvas_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            UpdateButtonState();
        }

        private void Document_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            UpdateButtonState();
        }

        private void Button_Click(object sender, System.EventArgs e)
        {
            var canvas = Instances.ActiveCanvas;
            if (canvas?.Document == null) return;

            var selectedObj = canvas.Document.SelectedObjects().FirstOrDefault();
            if (selectedObj == null) return;

            if (selectedObj is EventComponent eventComponent)
            {
                HandleEventComponentJump(eventComponent, canvas);
            }
            else if (IsControlledByEvent(selectedObj))
            {
                HandleControlledComponentJump(selectedObj, canvas);
            }
        }

        private void HandleEventComponentJump(EventComponent eventComponent, GH_Canvas canvas)
        {
            if (eventComponent.affectedObjects == null || !eventComponent.affectedObjects.Any())
            {
                ShowTemporaryMessage(canvas, "没有找到受影响的组件");
                return;
            }

            var dialog = new JumpToComponentDialog(eventComponent.affectedObjects);
            if (dialog.ShowDialog() == true && dialog.SelectedComponent != null)
            {
                eventComponent.GoComponent(dialog.SelectedComponent);
            }
        }

        private void HandleControlledComponentJump(IGH_DocumentObject component, GH_Canvas canvas)
        {
            var controllingEvent = canvas.Document.Objects
                .OfType<EventComponent>()
                .FirstOrDefault(evt => evt.affectedObjects?.Contains(component) == true);

            if (controllingEvent != null)
            {
                controllingEvent.GoComponent(controllingEvent);
            }
            else
            {
                ShowTemporaryMessage(canvas, "未找到控制此组件的 Event");
            }
        }

        private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
        {
            UpdateButtonState();

            // 重新订阅文档事件
            if (sender.Document != null)
            {
                sender.Document.ObjectsDeleted += Document_ObjectsChanged;
                sender.Document.ObjectsAdded += Document_ObjectsChanged;
            }
        }

        private void Document_ObjectsChanged(object sender, GH_DocObjectEventArgs e)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            try
            {
                var canvas = Instances.ActiveCanvas;
                if (canvas?.Document == null)
                {
                    button.Enabled = false;
                    return;
                }

                var selectedObjects = canvas.Document.SelectedObjects();
                if (selectedObjects == null)
                {
                    button.Enabled = false;
                    return;
                }

                // 检查是否有符合条件的选中对象
                bool hasValidSelection = selectedObjects.Any(obj => 
                    (obj is EventComponent && ((EventComponent)obj).affectedObjects?.Any() == true) || 
                    IsControlledByEvent(obj));

                button.Enabled = hasValidSelection;

                // 强制刷新工具栏
                if (button.Owner is ToolStrip toolStrip)
                {
                    toolStrip.Refresh();
                }
            }
            catch
            {
                button.Enabled = true;  // 如果出错，默认启用按钮
            }
        }

        private bool IsControlledByEvent(IGH_DocumentObject obj)
        {
            if (obj == null) return false;

            var doc = obj.OnPingDocument();
            if (doc == null) return false;

            return doc.Objects
                .OfType<EventComponent>()
                .Any(evt => evt.affectedObjects?.Contains(obj) == true);
        }
    }
} 