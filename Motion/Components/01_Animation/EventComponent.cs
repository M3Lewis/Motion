using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Special;
using System.Drawing;

namespace Motion.Animation
{
    public partial class EventComponent : GH_Component
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

        public EventComponent()
            : base("Event", "Event",
               "控制值变化的事件",
                "Motion", "01_Animation")
        {
            UpdateMessage();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Event;
        public override Guid ComponentGuid => new Guid("A7B3C4D5-E6F7-48A9-B0C1-D2E3F4A5B6C7");

        public override void CreateAttributes()
        {
            m_attributes = new EventComponentAttributes(this);
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
    }
}