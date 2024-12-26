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
                    ShowTemporaryMessage(canvas, "请选择至少一个Graph Mapper");
                    return;
                }

                // 检查是否所有选中的 Graph Mapper 都已经连接到同一个 EventOperation
                var connectedEventOp = GetCommonConnectedEventOperation(selectedMappers);
                
                if (connectedEventOp != null)
                {
                    ShowTemporaryMessage(canvas, "Graph Mapper已连接至Event Operation");
                    return;
                }

                if (selectedEventOp == null)
                {
                    // 尝试从同组中查找已连接的 EventOperation
                    var group = GetCommonGroup(selectedMappers, new List<EventComponent>());
                    if (group != null)
                    {
                        // 获取组内所有的 Graph Mappers
                        var groupMappers = doc.Objects
                            .Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid))
                            .OfType<GH_GraphMapper>()
                            .ToList();

                        // 查找组内已连接到 EventOperation 的 Graph Mapper
                        foreach (var mapper in groupMappers)
                        {
                            var existingEventOp = mapper.Recipients
                                .Select(r => r.Attributes.GetTopLevel.DocObject)
                                .OfType<EventOperation>()
                                .FirstOrDefault();

                            if (existingEventOp != null)
                            {
                                // 将选中的 Graph Mappers 连接到找到的 EventOperation
                                foreach (var selectedMapper in selectedMappers)
                                {
                                    if (!selectedMapper.Recipients.Any(r => 
                                        r.Attributes.GetTopLevel.DocObject == existingEventOp))
                                    {
                                        existingEventOp.Params.Input[0].AddSource(selectedMapper);
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

                    // 计算新组件的位置（在选中的Graph Mappers的最右侧
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

                    // 创建新组
                    CreateOrUpdateGroup(eventOp, selectedMappers);

                    doc.NewSolution(true);
                }
                else
                {
                    // 如果选中了 Event Operation，直接连接到现有组件
                    foreach (var mapper in selectedMappers)
                    {
                        selectedEventOp.Params.Input[0].AddSource(mapper);
                    }
                    // 将新组件添加到现有组或创建新组
                    AddToExistingGroupOrCreate(selectedEventOp, selectedMappers);
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas, $"Error: {ex.Message}");
            }
        }

        // 新增方法：将新组件添加到现有组或创建新组
        private void AddToExistingGroupOrCreate(EventOperation eventOp, List<GH_GraphMapper> mappers)
        {
            var doc = Instances.ActiveCanvas.Document;
            
            // 获取所有相关的Event组件
            var relatedEvents = new List<EventComponent>();
            foreach (var mapper in mappers)
            {
                var sources = mapper.Sources;
                foreach (var source in sources)
                {
                    if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                    {
                        relatedEvents.Add(eventComp);
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
                    .FirstOrDefault(g => mappers.Any(m => g.ObjectIDs.Contains(m.InstanceGuid)) || 
                                       relatedEvents.Any(e => g.ObjectIDs.Contains(e.InstanceGuid)));
            }

            if (existingGroup != null)
            {
                // 将新的Graph Mappers添加到现有组
                foreach (var mapper in mappers)
                {
                    if (!existingGroup.ObjectIDs.Contains(mapper.InstanceGuid))
                    {
                        existingGroup.AddObject(mapper.InstanceGuid);
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
                CreateOrUpdateGroup(eventOp, mappers);
            }
        }

        // 添加辅助方法来检查组件是否在组内并创建/更新组
        private void CreateOrUpdateGroup(EventOperation eventOp, List<GH_GraphMapper> mappers)
        {
            var doc = Instances.ActiveCanvas.Document;
            
            // 获取所有相关的Event组件
            var relatedEvents = new List<EventComponent>();
            foreach (var mapper in mappers)
            {
                var sources = mapper.Sources;
                foreach (var source in sources)
                {
                    if (source.Attributes.GetTopLevel.DocObject is EventComponent eventComp)
                    {
                        relatedEvents.Add(eventComp);
                    }
                }
            }
            
            // 检查是否所有组件都已经在同一个组内
            var existingGroup = GetCommonGroup(mappers, relatedEvents);
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

            // 添加所有Graph Mappers到组
            foreach (var mapper in mappers)
            {
                group.AddObject(mapper.InstanceGuid);
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
        private GH_Group GetCommonGroup(List<GH_GraphMapper> mappers, List<EventComponent> events)
        {
            var doc = Instances.ActiveCanvas.Document;
            var allGroups = doc.Objects.OfType<GH_Group>().ToList();

            foreach (var group in allGroups)
            {
                bool containsAll = true;

                // 检查组是否包含所有Graph Mappers
                foreach (var mapper in mappers)
                {
                    if (!group.ObjectIDs.Contains(mapper.InstanceGuid))
                    {
                        containsAll = false;
                        break;
                    }
                }

                // 检查组是否包含所有Event组件
                if (containsAll && events.Any())
                {
                    foreach (var evt in events)
                    {
                        if (!group.ObjectIDs.Contains(evt.InstanceGuid))
                        {
                            containsAll = false;
                            break;
                        }
                    }
                }

                if (containsAll)
                {
                    return group;
                }
            }

            return null;
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