using Grasshopper.Kernel;
using System;
using GH_IO.Serialization;
using Rhino.Geometry;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel.Special;
using System.Windows.Forms;

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

        private bool _hideWhenEmpty;
        public bool HideWhenEmpty
        {
            get { return _hideWhenEmpty; }
            set { _hideWhenEmpty = value; }
        }
        private bool _lockWhenEmpty;

        public bool LockWhenEmpty
        {
            get { return _lockWhenEmpty; }
            set { _lockWhenEmpty = value; }
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

        public EventComponent()
            : base("Event", "Event",
                "事件组件",
                "Motion", "04_Motility")
        {
            UpdateMessage();
        }
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
            pManager.AddIntervalParameter("Domain", "D", "区间参数", GH_ParamAccess.item,new Interval(0,1));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "当前时间在区间内的比例值", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double time = 0;
            Interval domain = new Interval(0, 1);

            // 检查当前是否有数据
            bool currentHasData = DA.GetData(0, ref time);
            if (!currentHasData)
            {
                // 只在从有值变为空值时更新
                if (_lastHasData && UseEmptyValueMode)
                {
                    UpdateGroupVisibilityAndLock();
                }
                _lastHasData = false;
                return;
            }
            if (!DA.GetData(1, ref domain)) return;

            // 从NickName解析区间
            string[] parts = this.NickName.Split('-');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double min) &&
                double.TryParse(parts[1], out double max))
            {
                GH_Document doc = OnPingDocument();
                // 使用Timeline Slider模式
                double timelineSliderValue = (double)doc.Objects
                    .OfType<GH_NumberSlider>()
                    .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase)).Slider.Value;
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
                if (currentInInterval != _lastInInterval && !UseEmptyValueMode)
                {
                    UpdateGroupVisibilityAndLock();
                }

                // 更新状态
                _lastInInterval = currentInInterval;
                _lastHasData = true;

                // 更新Message以显示当前时间值
                this.Message = $"[{min}-{max}] T:{time:F0}";
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
            UpdateMessage();  // 更新Message

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
                // 序列化模式和状态
                writer.SetBoolean("UseEmptyValueMode", UseEmptyValueMode);
                writer.SetBoolean("HideWhenEmpty", HideWhenEmpty);
                writer.SetBoolean("LockWhenEmpty", LockWhenEmpty);

                // 序列化折叠状态
                var attributes = this.Attributes as EventComponentAttributes;
                if (attributes != null)
                {
                    writer.SetBoolean("IsCollapsed", attributes.IsCollapsed);
                }

                // 序列化影响组件的 GUID 列表
                var guidList = affectedObjects?.Select(obj => obj.InstanceGuid).ToList() ?? new List<Guid>();
                writer.SetInt32("AffectedCount", guidList.Count);
                for (int i = 0; i < guidList.Count; i++)
                {
                    writer.SetGuid($"Affected_{i}", guidList[i]);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            try
            {
                // 读取模式和状态
                if (reader.ItemExists("UseEmptyValueMode"))
                    UseEmptyValueMode = reader.GetBoolean("UseEmptyValueMode");
                if (reader.ItemExists("HideWhenEmpty"))
                    HideWhenEmpty = reader.GetBoolean("HideWhenEmpty");
                if (reader.ItemExists("LockWhenEmpty"))
                    LockWhenEmpty = reader.GetBoolean("LockWhenEmpty");

                // 读取折叠状态
                if (reader.ItemExists("IsCollapsed"))
                {
                    var isCollapsed = reader.GetBoolean("IsCollapsed");
                    var attributes = this.Attributes as EventComponentAttributes;
                    if (attributes != null)
                    {
                        attributes.SetCollapsedState(isCollapsed);
                    }
                }

                // 先清空待处理的GUID列表
                _pendingGuids.Clear();

                // 只读取GUID并存储，不立即查找对象
                if (reader.ItemExists("AffectedCount"))
                {
                    int count = reader.GetInt32("AffectedCount");
                    for (int i = 0; i < count; i++)
                    {
                        if (reader.ItemExists($"Affected_{i}"))
                        {
                            var guid = reader.GetGuid($"Affected_{i}");
                            _pendingGuids.Add(guid);
                        }
                    }
                }

                // 重置状态追踪变量
                _lastHasData = false;  // 设置为false以触发第一次更新
                _lastInInterval = false;  // 设置为false以触发第一次更新

                // 在下一个解决方案中强制更新状态
                GH_Document doc = OnPingDocument();
                if (doc != null)
                {
                    doc.ScheduleSolution(5, d =>
                    {
                        UpdateGroupVisibilityAndLock();
                    });
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        protected override System.Drawing.Bitmap Icon => null;
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

        public void UpdateGroupVisibilityAndLock()
        {
            if (affectedObjects == null || !affectedObjects.Any()) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            bool shouldHideOrLock = false;

            if (UseEmptyValueMode)
            {
                // 使用空值模式
                shouldHideOrLock = !this.Params.Input[0].VolatileData.AllData(true).Any();
            }
            else
            {
                // 使用Timeline Slider模式
                var timelineSlider = doc.Objects
                    .OfType<GH_NumberSlider>()
                    .FirstOrDefault(s => s.NickName.Equals("TimeLine(Union)", StringComparison.OrdinalIgnoreCase));

                if (timelineSlider != null)
                {
                    double currentValue = (double)timelineSlider.Slider.Value;

                    // 解析当前receiver的nickname获取区间
                    string[] parts = this.NickName.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double min) &&
                        double.TryParse(parts[1], out double max))
                    {
                        shouldHideOrLock = currentValue < min || currentValue > max;
                    }
                }
            }

            // 应用Hide/Lock状态
            foreach (var obj in affectedObjects)
            {
                if (obj is IGH_PreviewObject previewObj)
                {
                    if (HideWhenEmpty)
                    {
                        previewObj.Hidden = shouldHideOrLock;
                    }
                }
                if (obj is IGH_ActiveObject activeObj)
                {
                    if (LockWhenEmpty)
                    {
                        activeObj.Locked = shouldHideOrLock;
                        activeObj.ClearData();
                    }
                }
            }

            // 刷新文档
            doc.ScheduleSolution(5);
        }
    }
}