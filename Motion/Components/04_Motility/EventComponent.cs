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

namespace Motion.Motility
{
    public class EventComponent : GH_Component
    {
        
        private Param_RemoteSender _linkedSender;
        // 添加一个临时存储GUID的列表
        private List<Guid> _pendingGuids = new List<Guid>();

        // 添加受影响对象列表
        internal List<IGH_DocumentObject> affectedObjects = new List<IGH_DocumentObject>();
        public bool UseEmptyValueMode { get; private set; } = false;  // 是否使用空值模式

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

            //RhinoApp.WriteLine("InitializeAfterLoad: Starting initialization");

            _timelineSlider = doc.Objects
                .OfType<GH_NumberSlider>()
                .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase));

            if (_timelineSlider != null)
            {
                //RhinoApp.WriteLine($"Found Timeline Slider: {_timelineSlider.InstanceGuid}");

                var numberSlider = _timelineSlider as GH_NumberSlider;
                if (numberSlider?.Slider != null)
                {
                    numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    numberSlider.Slider.ValueChanged += OnSliderValueChanged;
                    //RhinoApp.WriteLine("Timeline Slider ValueChanged event subscribed");
                }
            }
            else
            {
                //RhinoApp.WriteLine("WARNING: Timeline Slider not found!");
            }

            _isInitialized = true;
            UpdateGroupVisibilityAndLock();
        }

        // 修正事件处理器的参数类型
        private void OnSliderValueChanged(object sender, GH_SliderEventArgs e)
        {
            //RhinoApp.WriteLine($"Slider value changed to: {e.Value}");
            UpdateGroupVisibilityAndLock();
        }

        public EventComponent()
            : base("Event", "Event",
                "事件组件",
                "Motion", "04_Motility")
        {
            UpdateMessage();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);

            // 添加模式切换选项
            var modeItem = Menu_AppendItem(menu, "Use Empty Value Mode", OnModeToggle, true, UseEmptyValueMode);
            modeItem.ToolTipText = "切换是否使用空值模式进行Hide/Lock控制";

            if (UseEmptyValueMode)
            {
                // 只在空值模式下显示这些选项
                Menu_AppendItem(menu, "Hide When Empty", OnHideToggle, true, HideWhenEmpty);
                Menu_AppendItem(menu, "Lock When Empty", OnLockToggle, true, LockWhenEmpty);
            }

            // 添加分隔线和跳转选项
            Menu_AppendSeparator(menu);

            // 检查是否有连接的 EventOperation
            var hasEventOperation = false;
            IGH_DocumentObject targetOperation = null;

            if (Params.Output[0].Recipients.Count > 0)
            {
                foreach (var recipient in Params.Output[0].Recipients)
                {
                    var graphMapper = recipient.Attributes.GetTopLevel.DocObject as GH_GraphMapper;
                    if (graphMapper?.Recipients.Count > 0)
                    {
                        targetOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                        if (targetOperation != null)
                        {
                            hasEventOperation = true;
                            break;
                        }
                    }
                }
            }

            // 添加跳转菜单项
            var jumpItem = Menu_AppendItem(menu, "跳转到 EventOperation", OnJumpToOperation);
            jumpItem.Enabled = hasEventOperation;
            if (hasEventOperation)
            {
                jumpItem.ToolTipText = "跳转到关联的 EventOperation 组件";
            }
            else
            {
                jumpItem.ToolTipText = "没有找到关联的 EventOperation 组件";
            }
        }

        private void OnJumpToOperation(object sender, EventArgs e)
        {
            if (Params.Output[0].Recipients.Count == 0) return;

            foreach (var recipient in Params.Output[0].Recipients)
            {
                var graphMapper = recipient.Attributes.GetTopLevel.DocObject as GH_GraphMapper;
                if (graphMapper?.Recipients.Count > 0)
                {
                    var eventOperation = graphMapper.Recipients[0].Attributes.GetTopLevel.DocObject;
                    if (eventOperation != null)
                    {
                        // 跳转到 EventOperation
                        GoComponent(eventOperation);
                        break;
                    }
                }
            }
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
            pManager.AddIntervalParameter("Domain", "D", "区间参数", GH_ParamAccess.item);
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

            double time = 0;
            Interval domain = new Interval(0, 1);

            // 获取输入数据
            if (!DA.GetData(0, ref time)) return;
            if (!DA.GetData(1, ref domain)) return;
            DA.SetData(1, domain);
            // 从NickName解析区间
            string[] parts = this.NickName.Split('-');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                var doc = OnPingDocument();
                // 使用Timeline Slider模式
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

        public void LinkToSender(Param_RemoteSender sender)
        {
            if (_linkedSender == sender) return;

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
            }

            _linkedSender = sender;
            this.NickName = sender.NickName;
            UpdateMessage();  // 更Message

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
                    .OfType<Param_RemoteSender>()
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
                    if (source is Param_RemoteSender remoteSender)
                    {
                        LinkToSender(remoteSender);
                    }
                }
            });
        }

        private void Input_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            var timeInput = this.Params.Input[0];
            if (timeInput.SourceCount > 0)
            {
                var source = timeInput.Sources[0].Attributes.GetTopLevel.DocObject;
                if (source is Param_RemoteSender remoteSender)
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
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                if (affectedObjects == null || !affectedObjects.Any()) return;

                var doc = OnPingDocument();
                if (doc == null) return;

                // 处理两种模式
                if (UseEmptyValueMode)
                {
                    // 检查第一个输入端是否为空
                    bool isEmpty = this.Params.Input[0].VolatileData.IsEmpty;

                    // 如果状态没有改变，不需要更新
                    if (lastEmptyState.HasValue && lastEmptyState.Value == isEmpty)
                        return;

                    lastEmptyState = isEmpty;

                    //RhinoApp.WriteLine($"Empty Value Mode - Is Empty: {isEmpty}");

                    // 更新所有受影响对象的状态
                    foreach (var obj in affectedObjects)
                    {
                        if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                        {
                            previewObj.Hidden = isEmpty;
                            //RhinoApp.WriteLine($"HIDE={isEmpty}: {obj.GetType().Name}|{obj.Name}");
                        }
                        if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                        {
                            activeObj.Locked = isEmpty;
                            if (isEmpty)
                            {
                                activeObj.ClearData();
                            }
                            //RhinoApp.WriteLine($"LOCK={isEmpty}: {obj.GetType().Name}|{obj.Name}");
                        }
                    }

                    // 强制更新文档
                    doc.ScheduleSolution(5);
                }
                else if (_timelineSlider != null)
                {
                    // Timeline 模式代码
                    double currentValue = (double)_timelineSlider.CurrentValue;
                    string[] parts = this.NickName.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double min) &&
                        double.TryParse(parts[1], out double max))
                    {
                        bool shouldHideOrLock = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);

                        if (_lastHideOrLockState != shouldHideOrLock)
                        {
                            //RhinoApp.WriteLine($"Timeline Value: {currentValue}, Interval: [{min}-{max}]");

                            foreach (var obj in affectedObjects)
                            {
                                if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                                {
                                    previewObj.Hidden = shouldHideOrLock;
                                    //RhinoApp.WriteLine($"HIDE={shouldHideOrLock}: {obj.GetType().Name}|{obj.Name}");
                                }
                                if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                                {
                                    activeObj.Locked = shouldHideOrLock;
                                    if (shouldHideOrLock)
                                    {
                                        activeObj.ClearData();
                                    }
                                    //RhinoApp.WriteLine($"LOCK={shouldHideOrLock}: {obj.GetType().Name}|{obj.Name}");
                                }
                            }

                            _lastHideOrLockState = shouldHideOrLock;
                            doc.ScheduleSolution(5);
                        }
                    }
                }
            }
            finally
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

        // 添加一个方法来重新订阅事件
        private void ResubscribeToEvents()
        {
            //RhinoApp.WriteLine("Resubscribing to events");
            if (_timelineSlider != null)
            {
                var numberSlider = _timelineSlider as GH_NumberSlider;
                if (numberSlider?.Slider != null)
                {
                    numberSlider.Slider.ValueChanged -= OnSliderValueChanged;
                    numberSlider.Slider.ValueChanged += OnSliderValueChanged;
                    //RhinoApp.WriteLine("Timeline Slider ValueChanged event resubscribed");
                }
            }
        }

        public void GoComponent(IGH_DocumentObject com)
        {
            PointF view_point = new PointF(com.Attributes.Pivot.X, com.Attributes.Pivot.Y);
            GH_NamedView gH_NamedView = new GH_NamedView("", view_point, 1.5f, GH_NamedViewType.center);
            foreach (IGH_DocumentObject item in com.OnPingDocument().SelectedObjects())
            {
                item.Attributes.Selected = false;
            }
            com.Attributes.Selected = true;
            gH_NamedView.SetToViewport(Instances.ActiveCanvas, 300);
        }
    }
}