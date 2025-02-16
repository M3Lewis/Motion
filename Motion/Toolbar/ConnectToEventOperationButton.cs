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
            button.ToolTipText = "将选定的Graph Mapper连接到新的Event Operation\n可选择已存在的Event Operation和Graph Mapper进行连接";
            button.Click += ConnectToEventOperation;
        }

        // 添加一个新的类来表示可连接的对象
        private class ConnectableObject
        {
            public IGH_DocumentObject Object { get; set; }
            public bool IsGraphMapper => Object is GH_GraphMapper;
            public bool IsComponent => Object is GH_Component;

            public ConnectableObject(IGH_DocumentObject obj)
            {
                Object = obj;
            }

            public IEnumerable<IGH_Param> GetRecipients()
            {
                if (IsGraphMapper)
                    return (Object as GH_GraphMapper).Recipients;
                else if (IsComponent)
                    return (Object as GH_Component).Params.Output[0].Recipients;
                return Enumerable.Empty<IGH_Param>();
            }

            public bool IsConnectedToEventOperation()
            {
                return GetRecipients().Any(r => r.Attributes.GetTopLevel.DocObject is EventOperation);
            }
        }

        public void ConnectToEventOperation(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas.Document;
                var canvas = Instances.ActiveCanvas;
                var selectedObjects = doc.SelectedObjects();

                // 获取选中的 Graph Mappers 和 Components
                var selectedConnectables = selectedObjects
                    .Where(obj => obj is GH_GraphMapper || 
                                 (obj is GH_Component comp && 
                                  comp.Params.Input.Any(p => p.Sources.Any(s => s.Attributes.GetTopLevel.DocObject is EventComponent))))
                    .Select(obj => new ConnectableObject(obj))
                    .ToList();

                var selectedEventOp = selectedObjects.OfType<EventOperation>().FirstOrDefault();

                if (selectedConnectables.Count == 0)
                {
                    ShowTemporaryMessage(canvas, "请选择至少一个Graph Mapper或组件");
                    return;
                }

                // 检查是否所有选中的对象都已经连接到同一个 EventOperation
                var connectedEventOp = GetCommonConnectedEventOperation(selectedConnectables);
                
                if (connectedEventOp != null)
                {
                    ShowTemporaryMessage(canvas, "选中的对象已连接至Event Operation");
                    return;
                }

                if (selectedEventOp == null)
                {
                    // 尝试从同组中查找已连接的 EventOperation
                    var group = GetCommonGroup(selectedConnectables);
                    if (group != null)
                    {
                        // 获取组内所有的可连接对象
                        var groupConnectables = doc.Objects
                            .Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid))
                            .Where(obj => obj is GH_GraphMapper || 
                                         (obj is GH_Component comp && 
                                          comp.Params.Input.Any(p => p.Sources.Any(s => s.Attributes.GetTopLevel.DocObject is EventComponent))))
                            .Select(obj => new ConnectableObject(obj))
                            .ToList();

                        // 查找组内已连接到 EventOperation 的对象
                        foreach (var connectable in groupConnectables)
                        {
                            var existingEventOp = connectable.GetRecipients()
                                .Select(r => r.Attributes.GetTopLevel.DocObject)
                                .OfType<EventOperation>()
                                .FirstOrDefault();

                            if (existingEventOp != null)
                            {
                                // 将选中的对象连接到找到的 EventOperation
                                foreach (var selected in selectedConnectables)
                                {
                                    if (!selected.IsConnectedToEventOperation())
                                    {
                                        if (selected.IsGraphMapper)
                                        {
                                            var graphMapper = selected.Object as GH_GraphMapper;
                                            existingEventOp.Params.Input[0].AddSource(graphMapper);
                                        }
                                        else if (selected.IsComponent)
                                        {
                                            var component = selected.Object as GH_Component;
                                            if (component.Params.Output.Count > 0)
                                            {
                                                existingEventOp.Params.Input[0].AddSource(component.Params.Output[0]);
                                            }
                                        }
                                    }
                                }
                                doc.NewSolution(true);
                                return;
                            }
                        }
                    }

                    // 如果没有找到已连接的 EventOperation，创建新的组件
                    var eventOp = new EventOperation();
                    eventOp.CreateAttributes();

                    // 计算新组件的位置
                    float rightmostX = selectedConnectables[0].Object.Attributes.Bounds.Right;
                    float avgY = selectedConnectables[0].Object.Attributes.Bounds.Y + 
                                selectedConnectables[0].Object.Attributes.Bounds.Height / 2;

                    PointF newPos = new PointF(rightmostX + 200, avgY + 10);
                    eventOp.Attributes.Pivot = newPos;

                    // 添加组件到文档
                    doc.AddObject(eventOp, false);

                    // 连接所有选中的对象到 Event Operation
                    foreach (var connectable in selectedConnectables)
                    {
                        if (connectable.IsGraphMapper)
                        {
                            var graphMapper = connectable.Object as GH_GraphMapper;
                            eventOp.Params.Input[0].AddSource(graphMapper);
                        }
                        else if (connectable.IsComponent)
                        {
                            var component = connectable.Object as GH_Component;
                            if (component.Params.Output.Count > 0)
                            {
                                eventOp.Params.Input[0].AddSource(component.Params.Output[0]);
                            }
                        }
                    }

                    // 创建新组
                    CreateOrUpdateGroup(eventOp, selectedConnectables);

                    doc.NewSolution(true);
                }
                else
                {
                    // 连接到现有的 Event Operation
                    foreach (var connectable in selectedConnectables)
                    {
                        if (connectable.IsGraphMapper)
                        {
                            var graphMapper = connectable.Object as GH_GraphMapper;
                            selectedEventOp.Params.Input[0].AddSource(graphMapper);
                        }
                        else if (connectable.IsComponent)
                        {
                            var component = connectable.Object as GH_Component;
                            if (component.Params.Output.Count > 0)
                            {
                                selectedEventOp.Params.Input[0].AddSource(component.Params.Output[0]);
                            }
                        }
                    }
                    // 更新组
                    AddToExistingGroupOrCreate(selectedEventOp, selectedConnectables);
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }

        // 新增方法：将新组件添加到现有组或创建新组
        private void AddToExistingGroupOrCreate(EventOperation eventOp, List<ConnectableObject> connectables)
        {
            var doc = Instances.ActiveCanvas.Document;
            
            // 获取所有相关的Event组件
            var relatedEvents = new List<EventComponent>();
            foreach (var connectable in connectables)
            {
                if (connectable.IsComponent)
                {
                    var component = connectable.Object as GH_Component;
                    foreach (var input in component.Params.Input)
                    {
                        foreach (var source in input.Sources)
                        {
                            if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                            {
                                relatedEvents.Add(eventComp);
                            }
                        }
                    }
                }
                else if (connectable.IsGraphMapper)
                {
                    var mapper = connectable.Object as GH_GraphMapper;
                    foreach (var source in mapper.Sources)
                    {
                        if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                        {
                            relatedEvents.Add(eventComp);
                        }
                    }
                }
            }

            // 获取已经连接到EventOperation的所有Graph Mapper
            var existingMappers = eventOp.Params.Input[0].Sources
                .Select(s => s.Attributes.GetTopLevel.DocObject)
                .OfType<GH_GraphMapper>()
                .ToList();

            // 查找包含现有Graph Mapper的组
            var existingGroup = doc.Objects.OfType<GH_Group>()
                .FirstOrDefault(g => existingMappers.Any(m => g.ObjectIDs.Contains(m.InstanceGuid)));

            // 如果没有找到包含现有Graph Mapper的组，则查找是否有组包含新的Graph Mapper或Event
            if (existingGroup == null)
            {
                existingGroup = doc.Objects.OfType<GH_Group>()
                    .FirstOrDefault(g => relatedEvents.Any(e => g.ObjectIDs.Contains(e.InstanceGuid)));
            }

            if (existingGroup != null)
            {
                // 将新的Graph Mappers添加到现有组
                foreach (var connectable in connectables)
                {
                    if (!existingGroup.ObjectIDs.Contains(connectable.Object.InstanceGuid))
                    {
                        existingGroup.AddObject(connectable.Object.InstanceGuid);
                    }
                }

                // 将相关的Event组件添加到现有组
                foreach (var evt in relatedEvents)
                {
                    if (!existingGroup.ObjectIDs.Contains(evt.InstanceGuid))
                    {
                        existingGroup.AddObject(evt.InstanceGuid);
                    }
                }
            }
            else
            {
                // 如果没有找到现有组，创建新组
                CreateOrUpdateGroup(eventOp, connectables);
            }
        }

        // 添加辅助方法来检查组件是否在组内并创建/更新组
        private void CreateOrUpdateGroup(EventOperation eventOp, List<ConnectableObject> connectables)
        {
            var doc = Instances.ActiveCanvas.Document;
            
            // 获取所有相关的Event组件
            var relatedEvents = new List<EventComponent>();
            foreach (var connectable in connectables)
            {
                if (connectable.IsComponent)
                {
                    var component = connectable.Object as GH_Component;
                    foreach (var input in component.Params.Input)
                    {
                        foreach (var source in input.Sources)
                        {
                            if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                            {
                                relatedEvents.Add(eventComp);
                            }
                        }
                    }
                }
                else if (connectable.IsGraphMapper)
                {
                    var mapper = connectable.Object as GH_GraphMapper;
                    foreach (var source in mapper.Sources)
                    {
                        if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                        {
                            relatedEvents.Add(eventComp);
                        }
                    }
                }
            }
            
            // 检查是否所有组件都已经在同一个组内
            var existingGroup = GetCommonGroup(connectables);
            if (existingGroup != null)
            {
                // 如果已经在同一个组内，不需要进行操作
                return;
            }

            // 创建新组
            var group = new GH_Group();
            group.CreateAttributes();
            
            // 设置组的名称和颜色
            group.NickName = "Events";  // 使用EventOperation的nickname
            group.Colour = Color.FromArgb(60, 150, 150, 150);

            // 添加所有对象到组
            foreach (var connectable in connectables)
            {
                group.AddObject(connectable.Object.InstanceGuid);
            }

            // 添加所有相关的Event组件到组
            foreach (var evt in relatedEvents)
            {
                group.AddObject(evt.InstanceGuid);
            }

            // 添加组到文档
            doc.AddObject(group, false);
        }

        // 添加辅助方法来获取组件共同所在的组
        private GH_Group GetCommonGroup(List<ConnectableObject> connectables)
        {
            var doc = Instances.ActiveCanvas.Document;
            var allGroups = doc.Objects.OfType<GH_Group>().ToList();

            foreach (var group in allGroups)
            {
                bool containsAll = true;

                // 检查组是否包含所有对象
                foreach (var connectable in connectables)
                {
                    if (!group.ObjectIDs.Contains(connectable.Object.InstanceGuid))
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                {
                    return group;
                }
            }

            return null;
        }

        // 新增辅助方法：检查是否所有对象都连接到同一个 EventOperation
        private EventOperation GetCommonConnectedEventOperation(List<ConnectableObject> connectables)
        {
            if (!connectables.Any()) return null;

            var firstConnectable = connectables[0];
            var firstRecipients = firstConnectable.GetRecipients()
                .Select(r => r.Attributes.GetTopLevel.DocObject)
                .OfType<EventOperation>()
                .ToList();

            if (!firstRecipients.Any()) return null;

            foreach (var eventOp in firstRecipients)
            {
                bool allConnected = connectables.All(c => 
                    c.GetRecipients().Any(r => r.Attributes.GetTopLevel.DocObject == eventOp));

                if (allConnected)
                {
                    return eventOp;
                }
            }

            return null;
        }
    }
} 