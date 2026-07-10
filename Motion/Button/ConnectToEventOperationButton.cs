using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Motion.Animation;
using Motion.Toolbar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Motion.Animation;

namespace Motion.Toolbar
{
    public class ConnectToEventOperationButton : MotionToolbarButton
    {
        private ToolStripButton _button;
        public override int ToolbarOrder => 50;

        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += OnCanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void OnCanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= OnCanvasCreated;

            GH_DocumentEditor editor = Instances.DocumentEditor;
            if (editor == null) return;

            InitializeButton();
        }

        private void InitializeButton()
        {
            InitializeToolbarGroup();

            _button = new ToolStripButton
            {
                Name = "Connect To Event Operation",
                Size = new Size(24, 24),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Properties.Resources.ConnectToEventOperation,
                ToolTipText = General.LanguageManager.GetString("Button.ConnectToEventOperation.Tooltip",
                    "将选定的Graph Mapper连接到新的Event Operation\n可选择已存在的Event Operation和Graph Mapper进行连接\n若Graph Mapper位于不同组中，将各自连接到所在组 of Event Operation")
            };

            _button.Click += OnConnectToEventOperation;
            AddButtonToToolbars(_button);
        }

        public override void UpdateLanguage()
        {
            if (_button != null)
            {
                _button.ToolTipText = General.LanguageManager.GetString("Button.ConnectToEventOperation.Tooltip",
                    "将选定的Graph Mapper连接到新的Event Operation\n可选择已存在的Event Operation和Graph Mapper进行连接\n若Graph Mapper位于不同组中，将各自连接到所在组 of Event Operation");
            }
        }

        private void OnConnectToEventOperation(object sender, EventArgs e)
        {
            try
            {
                var doc = Instances.ActiveCanvas.Document;
                var canvas = Instances.ActiveCanvas;
                var selectedObjects = doc.SelectedObjects();

                var connectableSelector = new ConnectableObjectSelector(selectedObjects);
                var selectedConnectables = connectableSelector.GetConnectableObjects();
                var selectedEventOp = selectedObjects.OfType<EventOperation>().FirstOrDefault();

                if (selectedConnectables.Count == 0)
                {
                    ShowTemporaryMessage(canvas,
                        General.LanguageManager.GetString("Msg.SelectGraphMapperOrComponent",
                            "请选择至少一个Graph Mapper或组件"));
                    return;
                }

                var connectionManager = new EventOperationConnectionManager(doc);

                // 检查选中的对象是否位于不同的组
                var connectablesByGroup = connectionManager.GroupConnectablesByGroup(selectedConnectables);

                // 获取不在任何组内的对象
                var ungroupedConnectables = GetUngroupedConnectables(selectedConnectables, connectablesByGroup);

                // 如果同时有组内和组外的Graph Mapper
                if (connectablesByGroup.Count > 0 && ungroupedConnectables.Count > 0)
                {
                    ProcessMixedGroupConnectables(connectablesByGroup, ungroupedConnectables, connectionManager);
                    doc.NewSolution(true);
                    return;
                }

                // 如果只有不在组内的对象，单独处理它们
                if (ungroupedConnectables.Count > 0 && connectablesByGroup.Count == 0)
                {
                    ProcessUngroupedConnectables(ungroupedConnectables, connectionManager);
                    doc.NewSolution(true);
                    return;
                }

                // 如果对象位于不同的组且没有选定特定的EventOperation
                if (connectablesByGroup.Count > 0 && selectedEventOp == null)
                {
                    ProcessMultiGroupConnectables(connectablesByGroup, connectionManager);
                    doc.NewSolution(true);
                    return;
                }

                // 处理原有逻辑
                if (selectedEventOp != null)
                {
                    connectionManager.ConnectObjectsToEventOperation(selectedConnectables, selectedEventOp);
                    connectionManager.AddToExistingGroupOrCreate(selectedEventOp, selectedConnectables);
                    doc.NewSolution(true);
                    return;
                }
                else
                {
                    ProcessWithoutSelectedEventOperation(selectedConnectables, connectionManager);
                    doc.NewSolution(true);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas,
                    string.Format(General.LanguageManager.GetString("Msg.ErrorOccurred", "发生错误：{0}"), ex.Message));
            }
        }

