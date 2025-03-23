using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.General;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class TimeInterval : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.TimeInterval;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("D2C75940-DF88-4BFD-B398-4A77A488AF27");

        // 添加 nicknameKey 字段和 NickName 属性
        protected string nicknameKey = "";
        private MotionSender _linkedSender;
        private Guid? _associatedGroupId = null; // 添加字段跟踪关联的组ID
        private bool _isGroupCreated = false; // 添加标志以跟踪组是否已创建

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
                    ExpireSolution(true);
                }
            }
        }

        // 添加 NickNameChanged 事件
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;

        public TimeInterval()
            : base("Time Interval", "Time Interval", "获取时间区间", "Motion", "01_Animation")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Input Data",
                "I",
                "输入数据，可接收Motion Sender/Event的输出值",
                GH_ParamAccess.item
            );
            pManager.AddIntegerParameter(
                "Start Offset",
                "S",
                "区间最小值偏移",
                GH_ParamAccess.item,
                0
            );
            pManager.AddIntegerParameter(
                "End Offset",
                "E",
                "区间最大值偏移",
                GH_ParamAccess.item,
                0
            );
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntervalParameter("Range", "R", "时间区间", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
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

            // 添加跳转到 Motion Sender 的选项
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "跳转到 Motion Sender", OnJumpToMotionSender, true);
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

        public void LinkToSender(MotionSender sender)
        {
            if (_linkedSender == sender) return;

            if (_linkedSender != null)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
            }

            _linkedSender = sender;
            this.NickName = sender.NickName;

            sender.NickNameChanged += OnSenderNickNameChanged;
        }

        private void OnSenderNickNameChanged(IGH_DocumentObject sender, string newNickName)
        {
            if (this.NickName != newNickName)
            {
                this.NickName = newNickName;
                ExpireSolution(true);
            }
        }

        private void HandleConnectionsOnNicknameChange()
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var input = this.Params.Input[0];

            // 如果新的nickname与原linked sender不同，断开连接
            if (_linkedSender != null && _linkedSender.NickName != base.NickName)
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
                input.RemoveAllSources();
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
                    if (!input.Sources.Contains(sender))
                    {
                        // 移除现有连接
                        input.RemoveAllSources();

                        // 建立新连接
                        input.AddSource(sender);
                        input.WireDisplay = GH_ParamWireDisplay.hidden;

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
                // 监听末端点的变化
                this.Params.Input[0].ObjectChanged += Input_ObjectChanged;

                // 检查现有连接
                var input = this.Params.Input[0];
                if (input.SourceCount > 0)
                {
                    var source = input.Sources[0].Attributes.GetTopLevel.DocObject;
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
                        var input = this.Params.Input[0];
                        if (input.SourceCount == 0)  // 只在没有连接时尝试连接
                        {
                            input.AddSource(addedSender);
                            input.WireDisplay = GH_ParamWireDisplay.hidden;
                            LinkToSender(addedSender);
                        }
                    });
                }
            }
        }

        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            // 检查是否删除了连接的 MotionSender
            if (_linkedSender != null && e.Objects.Any(obj => obj.InstanceGuid == _linkedSender.InstanceGuid))
            {
                _linkedSender.NickNameChanged -= OnSenderNickNameChanged;
                _linkedSender = null;
            }

            // 检查是否删除了关联的组
            if (_associatedGroupId.HasValue && e.Objects.Any(obj => obj.InstanceGuid == _associatedGroupId.Value))
            {
                _associatedGroupId = null;
                _isGroupCreated = false;
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
                var input = this.Params.Input[0];
                input.AddSource(matchingSender);
                input.WireDisplay = GH_ParamWireDisplay.hidden;
                LinkToSender(matchingSender);
            }
        }

        private void Input_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            var input = this.Params.Input[0];
            if (input.SourceCount > 0)
            {
                var source = input.Sources[0].Attributes.GetTopLevel.DocObject;
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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 检查输入连接
            if (this.Params.Input[0].SourceCount == 0)
                return;

            var doc = OnPingDocument();
            if (doc == null) return;

            // 检查是否已有关联的组
            if (_associatedGroupId.HasValue)
            {
                var existingGroup = doc.FindObject(_associatedGroupId.Value, false) as GH_Group;
                if (existingGroup != null)
                {
                    // 只在需要时更新组名称
                    string newGroupName = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject.NickName + " Param";
                    if (existingGroup.NickName != newGroupName)
                    {
                        existingGroup.NickName = newGroupName;
                        doc.ScheduleSolution(1, d => { }); // 轻量级刷新
                    }

                    // 解析区间并设置输出
                    SetIntervalOutput(DA);
                    return;
                }
                else
                {
                    // 组已被删除，重置标志
                    _associatedGroupId = null;
                    _isGroupCreated = false;
                }
            }

            // 检查是否已经在某个组中
            var allGroups = doc.Objects.OfType<GH_Group>().ToList();
            foreach (var g in allGroups)
            {
                if (g.ObjectIDs.Contains(this.InstanceGuid))
                {
                    // 更新现有组的名称
                    string newGroupName = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject.NickName + " Param";
                    if (g.NickName != newGroupName)
                    {
                        g.NickName = newGroupName;
                    }

                    _associatedGroupId = g.InstanceGuid;
                    _isGroupCreated = true;

                    // 解析区间并设置输出
                    SetIntervalOutput(DA);
                    return;
                }
            }

            // 如果没有关联的组，创建新组
            if (!_isGroupCreated)
            {
                GH_Group group = new GH_Group();
                group.CreateAttributes();
                group.Colour = Color.FromArgb(60, 150, 150, 150);
                group.AddObject(this.InstanceGuid);
                group.NickName = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject.NickName + " Param";
                group.Border = GH_GroupBorder.Blob;
                doc.AddObject(group, false);

                _associatedGroupId = group.InstanceGuid;
                _isGroupCreated = true;

                // 使用轻量级刷新而不是完全重新求解
                doc.ScheduleSolution(1, d => { });
            }

            // 解析区间并设置输出
            SetIntervalOutput(DA);
        }

        // 提取解析区间和设置输出的逻辑到单独的方法
        private void SetIntervalOutput(IGH_DataAccess DA)
        {
            // 检查第一个输入是否为区间数据
            var firstInput = this.Params.Input[0].VolatileData.AllData(true).FirstOrDefault();
            if (firstInput is GH_Interval ghInterval)
            {
                // 如果是区间数据，直接使用
                var interval = ghInterval.Value;

                // 获取偏移值
                int domainStartOffset = 0;
                int domainEndOffset = 0;
                DA.GetData(1, ref domainStartOffset);
                DA.GetData(2, ref domainEndOffset);

                // 应用偏移并输出
                DA.SetData(0, new Interval(interval.T0 - domainStartOffset, interval.T1 + domainEndOffset));
                return;
            }
            else if (firstInput == null)
            {
                DA.SetData(0, null);
                return;
            }
        
            // 原有的从NickName解析区间的逻辑
            double min = 0;
            double max = 100;
        
            string[] parts = this.NickName.Split('-');
            if (parts.Length == 2)
            {
                double.TryParse(parts[0], out min);
                double.TryParse(parts[1], out max);
            }
        
            int startOffset = 0;
            int endOffset = 0;
            DA.GetData(1, ref startOffset);
            DA.GetData(2, ref endOffset);
        
            DA.SetData(0, new Interval(min - startOffset, max + endOffset));
        }
    }
}