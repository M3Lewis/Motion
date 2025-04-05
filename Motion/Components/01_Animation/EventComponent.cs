using Grasshopper.Kernel;
using System;
using GH_IO.Serialization;
using Rhino.Geometry;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel.Special;
using System.Windows.Forms;
using Rhino;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper;
using System.Drawing;
using Motion.General;

namespace Motion.Animation
{
    public class EventComponent : GH_Component
    {

        private MotionSender _linkedSender;
        // 添加一个临时存储GUID的列表
        private List<Guid> _pendingGuids = new List<Guid>();

        // 添加受影响对象列表
        internal List<IGH_DocumentObject> affectedObjects = new List<IGH_DocumentObject>();
        public bool UseEmptyValueMode { get; private set; } = false;  // 是否使用空值模式

        // 检查是否有连接的 EventOperation
        private bool _hasEventOperation = false;

        private bool _hideWhenEmpty = false;
        public bool HideWhenEmpty
        {
            get { return _hideWhenEmpty; }
            set
            {
                _hideWhenEmpty = value;
                // 当值改变时更新状态
                UpdateGroupVisibilityAndLock();
            }
        }
        private bool _lockWhenEmpty = false;

        public bool LockWhenEmpty
        {
            get { return _lockWhenEmpty; }
            set
            {
                _lockWhenEmpty = value;
                // 当值改变时更新状态
                UpdateGroupVisibilityAndLock();
            }
        }

        // 添加 nicknameKey 字段和 NickName 属性
        protected string nicknameKey = "";
        public override string NickName
        {
            get => nicknameKey;
            set
            {
                if (nicknameKey != value)
                {
                    nicknameKey = value;
                    base.NickName = nicknameKey;
                    NickNameChanged?.Invoke(this, nicknameKey);

                    // 处理连接
                    HandleConnectionsOnNicknameChange();

                    // 更新消息显示
                    UpdateMessage();
                    ExpireSolution(true);
                }
            }
        }


        // 添加 NickNameChanged 事件
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;

        // 添加状态追踪变量
        private bool _lastHasData = true;
        private bool _lastInInterval = true;
        private bool _lastHideOrLockState = false;

        private GH_NumberSlider _timelineSlider;
        private bool _isInitialized = false;

        // 添加文档加载完成后的初始化方法
        private void InitializeAfterLoad()
        {
            if (_isInitialized) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找或等待 Timeline Slider
            FindAndConnectTimelineSlider();

            // 添加文档事件监听，以便在后续添加 Timeline Slider 时能够检测到
            doc.ObjectsAdded += Doc_ObjectsAdded;

            _isInitialized = true;
            UpdateGroupVisibilityAndLock();
        }

        private void Doc_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            // 检查是否添加了 Union Slider
            var addedUnionSlider = e.Objects
                .FirstOrDefault(obj => obj is MotionSlider);