        // 处理混合情况：部分Graph Mapper在组内，部分在组外
        private void ProcessMixedGroupConnectables(
            Dictionary<GH_Group, List<ConnectableObject>> connectablesByGroup,
            List<ConnectableObject> ungroupedConnectables,
            EventOperationConnectionManager connectionManager)
        {
            bool anyConnected = false;

            foreach (var groupEntry in connectablesByGroup)
            {
                var group = groupEntry.Key;
                var groupConnectables = groupEntry.Value;

                // 将组外的对象（及其关联 Event）加入组
                AddUngroupedObjectsToGroup(group, ungroupedConnectables);

                // 查找或创建组内的 EventOperation
                var eventOp = FindOrCreateEventOperationForGroup(group, connectionManager);

                // 合并组内原有对象和新加入的对象，统一连接
                var allConnectables = new List<ConnectableObject>(groupConnectables);
                allConnectables.AddRange(ungroupedConnectables);

                bool connected = connectionManager.ConnectObjectsToEventOperation(allConnectables, eventOp);
                anyConnected |= connected;
            }

            // 显示结果消息
            var canvas = Instances.ActiveCanvas;
            if (anyConnected)
                ShowTemporaryMessage(canvas,
                    General.LanguageManager.GetString("Msg.ConnectedGraphMapperToGroup",
                        "已将Graph Mapper连接到组内的Event Operation"));
            else
                ShowTemporaryMessage(canvas,
                    General.LanguageManager.GetString("Msg.CannotConnectGraphMapper",
                        "无法连接Graph Mapper到Event Operation"));
        }

        /// <summary>
        /// 将未分组的对象加入组，并将其关联的 Event 组件也一并加入（如果尚未在组内）。
        /// </summary>
        private void AddUngroupedObjectsToGroup(GH_Group group, List<ConnectableObject> ungroupedConnectables)
        {
            foreach (var ungrouped in ungroupedConnectables)
            {
                group.AddObject(ungrouped.Object.InstanceGuid);
                foreach (var evt in ungrouped.GetRelatedEventComponents())
                {
                    if (!group.ObjectIDs.Contains(evt.InstanceGuid))
                        group.AddObject(evt.InstanceGuid);
                }
            }
        }

