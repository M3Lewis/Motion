using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Motion.Animation;
using Motion.Motility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Toolbar
{
    public class ConnectToEventOperationButton : MotionToolbarButton
    {
        private ToolStripButton button;
        protected override int ToolbarOrder => 50;
        public ConnectToEventOperationButton()
        {
        }

        private void AddConnectToEventOperationButton()
        {
            InitializeToolbarGroup();
            button = new ToolStripButton();
            Instantiate();
            AddButtonToGroup(button); // 使用基类方法添加按钮
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
            AddConnectToEventOperationButton();
        }

        private void Instantiate()
        {
            button.Name = "Connect To Event Operation";
            button.Size = new Size(24, 24);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Image = Properties.Resources.ConnectToEventOperation; // 需要添加对应的图标资源
            button.ToolTipText = "Connect selected Graph Mappers to new Event Operation";
            button.Click += ConnectToEventOperation;
        }

        public void ConnectToEventOperation(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas.Document;
                var canvas = Instances.ActiveCanvas;
                var selectedObjects = doc.SelectedObjects();

                // 分别获取选中的 Graph Mappers 和 Event Operation
                var selectedMappers = selectedObjects.OfType<GH_GraphMapper>().ToList();
                var selectedEventOp = selectedObjects.OfType<EventOperation>().FirstOrDefault();

                if (selectedMappers.Count == 0)
                {
                    ShowTemporaryMessage(canvas, "Please select at least one Graph Mapper!");
                    return;
                }

                // 检查是否所有选中的 Graph Mapper 都已经连接到同一个 EventOperation
                var connectedEventOp = GetCommonConnectedEventOperation(selectedMappers);
                
                if (connectedEventOp != null)
                {
                    ShowTemporaryMessage(canvas, "Already connected to an Event Operation!");
                    return;
                }

                if (selectedEventOp != null)
                {
                    // 如果选中了 Event Operation，直接连接到现有组件
                    foreach (var mapper in selectedMappers)
                    {
                        selectedEventOp.Params.Input[0].AddSource(mapper);
                    }
                }
                else
                {
                    // 如果没有选中 Event Operation，创建新的组件
                    var eventOp = new EventOperation();
                    eventOp.CreateAttributes();

                    // 计算新组件的位置（在选中的Graph Mappers的最右侧）
                    float rightmostX = selectedMappers[0].Attributes.Bounds.Right;
                    float avgY = selectedMappers[0].Attributes.Bounds.Y + selectedMappers[0].Attributes.Bounds.Height / 2;

                    // 在最右侧Graph Mapper右边留出50个单位的间距
                    PointF newPos = new PointF(rightmostX + 200, avgY+10);
                    eventOp.Attributes.Pivot = newPos;

                    // 添加组件到文档
                    doc.AddObject(eventOp, false);

                    // 连接所有选中的 Graph Mapper 到 Event Operation 的第一个输入端
                    foreach (var mapper in selectedMappers)
                    {
                        eventOp.Params.Input[0].AddSource(mapper);
                    }

                    doc.NewSolution(true);
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }

        private void ShowTemporaryMessage(GH_Canvas canvas, string message)
        {
            GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                // 保存当前的变换矩阵
                var originalTransform = g.Transform;
                
                // 重置变换，确保文字大小不受画布缩放影响
                g.ResetTransform();

                // 计算文本大小
                SizeF textSize = new SizeF(30, 30);
                
                // 设置消息位置在画布顶部居中
                float padding = 20;
                float x = textSize.Width + 300;
                float y = padding + 30;

                RectangleF textBounds = new RectangleF(x, y, textSize.Width + 300, textSize.Height + 30);
                textBounds.Inflate(6, 3);  // 添加一些内边距

                // 绘制消息
                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();

                // 恢复原始变换
                g.Transform = originalTransform;
            };

            // 添加临时事件处理器
            canvas.CanvasPostPaintObjects += canvasRepaint;
            
            // 立即刷新画布以显示消息
            canvas.Refresh();

            // 设置定时器移除事件处理器
            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) =>
            {
                canvas.CanvasPostPaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        // 新增辅助方法：检查是否所有 Graph Mapper 都连接到同一个 EventOperation
        private EventOperation GetCommonConnectedEventOperation(List<GH_GraphMapper> mappers)
        {
            if (!mappers.Any()) return null;

            // 获取第一个 mapper 连接的所有 EventOperation
            var firstMapperRecipients = mappers[0].Recipients
                .Select(r => r.Attributes.GetTopLevel.DocObject)
                .OfType<EventOperation>()
                .ToList();

            if (!firstMapperRecipients.Any()) return null;

            // 检查其他 mapper 是否都连接到相同的 EventOperation
            foreach (var eventOp in firstMapperRecipients)
            {
                bool allMappersConnected = mappers.All(m => 
                    m.Recipients.Any(r => r.Attributes.GetTopLevel.DocObject == eventOp));

                if (allMappersConnected)
                {
                    return eventOp;
                }
            }

            return null;
        }
    }
} 