            if (addedUnionSlider != null)
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    // 延迟执行以确保 Union Slider 完全初始化
                    doc.ScheduleSolution(5, d =>
                    {
                        FindAndConnectTimelineSlider();
                        ExpireSolution(true);
                    });
                }
            }
        }

        private void FindAndConnectTimelineSlider()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            // 查找 Timeline Slider
            _timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault();

            if (_timelineSlider != null)
            {
                var numberSlider = _timelineSlider as GH_NumberSlider;
                if (numberSlider?.Slider != null)
                {
                    numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    numberSlider.Slider.ValueChanged += OnSliderValueChanged;
                }
            }
        }

        // 修正事件处理器的参数类型
        private void OnSliderValueChanged(object sender, GH_SliderEventArgs e)
        {
            //RhinoApp.WriteLine($"Slider value changed to: {e.Value}");
            UpdateGroupVisibilityAndLock();
        }

        public EventComponent()
            : base("Event", "Event",
               "控制值变化的事件",
                "Motion", "01_Animation")
        {
            UpdateMessage();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            // 添加显示/隐藏按钮的菜单项
            var showHideItem = Menu_AppendItem(menu,
                IsCollapsed ? "显示HIDE/LOCK按钮" : "显示HIDE/LOCK按钮",
                OnShowHideButtonsClicked,
                true,
                !IsCollapsed);

            Menu_AppendSeparator(menu);

            // 添加模式切换选项
            var modeItem = Menu_AppendItem(menu, "空值模式", OnModeToggle, true, UseEmptyValueMode);
            modeItem.ToolTipText = "切换是否使用空值模式进行Hide/Lock控制";

            if (UseEmptyValueMode)
            {
                // 只在空值模式下显示这些选项
                Menu_AppendItem(menu, "Hide When Empty", OnHideToggle, true, HideWhenEmpty);
                Menu_AppendItem(menu, "Lock When Empty", OnLockToggle, true, LockWhenEmpty);
            }

            Menu_AppendSeparator(menu);

            // 添加跳转到 Motion Slider 的选项
            Menu_AppendItem(menu, "跳转到 Motion Sender", OnJumpToMotionSender, true);

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            ToolStripMenuItem recentKeyMenu = Menu_AppendItem(menu, "选择区间");

            // 获取所有区间并排序
            var sortedKeys = MotilityUtils.GetAllKeys(Instances.ActiveCanvas.Document)
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k =>
                {
                    var parts = k.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double start) &&
                        double.TryParse(parts[1], out double end))
                    {
                        return new { Key = k, Start = start, End = end };
                    }
                    return new { Key = k, Start = double.MaxValue, End = double.MaxValue };
                })
                .OrderBy(x => x.Start)
                .ToList();

            // 创建多列布局
            const int maxItemsPerColumn = 20;
            int totalItems = sortedKeys.Count;
            int columnsNeeded = (int)Math.Ceiling((double)totalItems / maxItemsPerColumn);

            // 创建列菜单
            for (int col = 0; col < columnsNeeded; col++)
            {
                int startIdx = col * maxItemsPerColumn;
                int endIdx = Math.Min(startIdx + maxItemsPerColumn, totalItems);

                if (endIdx <= startIdx) break;

                var columnItems = sortedKeys.GetRange(startIdx, endIdx - startIdx);
                var firstItem = columnItems.First();
                var lastItem = columnItems.Last();

                // 创建列标题
                string columnTitle = $"{firstItem.Start:0}-{lastItem.End:0}";
                var columnMenu = new ToolStripMenuItem(columnTitle);

                // 添加区间项
                foreach (var item in columnItems)
                {
                    var menuItem = new ToolStripMenuItem(item.Key);
                    menuItem.Click += Menu_KeyClicked;
                    columnMenu.DropDownItems.Add(menuItem);
                }

                // 将列添加到主菜单
                recentKeyMenu.DropDownItems.Add(columnMenu);
            }
        }

        private void OnJumpToMotionSender(object sender, EventArgs e)
        {
            if (this.Params.Input[0].SourceCount == 0) return;

            var source = this.Params.Input[0].Sources[0];
            if (source is MotionSender motionSender)
            {
                // 跳转到 Motion Sender
                MotilityUtils.GoComponent(motionSender);
            }
        }

        protected void Menu_KeyClicked(object sender, EventArgs e)
        {
            ToolStripMenuItem keyItem = (ToolStripMenuItem)sender;
            this.NickName = keyItem.Text;
            this.Attributes.ExpireLayout();
        }

        private void UpdateMessage()
        {
            // 解析NickName中的区间
            string[] parts = this.NickName?.Split('-');
            if (parts != null && parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                this.Message = $"[{min}-{max}]";
            }
            else
            {
                this.Message = "Invalid Interval";
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Time", "T", "时间参数", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Domain", "D", "区间参数", GH_ParamAccess.item, new Interval(0, 1));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "当前时间在区间内的比例值", GH_ParamAccess.item);
            //pManager.AddIntervalParameter("Domain", "D", "区间参数", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!_isInitialized || _timelineSlider == null)
            {
                //RhinoApp.WriteLine("SolveInstance: Reinitializing");
                InitializeAfterLoad();
            }

            // 检查第一个输入端是否为空
            bool hasData = !this.Params.Input[0].VolatileData.IsEmpty;

            if (!hasData)
            {
                // 在空值模式下，直接更新状态
                if (UseEmptyValueMode)
                {
                    //RhinoApp.WriteLine("Empty input detected in Empty Value Mode");
                    UpdateGroupVisibilityAndLock();
                }
                return;
            }
            IGH_DocumentObject targetOperation = null;

            if (Params.Output[0].Recipients.Count == 0) return;

            foreach (var recipient in Params.Output[0].Recipients)
            {
                var topLevelObj = recipient.Attributes.GetTopLevel.DocObject;

                // 处理 Graph Mapper 的情况
                var graphMapper = topLevelObj as GH_GraphMapper;
                if (graphMapper != null)
                {
                    if (graphMapper.Recipients.Count > 0)
                    {
                        targetOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                    }
                }
                // 处理 Component 的情况
                else
                {
                    var component = topLevelObj as GH_Component;
                    if (component != null && component.Params.Input.Count > 0)
                    {
                        bool isGraphMapperPlus = component.NickName.StartsWith("Mapper+");
                        IGH_Param inputParameter = component.Params.Input[isGraphMapperPlus ? 2 : 0];

                        if (inputParameter.Sources.Contains(this.Params.Output[0]))
                        {
                            targetOperation = component;
                        }
                    }
                }

                if (targetOperation == null)
                {
                    _hasEventOperation = false;
                    continue;
                }

                _hasEventOperation = true;
                break;
            }
            if (!_hasEventOperation)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "没有找到关联的 EventOperation 组件");
            }

            double time = 0;
            Interval domain = new Interval(0, 1);

            // 获取输入数据
            if (!DA.GetData(0, ref time)) return;
            if (!DA.GetData(1, ref domain)) return;
            //DA.SetData(1, domain);
            // 从NickName解析区间
            string[] parts = this.NickName.Split('-');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                var doc = OnPingDocument();
                // 用Timeline Slider模式
                if (_timelineSlider != null)
                {
                    double timelineSliderValue = (double)_timelineSlider.CurrentValue;
                    // 计算当前是否在区间内
                    bool currentInInterval = timelineSliderValue > min && timelineSliderValue < max;

                    // 计算比例值
                    double value;
                    if (time <= min)
                        value = 0;
                    else if (time >= max)
                        value = 1;
                    else
                        value = (time - min) / (max - min);

                    // 输出计算得到的值
                    DA.SetData(0, value);

                    // 只在区间状态发生变化时更新
                    if (currentInInterval != _lastInInterval)
                    {
                        UpdateGroupVisibilityAndLock();
                    }
                    // 非空值模式下的处理
                    if (!UseEmptyValueMode)
                    {

                        // 更新状态
                        _lastInInterval = currentInInterval;
                        _lastHasData = true;

                    }
                    // 更新Message以显示当前时间值
                    this.Message = $"[{min}-{max}]\n{value:F2}";
                }
            }
            else
            {
                // 空值模式下，有数据时也需要更新状态
                if (_lastHasData != true)
                {
                    UpdateGroupVisibilityAndLock();
                }
                _lastHasData = true;
            }
        }

        public void LinkToSender(MotionSender sender)
        {
            if (_linkedSender == sender) return;

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
            }

            _linkedSender = sender;
            // 确保 NickName 与 Sender 保持一致
            if (this.NickName != sender.NickName)
            {
                this.NickName = sender.NickName;
            }
            UpdateMessage();  // 更Message

            // 重新订阅事件
            sender.NickNameChanged -= OnSenderNickNameChanged; // 先取消订阅以避免重复
            sender.NickNameChanged += OnSenderNickNameChanged;
        }

        private void OnSenderNickNameChanged(IGH_DocumentObject sender, string newNickName)
        {
            if (this.NickName != newNickName)
            {
                this.NickName = newNickName;
                UpdateMessage();
                ExpireSolution(true);
            }
        }

        private void HandleConnectionsOnNicknameChange()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var timeInput = this.Params.Input[0];

            // 如果新的nickname与原linked sender不同，断开连接
            if (_linkedSender != null && _linkedSender.NickName != base.NickName)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
                timeInput.RemoveAllSources();
            }

            // 查找匹配的sender并连接
            doc.ScheduleSolution(5, d =>
            {
                var matchingSenders = d.Objects
                    .OfType<MotionSender>()
                    .Where(s => s.NickName == base.NickName)
                    .ToList();

                foreach (var sender in matchingSenders)
                {
                    if (!timeInput.Sources.Contains(sender))
                    {
                        // 移除现有连接
                        timeInput.RemoveAllSources();

                        // 建立新连接
                        timeInput.AddSource(sender);
                        timeInput.WireDisplay = GH_ParamWireDisplay.hidden;

                        // 更新linked sender
                        LinkToSender(sender);
                        break;  // 只连接第一个匹配的sender
                    }
                }
            });
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            // 监听文档的对象添加事件
            document.ObjectsAdded += Document_ObjectsAdded;

            // 延迟执行以确保所有组件都已加载
            document.ScheduleSolution(5, doc =>
            {
                // 监听输入端的变化
                this.Params.Input[0].ObjectChanged += Input_ObjectChanged;

                // 检查现有连接
                var timeInput = this.Params.Input[0];
                if (timeInput.SourceCount > 0)
                {
                    var source = timeInput.Sources[0].Attributes.GetTopLevel.DocObject;
                    if (source is MotionSender remoteSender)
                    {
                        LinkToSender(remoteSender);
                    }
                }
                else
                {
                    // 如果没有连接，尝试查找匹配的 Sender
                    TryConnectToMatchingSender();
                }
            });

            // 添加对象删除事件监听
            document.ObjectsDeleted += Document_ObjectsDeleted;
        }

        private void Document_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            // 检查是否添加了匹配的 MotionSender
            var addedSender = e.Objects
                .OfType<MotionSender>()
                .FirstOrDefault(s => s.NickName == this.NickName);

            if (addedSender != null)
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    // 延迟执行以确保 Sender 完全初始化
                    doc.ScheduleSolution(5, d =>
                    {
                        var timeInput = this.Params.Input[0];
                        if (timeInput.SourceCount == 0)  // 只在没有连接时尝试连接
                        {
                            timeInput.AddSource(addedSender);
                            timeInput.WireDisplay = GH_ParamWireDisplay.hidden;
                            LinkToSender(addedSender);
                        }
                    });
                }
            }
        }

        private void TryConnectToMatchingSender()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var matchingSender = doc.Objects
                .OfType<MotionSender>()
                .FirstOrDefault(s => s.NickName == this.NickName);

            if (matchingSender != null)
            {
                var timeInput = this.Params.Input[0];
                timeInput.AddSource(matchingSender);
                timeInput.WireDisplay = GH_ParamWireDisplay.hidden;
                LinkToSender(matchingSender);

                // 确保 NickName 与 Sender 保持一致
                if (this.NickName == matchingSender.NickName) return;

                this.NickName = matchingSender.NickName;

            }
        }

        private void Input_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            var timeInput = this.Params.Input[0];
            if (timeInput.SourceCount > 0)
            {
                var source = timeInput.Sources[0].Attributes.GetTopLevel.DocObject;
                if (source is MotionSender remoteSender)
                {
                    LinkToSender(remoteSender);
                }
            }
            else if (_linkedSender != null)
            {
                // 断开连接时清理
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            // 清理事件订阅
            if (document != null)
            {
                document.ObjectsAdded -= Document_ObjectsAdded;
                document.ObjectsDeleted -= Document_ObjectsDeleted;
            }

            if (this.Params.Input.Count > 0)
            {
                this.Params.Input[0].ObjectChanged -= Input_ObjectChanged;
            }

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
            }

            base.RemovedFromDocument(document);
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                // 序列化基本状态
                writer.SetBoolean("HideWhenEmpty", _hideWhenEmpty);
                writer.SetBoolean("LockWhenEmpty", _lockWhenEmpty);
                writer.SetBoolean("UseEmptyValueMode", UseEmptyValueMode);
                writer.SetString("NickNameKey", nicknameKey);

                // 序列化受影响对象的GUID
                if (affectedObjects != null)
                {
                    writer.SetInt32("AffectedCount", affectedObjects.Count);
                    for (int i = 0; i < affectedObjects.Count; i++)
                    {
                        if (affectedObjects[i] != null)
                        {
                            writer.SetGuid($"Affected_{i}", affectedObjects[i].InstanceGuid);
                        }
                    }
                }

                // 序列化折叠状态
                var attrs = m_attributes as EventComponentAttributes;
                if (attrs != null)
                {
                    writer.SetBoolean("IsCollapsed", attrs.IsCollapsed);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            try
            {
                //RhinoApp.WriteLine("Reading component state");

                // 取基本状态
                if (reader.ItemExists("HideWhenEmpty"))
                    _hideWhenEmpty = reader.GetBoolean("HideWhenEmpty");

                if (reader.ItemExists("LockWhenEmpty"))
                    _lockWhenEmpty = reader.GetBoolean("LockWhenEmpty");

                if (reader.ItemExists("UseEmptyValueMode"))
                    UseEmptyValueMode = reader.GetBoolean("UseEmptyValueMode");

                if (reader.ItemExists("NickNameKey"))
                    nicknameKey = reader.GetString("NickNameKey");

                // 确保 base.NickName 也被设置
                base.NickName = nicknameKey;

                // 读取折叠态
                if (reader.ItemExists("IsCollapsed"))
                {
                    var isCollapsed = reader.GetBoolean("IsCollapsed");
                    var attributes = this.m_attributes as EventComponentAttributes;
                    if (attributes != null)
                    {
                        attributes.SetCollapsedState(isCollapsed);
                    }
                }

                // 清空并准备恢复受影响对象
                _pendingGuids.Clear();
                affectedObjects.Clear();

                if (reader.ItemExists("AffectedCount"))
                {
                    int count = reader.GetInt32("AffectedCount");
                    for (int i = 0; i < count; i++)
                    {
                        if (reader.ItemExists($"Affected_{i}"))
                        {
                            _pendingGuids.Add(reader.GetGuid($"Affected_{i}"));
                        }
                    }
                }

                //RhinoApp.WriteLine($"Read {_pendingGuids.Count} pending GUIDs");

                // 延迟初始化到组件被添加到文档后
                Grasshopper.Instances.DocumentServer.DocumentAdded += OnDocumentAdded;

                return true;
            }
            catch (Exception ex)
            {
                //RhinoApp.WriteLine($"Error reading component state: {ex.Message}");
                return false;
            }
        }

        private void OnDocumentAdded(GH_DocumentServer sender, GH_Document doc)
        {
            if (doc == null) return;

            // 确保只处理一次
            Grasshopper.Instances.DocumentServer.DocumentAdded -= OnDocumentAdded;

            //RhinoApp.WriteLine("Document added, initializing component");

            // 使用 SolutionStart 事件来确保文档完全加载
            doc.SolutionStart += Doc_SolutionStart;

            // 强制一次解决方案运行
            doc.ScheduleSolution(5);
        }

        private void Doc_SolutionStart(object sender, GH_SolutionEventArgs e)
        {
            var doc = sender as GH_Document;
            if (doc == null) return;

            doc.SolutionStart -= Doc_SolutionStart;
            //RhinoApp.WriteLine("Doc_SolutionStart: Restoring state");

            try
            {
                // 清空当前列表
                affectedObjects.Clear();

                // 恢复受影响对象
                foreach (var guid in _pendingGuids)
                {
                    var obj = doc.FindObject(guid, true);
                    if (obj != null && !(obj is EventComponent))
                    {
                        affectedObjects.Add(obj);
                        //RhinoApp.WriteLine($"Restored affected object: {obj.GetType().Name}|{obj.Name}|{obj.InstanceGuid}");
                    }
                }
                _pendingGuids.Clear();

                // 重置初始化标志
                _isInitialized = false;

                // 初始化组件
                InitializeAfterLoad();

                // 立即强制更新一次状态
                if (affectedObjects.Any())
                {
                    // 确保状态立即生效
                    var currentValue = (double)_timelineSlider?.CurrentValue;
                    string[] parts = this.NickName.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double min) &&
                        double.TryParse(parts[1], out double max))
                    {
                        bool shouldHideOrLock = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);

                        //RhinoApp.WriteLine($"Reapplying initial state - Timeline: {currentValue}, Interval: [{min}-{max}]");

                        foreach (var obj in affectedObjects)
                        {
                            if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                            {
                                previewObj.Hidden = shouldHideOrLock;
                                //RhinoApp.WriteLine($"Initial HIDE={shouldHideOrLock}: {obj.GetType().Name}|{obj.Name}");
                            }
                            if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                            {
                                activeObj.Locked = shouldHideOrLock;
                                if (shouldHideOrLock)
                                {
                                    activeObj.ClearData();
                                }
                                //RhinoApp.WriteLine($"Initial LOCK={shouldHideOrLock}: {obj.GetType().Name}|{obj.Name}");
                            }
                        }

                        _lastHideOrLockState = shouldHideOrLock;
                    }

                    // 强制更新文档
                    doc.ScheduleSolution(5);
                }
            }
            catch (Exception ex)
            {
                //RhinoApp.WriteLine($"Error in Doc_SolutionStart: {ex.Message}");
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Event;
        public override Guid ComponentGuid => new Guid("A7B3C4D5-E6F7-48A9-B0C1-D2E3F4A5B6C7");

        public override void CreateAttributes()
        {
            m_attributes = new EventComponentAttributes(this);
        }

        private void OnModeToggle(object sender, EventArgs e)
        {
            UseEmptyValueMode = !UseEmptyValueMode;
            // 切换模式时重置状态
            _lastHasData = true;
            _lastInInterval = true;
            ExpireSolution(true);
        }

        private void OnHideToggle(object sender, EventArgs e)
        {
            if (UseEmptyValueMode)
            {
                _hideWhenEmpty = !_hideWhenEmpty;
                UpdateGroupVisibilityAndLock();
                ExpireSolution(true);
            }
        }

        private void OnLockToggle(object sender, EventArgs e)
        {
            if (UseEmptyValueMode)
            {
                _lockWhenEmpty = !_lockWhenEmpty;
                UpdateGroupVisibilityAndLock();
                ExpireSolution(true);
            }
        }

        private bool isUpdating = false;
        private bool? lastEmptyState = null;

        public void UpdateGroupVisibilityAndLock()
        {
            // 添加超时机制
            if (isUpdating)
            {
                // 如果上次更新时间超过1秒，强制重置状态
                var doc = OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(1000, d => isUpdating = false);
                }
                return;
            }

            try
            {
                isUpdating = true;

                if (affectedObjects == null || !affectedObjects.Any())
                {
                    isUpdating = false;
                    return;
                }

                var doc = OnPingDocument();
                if (doc == null)
                {
                    isUpdating = false;
                    return;
                }

                // 使用 ScheduleSolution 来延迟更新状态
                doc.ScheduleSolution(5, d =>
                {
                    try
                    {
                        // 处理两种模式
                        if (UseEmptyValueMode)
                        {
                            bool isEmpty = this.Params.Input[0].VolatileData.IsEmpty;
                            if (lastEmptyState.HasValue && lastEmptyState.Value == isEmpty)
                            {
                                isUpdating = false;
                                return;
                            }

                            lastEmptyState = isEmpty;

                            // 创建受影响对象的副本以避免集合修改问题
                            var objectsToUpdate = new List<IGH_DocumentObject>(affectedObjects);

                            foreach (var obj in objectsToUpdate)
                            {
                                if (obj == null) continue;

                                try
                                {
                                    if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                                    {
                                        d.ScheduleSolution(1, doc =>
                                        {
                                            previewObj.Hidden = isEmpty;
                                            doc.ExpireSolution();
                                        });
                                    }
                                    if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                                    {
                                        d.ScheduleSolution(1, doc =>
                                        {
                                            activeObj.Locked = isEmpty;
                                            if (isEmpty)
                                            {
                                                activeObj.Phase = GH_SolutionPhase.Blank;
                                            }
                                            doc.ExpireSolution();
                                        });
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (_timelineSlider != null)
                        {
                            double currentValue = (double)_timelineSlider.CurrentValue;
                            string[] parts = this.NickName.Split('-');
                            if (parts.Length == 2 &&
                                double.TryParse(parts[0], out double min) &&
                                double.TryParse(parts[1], out double max))
                            {
                                bool shouldHideOrLock = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);

                                if (_lastHideOrLockState != shouldHideOrLock)
                                {
                                    var objectsToUpdate = new List<IGH_DocumentObject>(affectedObjects);

                                    foreach (var obj in objectsToUpdate)
                                    {
                                        if (obj == null) continue;

                                        try
                                        {
                                            if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                                            {
                                                d.ScheduleSolution(1, doc =>
                                                {
                                                    previewObj.Hidden = shouldHideOrLock;
                                                    doc.ExpireSolution();
                                                });
                                            }
                                            if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                                            {
                                                d.ScheduleSolution(1, doc =>
                                                {
                                                    activeObj.Locked = shouldHideOrLock;
                                                    if (shouldHideOrLock)
                                                    {
                                                        activeObj.Phase = GH_SolutionPhase.Blank;
                                                    }
                                                    doc.ExpireSolution();
                                                });
                                            }
                                        }
                                        catch { }
                                    }

                                    _lastHideOrLockState = shouldHideOrLock;
                                }
                            }
                        }
                    }
                    finally
                    {
                        // 确保在所有操作完成后重置状态
                        d.ScheduleSolution(10, doc => isUpdating = false);
                    }
                });
            }
            catch
            {
                isUpdating = false;
            }
        }

        // 修改为正确的重写方法
        protected override void BeforeSolveInstance()
        {
            if (!_isInitialized)
            {
                //RhinoApp.WriteLine("BeforeSolveInstance: Initializing component");
                InitializeAfterLoad();
            }
            base.BeforeSolveInstance();
        }

        // 修改为正确的清理方法
        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            base.DocumentContextChanged(document, context);

            if (context == GH_DocumentContext.Close)
            {
                if (_timelineSlider != null)
                {
                    var numberSlider = _timelineSlider as GH_NumberSlider;
                    if (numberSlider?.Slider != null)
                    {
                        numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    }
                    _timelineSlider = null;
                }
                _isInitialized = false;
            }
        }



        // 添加删除事件处理方法
        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            bool needUpdate = false;

            // 检查是否有受控制的对象被删除
            foreach (var deletedObj in e.Objects)
            {
                if (affectedObjects.Contains(deletedObj))
                {
                    affectedObjects.Remove(deletedObj);
                    needUpdate = true;
                }
            }

            // 如果没有任何受控制的对象，关闭 HIDE/LOCK 开关
            if (needUpdate && !affectedObjects.Any())
            {
                HideWhenEmpty = false;
                LockWhenEmpty = false;

                // 更新UI
                ExpireSolution(true);
            }
        }

        private void EmptyModeMenuItem_Clicked(object sender, EventArgs e)
        {
            UseEmptyValueMode = !UseEmptyValueMode;
            ExpireSolution(true);
        }

        // 添加折叠状态属性
        private bool _isCollapsed = false;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                _isCollapsed = value;
                (Attributes as EventComponentAttributes)?.SetCollapsedState(value);
            }
        }

        private void OnShowHideButtonsClicked(object sender, EventArgs e)
        {
            IsCollapsed = !IsCollapsed;
            ExpireSolution(true);
        }
    }
}