        // 查找组内是否有EventOperation，如果没有则创建一个
        private EventOperation FindOrCreateEventOperationForGroup(
            GH_Group group,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;

            // 首先在组内查找现有的EventOperation
            var existingEventOp = connectionManager.FindEventOperationInGroup(group);
            if (existingEventOp != null)
            {
                return existingEventOp;
            }

            // 获取组内所有对象
            var groupObjects = doc.Objects.Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid));
            var groupConnectableSelector = new ConnectableObjectSelector(groupObjects);
            var allGroupConnectables = groupConnectableSelector.GetConnectableObjects();

            // 查找组内Graph Mapper已连接的EventOperation（可能在组外）
            foreach (var groupConnectable in allGroupConnectables)
            {
                var connectedEventOps = groupConnectable.GetRecipients()
                    .Select(r => r.Attributes.GetTopLevel.DocObject)
                    .OfType<EventOperation>()
                    .FirstOrDefault();

                if (connectedEventOps != null)
                {
                    return connectedEventOps;
                }
            }

            // 计算新组件的位置 - 使用组内对象的平均位置
            PointF avgPos = connectionManager.CalculateAveragePosition(
                allGroupConnectables.Select(c => c.Object).ToList());

            PointF finalPos = new PointF(avgPos.X + 200, avgPos.Y);
            EventOperation eventOp = EventOperation.CreateAndAddEventOperation(doc, finalPos);

            return eventOp;
        }

        // 获取不在任何组内的对象
        private List<ConnectableObject> GetUngroupedConnectables(
            List<ConnectableObject> allConnectables,
            Dictionary<GH_Group, List<ConnectableObject>> connectablesByGroup)
        {
            var groupedConnectables = connectablesByGroup
                .SelectMany(group => group.Value)
                .ToList();

            return allConnectables
                .Where(c => !groupedConnectables.Contains(c))
                .ToList();
        }

        // 处理不在任何组内的对象
        private void ProcessUngroupedConnectables(
            List<ConnectableObject> ungroupedConnectables,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;

            // 检查是否所有选中的对象都已经连接到同一个 EventOperation
            var connectedEventOp = connectionManager.GetCommonConnectedEventOperation(ungroupedConnectables);
            if (connectedEventOp != null)
            {
                ShowTemporaryMessage(Instances.ActiveCanvas,
                    General.LanguageManager.GetString("Msg.AlreadyConnectedToEventOperation",
                        "选中的对象已连接至Event Operation"));
                return;
            }

            // 计算新组件的位置
            PointF avgPos = connectionManager.CalculateAveragePosition(
                ungroupedConnectables.Select(c => c.Object).ToList());
            PointF finalPos = new PointF(avgPos.X + 200, avgPos.Y);

            EventOperation eventOp = EventOperation.CreateAndAddEventOperation(doc, finalPos);

            // 连接对象到新的EventOperation
            connectionManager.ConnectObjectsToEventOperation(ungroupedConnectables, eventOp);

            // 创建新组，但不包含EventOperation
            CreateGroupWithoutEventOperation(ungroupedConnectables, connectionManager);
        }

        // 创建新组但不包含EventOperation
        private void CreateGroupWithoutEventOperation(
            List<ConnectableObject> connectables,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;

            // 获取所有相关的Event组件
            var relatedEvents = connectables.SelectMany(c => c.GetRelatedEventComponents()).Distinct().ToList();

            // 创建新组
            var group = new GH_Group
            {
                NickName = "Events"
            };
            group.CreateAttributes();
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

            // 添加组到文档，但不包含EventOperation
            doc.AddObject(group, false);
        }

        private void ProcessMultiGroupConnectables(
            Dictionary<GH_Group, List<ConnectableObject>> connectablesByGroup,
            EventOperationConnectionManager connectionManager)
        {
            bool anyConnected = false;

            foreach (var groupEntry in connectablesByGroup)
            {
                var group = groupEntry.Key;
                var connectables = groupEntry.Value;

                var existingEventOp = FindConnectedEventOperationInGroup(group, connectables);
                if (existingEventOp == null)
                    existingEventOp = connectionManager.FindEventOperationInGroup(group);

                if (existingEventOp != null)
                {
                    anyConnected |= connectionManager.ConnectObjectsToEventOperation(connectables, existingEventOp);
                }
                else
                {
                    var newOp = CreateEventOperationForGroup(connectables, group, connectionManager);
                    if (newOp != null)
                        anyConnected = true;
                }
            }

            var canvas = Instances.ActiveCanvas;
            if (anyConnected)
                ShowTemporaryMessage(canvas,
                    General.LanguageManager.GetString("Msg.ConnectedGraphMapperToRespectiveGroup",
                        "已将Graph Mapper连接到各自组内的Event Operation"));
            else
                ShowTemporaryMessage(canvas,
                    General.LanguageManager.GetString("Msg.CannotConnectGraphMapper",
                        "无法连接Graph Mapper到Event Operation"));
        }

        /// <summary>
        /// 查找组内其他已连接的 Graph Mapper 所使用的 EventOperation。
        /// 排除当前选中对象自身。
        /// </summary>
        private EventOperation FindConnectedEventOperationInGroup(GH_Group group,
            List<ConnectableObject> currentConnectables)
        {
            var doc = Instances.ActiveCanvas.Document;
            if (doc == null) return null;

            var groupObjects = doc.Objects.Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid));
            var groupConnectableSelector = new ConnectableObjectSelector(groupObjects);
            var allGroupConnectables = groupConnectableSelector.GetConnectableObjects();

            foreach (var groupConnectable in allGroupConnectables)
            {
                if (currentConnectables.Contains(groupConnectable))
                    continue; // 排除当前选中的对象

                var connectedEventOp = groupConnectable.GetRecipients()
                    .Select(r => r.Attributes.GetTopLevel.DocObject)
                    .OfType<EventOperation>()
                    .FirstOrDefault();

                if (connectedEventOp != null)
                    return connectedEventOp;
            }

            return null;
        }

        private EventOperation CreateEventOperationForGroup(
            List<ConnectableObject> connectables,
            GH_Group group,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;
            // 计算新组件的位置 - 使用组内对象的平均位置
            PointF avgPos = connectionManager.CalculateAveragePosition(
                connectables.Select(c => c.Object).ToList());
            PointF finalPos = new PointF(avgPos.X + 200, avgPos.Y);
            EventOperation eventOp = EventOperation.CreateAndAddEventOperation(doc, finalPos);

            // 连接对象到新的EventOperation
            connectionManager.ConnectObjectsToEventOperation(connectables, eventOp);

            return eventOp;
        }

        private void ProcessWithoutSelectedEventOperation(List<ConnectableObject> selectedConnectables,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;
            var group = connectionManager.GetCommonGroup(selectedConnectables);
            if (group == null) return;

            // 获取组内所有的可连接对象
            var connectableSelector = new ConnectableObjectSelector(
                doc.Objects.Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid))
            );
            var groupConnectables = connectableSelector.GetConnectableObjects();

            // 处理组内已连接的EventOperation
            if (ProcessExistingEventOperation(selectedConnectables, groupConnectables, connectionManager))
            {
                doc.NewSolution(true);
                return;
            }

            // 创建新的EventOperation
            CreateNewEventOperation(selectedConnectables, connectionManager);
            doc.NewSolution(true);
        }

        private bool ProcessExistingEventOperation(
            List<ConnectableObject> selectedConnectables,
            List<ConnectableObject> groupConnectables,
            EventOperationConnectionManager connectionManager)
        {
            foreach (var connectable in groupConnectables)
            {
                var existingEventOp = connectable.GetRecipients()
                    .Select(r => r.Attributes.GetTopLevel.DocObject)
                    .OfType<EventOperation>()
                    .FirstOrDefault();

                if (existingEventOp == null) continue;

                bool anyConnected = connectionManager.ConnectObjectsToEventOperation(
                    selectedConnectables.Where(s => !s.IsConnectedToEventOperation()).ToList(),
                    existingEventOp
                );

                if (anyConnected)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateNewEventOperation(
            List<ConnectableObject> selectedConnectables,
            EventOperationConnectionManager connectionManager)
        {
            var doc = Instances.ActiveCanvas.Document;

            // 计算新组件的位置
            var firstObject = selectedConnectables[0].Object;
            float rightmostX = firstObject.Attributes.Bounds.Right;
            float avgY = firstObject.Attributes.Bounds.Y + firstObject.Attributes.Bounds.Height / 2;

            PointF finalPos = new PointF(rightmostX + 200, avgY + 10);
            EventOperation eventOp = EventOperation.CreateAndAddEventOperation(doc, finalPos);

            connectionManager.ConnectObjectsToEventOperation(selectedConnectables, eventOp);
            connectionManager.CreateOrUpdateGroup(eventOp, selectedConnectables);
        }
    }

    // 可连接对象的选择器类
    public class ConnectableObjectSelector
    {
        private readonly IEnumerable<IGH_DocumentObject> _objects;

        public ConnectableObjectSelector(IEnumerable<IGH_DocumentObject> objects)
        {
            _objects = objects;
        }

        public List<ConnectableObject> GetConnectableObjects()
        {
            return _objects
                .Where(IsConnectableObject)
                .Select(obj => new ConnectableObject(obj))
                .ToList();
        }

        private bool IsConnectableObject(IGH_DocumentObject obj)
        {
            return obj is GH_GraphMapper ||
                   (obj is GH_Component comp &&
                    comp.Params.Input.Any(p =>
                        p.Sources.Any(s => s.Attributes.GetTopLevel.DocObject is EventComponent)));
        }
    }

    // 可连接对象类
    public class ConnectableObject
    {
        public IGH_DocumentObject Object { get; }
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
            else if (IsComponent && (Object as GH_Component).Params.Output.Count > 0)
                return (Object as GH_Component).Params.Output[0].Recipients;

            return Enumerable.Empty<IGH_Param>();
        }

        public bool IsConnectedToEventOperation()
        {
            return GetRecipients().Any(r => r.Attributes.GetTopLevel.DocObject is EventOperation);
        }

        public IEnumerable<EventComponent> GetRelatedEventComponents()
        {
            var sources = Enumerable.Empty<IGH_Param>();

            if (IsComponent)
                sources = ((GH_Component)Object).Params.Input.SelectMany(input => input.Sources);
            else if (IsGraphMapper)
                sources = ((GH_GraphMapper)Object).Sources;

            foreach (var source in sources)
            {
                var topLevel = source.Attributes.GetTopLevel?.DocObject;
                if (topLevel is EventComponent ec)
                    yield return ec;
            }
        }
    }

    // 连接管理器类
    public class EventOperationConnectionManager
    {
        private readonly GH_Document _document;

        public EventOperationConnectionManager(GH_Document document)
        {
            _document = document;
        }

        public Dictionary<GH_Group, List<ConnectableObject>> GroupConnectablesByGroup(
            List<ConnectableObject> connectables)
        {
            var result = new Dictionary<GH_Group, List<ConnectableObject>>();
            var allGroups = _document.Objects.OfType<GH_Group>().ToList();

            foreach (var connectable in connectables)
            {
                // 查找包含该对象的所有组
                var containingGroups = allGroups
                    .Where(g => g.ObjectIDs.Contains(connectable.Object.InstanceGuid))
                    .ToList();

                if (containingGroups.Count == 0) continue;

                // 如果对象在多个组中，选择第一个组
                var group = containingGroups.First();

                if (!result.ContainsKey(group))
                {
                    result[group] = new List<ConnectableObject>();
                }

                result[group].Add(connectable);
            }

            return result;
        }

        public EventOperation FindEventOperationInGroup(GH_Group group)
        {
            return _document.Objects
                .Where(obj => group.ObjectIDs.Contains(obj.InstanceGuid))
                .OfType<EventOperation>()
                .FirstOrDefault();
        }

        public PointF CalculateAveragePosition(List<IGH_DocumentObject> objects)
        {
            if (objects == null || objects.Count == 0)
                return PointF.Empty;

            float sumX = 0, sumY = 0;

            foreach (var obj in objects)
            {
                var bounds = obj.Attributes.Bounds;
                sumX += bounds.Right;
                sumY += bounds.Top + bounds.Height / 2;
            }

            return new PointF(sumX / objects.Count, sumY / objects.Count);
        }

        public EventOperation GetCommonConnectedEventOperation(List<ConnectableObject> connectables)
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

        public bool ConnectObjectsToEventOperation(List<ConnectableObject> connectables, EventOperation eventOp)
        {
            bool anyConnected = false;

            foreach (var connectable in connectables)
            {
                if (connectable.IsConnectedToEventOperation()) continue;

                if (connectable.IsGraphMapper)
                {
                    eventOp.Params.Input[0].AddSource((GH_GraphMapper)connectable.Object);
                    anyConnected = true;
                    continue;
                }

                if (!connectable.IsComponent) continue;

                var component = connectable.Object as GH_Component;
                if (component.Params.Output.Count == 0) continue;

                eventOp.Params.Input[0].AddSource(component.Params.Output[0]);
                anyConnected = true;
            }

            return anyConnected;
        }

        public GH_Group GetCommonGroup(List<ConnectableObject> connectables)
        {
            var allGroups = _document.Objects.OfType<GH_Group>().ToList();

            foreach (var group in allGroups)
            {
                if (connectables.All(c => group.ObjectIDs.Contains(c.Object.InstanceGuid)))
                {
                    return group;
                }
            }

            return null;
        }

        public void AddToExistingGroupOrCreate(EventOperation eventOp, List<ConnectableObject> connectables)
        {
            var relatedEvents = connectables.SelectMany(c => c.GetRelatedEventComponents()).Distinct().ToList();

            var existingMappers = eventOp.Params.Input[0].Sources
                .Select(s => s.Attributes.GetTopLevel.DocObject)
                .OfType<GH_GraphMapper>()
                .ToList();

            var existingGroup = FindMatchingGroup(existingMappers, relatedEvents);

            if (existingGroup != null)
            {
                UpdateExistingGroup(existingGroup, connectables, relatedEvents);
                if (!existingGroup.ObjectIDs.Contains(eventOp.InstanceGuid))
                    existingGroup.AddObject(eventOp.InstanceGuid);
                return;
            }

            CreateOrUpdateGroup(eventOp, connectables);
        }

        /// <summary>
        /// 查找包含现有 Graph Mapper 或相关 Event 组件的组。
        /// </summary>
        private GH_Group FindMatchingGroup(List<GH_GraphMapper> existingMappers, List<EventComponent> relatedEvents)
        {
            return _document.Objects.OfType<GH_Group>()
                .FirstOrDefault(g =>
                    existingMappers.Any(m => g.ObjectIDs.Contains(m.InstanceGuid)) ||
                    relatedEvents.Any(e => g.ObjectIDs.Contains(e.InstanceGuid)));
        }

        private void UpdateExistingGroup(GH_Group group, List<ConnectableObject> connectables,
            List<EventComponent> relatedEvents)
        {
            // 将新的连接对象添加到现有组
            foreach (var connectable in connectables)
            {
                if (!group.ObjectIDs.Contains(connectable.Object.InstanceGuid))
                {
                    group.AddObject(connectable.Object.InstanceGuid);
                }
            }

            // 将相关的Event组件添加到现有组
            foreach (var evt in relatedEvents)
            {
                if (!group.ObjectIDs.Contains(evt.InstanceGuid))
                {
                    group.AddObject(evt.InstanceGuid);
                }
            }
        }

        public void CreateOrUpdateGroup(EventOperation eventOp, List<ConnectableObject> connectables,
            bool includeEventOp = false)
        {
            // 获取所有相关的Event组件
            var relatedEvents = connectables.SelectMany(c => c.GetRelatedEventComponents()).Distinct().ToList();

            // 检查是否所有组件都已经在同一个组内
            var existingGroup = GetCommonGroup(connectables);
            if (existingGroup != null)
            {
                // 如果已经在同一个组内，根据参数决定是否将EventOperation添加到组中
                if (includeEventOp && !existingGroup.ObjectIDs.Contains(eventOp.InstanceGuid))
                {
                    existingGroup.AddObject(eventOp.InstanceGuid);
                }

                return;
            }

            // 创建新组
            var group = new GH_Group
            {
                NickName = "Events" // 使用EventOperation的nickname
            };
            group.CreateAttributes();
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

            // 根据参数决定是否添加EventOperation到组
            if (includeEventOp)
            {
                group.AddObject(eventOp.InstanceGuid);
            }

            // 添加组到文档
            _document.AddObject(group, false);
        }
    }
}