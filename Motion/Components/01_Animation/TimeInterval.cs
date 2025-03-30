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
            var doc = OnPingDocument();
            if (doc == null) return; // Guard Clause: Document unavailable

            // 1. 尝试获取基础区间
            Interval? baseInterval = TryGetBaseInterval();

            // 2. 如果无法从输入或有效 NickName 获取区间，则处理默认情况或无效输入
            if (!baseInterval.HasValue)
            {
                // 如果没有输入连接，尝试使用 NickName 或默认值
                if (this.Params.Input[0].SourceCount == 0)
                {
                     // 尝试从 NickName 解析（如果之前 TryGetBaseInterval 失败）
                     if (!string.IsNullOrEmpty(this.NickName))
                     {
                         string[] parts = this.NickName.Split('-');
                         if (parts.Length == 2 &&
                             double.TryParse(parts[0], out double min) &&
                             double.TryParse(parts[1], out double max))
                         {
                             baseInterval = new Interval(min, max);
                         }
                     }

                     // 如果 NickName 也无效，则使用默认值
                     if (!baseInterval.HasValue)
                     {
                         baseInterval = new Interval(0, 100); // Default interval
                     }
                }
                else // 有输入连接，但无法确定区间
                {
                    DA.SetData(0, null); // 输出 null
                    ManageComponentGroup(doc); // 仍然管理组，因为有连接
                    return; // 区间无效，提前返回
                }
            }

            // --- 此时 baseInterval 必有值 (来自输入、NickName 或默认值) ---

            // 3. 管理组件组
            ManageComponentGroup(doc);

            // 4. 设置输出 (应用偏移)
            SetIntervalOutput(DA, baseInterval.Value);
        }

        // --- 新增的辅助方法 ---

        private Interval? TryGetBaseInterval()
        {
            var inputParam = this.Params.Input[0];

            // 1. 检查输入源是否为 GH_NumberSlider
            if (inputParam.SourceCount > 0 && inputParam.Sources[0] is GH_NumberSlider slider)
            {
                // 使用 Slider 的 Min/Max 属性创建 Interval
                try
                {
                    // 添加类型转换以确保安全
                    return new Interval(Convert.ToDouble(slider.Slider.Minimum), Convert.ToDouble(slider.Slider.Maximum));
                }
                catch (InvalidCastException ex)
                {
                    // 处理可能的转换错误，例如如果 Minimum/Maximum 不是数值类型
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"无法从 Slider 获取有效的数值范围: {ex.Message}");
                    return null;
                }
            }

            // 2. 检查输入数据是否为 GH_Interval
            var firstInput = inputParam.VolatileData.AllData(true).FirstOrDefault();
            if (firstInput is GH_Interval ghInterval)
            {
                return ghInterval.Value;
            }

            // 3. 尝试从 NickName 解析 (仅当 NickName 格式有效时)
            // 注意：这一步现在主要用于没有输入连接时的回退，
            // 因为 SolveInstance 中会再次检查 NickName（如果 TryGetBaseInterval 返回 null 且无输入）
            if (!string.IsNullOrEmpty(this.NickName))
            {
                string[] parts = this.NickName.Split('-');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double min) &&
                    double.TryParse(parts[1], out double max))
                {
                    return new Interval(min, max);
                }
            }

            // 4. 如果以上都失败，返回 null
            return null;
        }


        private void ManageComponentGroup(GH_Document doc)
        {
            var inputParam = this.Params.Input[0];

            // 仅当有输入连接时才管理组
            if (inputParam.SourceCount == 0)
            {
                // 如果没有输入连接，确保组状态被重置（如果之前有关联）
                if (_associatedGroupId.HasValue)
                {
                    _associatedGroupId = null;
                    _isGroupCreated = false;
                }
                return; // 没有输入，无需管理组
            }

            // 尝试获取源对象的 NickName，如果源无效则使用默认名称或不处理
            IGH_DocumentObject sourceObject = null;
            try {
                 // 确保源存在且有效
                 if (inputParam.Sources.Count > 0 && inputParam.Sources[0] != null && inputParam.Sources[0].Attributes != null)
                 {
                    sourceObject = inputParam.Sources[0].Attributes.GetTopLevel?.DocObject;
                 }
            } catch (Exception ex) {
                 // 处理可能的异常，例如源对象已被删除但连接仍然存在
                 this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"无法获取源对象进行组管理: {ex.Message}");
                 return; // 无法获取源对象，跳过组管理
            }

            if (sourceObject == null)
            {
                 this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "无法获取有效的源对象进行组管理。");
                 return; // 源对象无效
            }

            string expectedGroupName = sourceObject.NickName;


            // 检查是否已有关联的组
            if (_associatedGroupId.HasValue)
            {
                var existingGroup = doc.FindObject(_associatedGroupId.Value, false) as GH_Group;
                if (existingGroup != null)
                {
                    // 组存在，检查并更新名称
                    if (existingGroup.NickName != expectedGroupName)
                    {
                        existingGroup.NickName = expectedGroupName;
                        doc.ScheduleSolution(1); // 轻量级刷新
                    }
                    // 确保 _isGroupCreated 状态正确
                    _isGroupCreated = true;
                    return; // 找到并处理了关联组
                }
                else
                {
                    // 组已被删除，重置标志，继续后续逻辑查找或创建
                    _associatedGroupId = null;
                    _isGroupCreated = false;
                }
            }

            // 检查是否已在其他组中 (可能被手动添加)
            var allGroups = doc.Objects.OfType<GH_Group>().ToList();
            foreach (var g in allGroups)
            {
                if (g.ObjectIDs.Contains(this.InstanceGuid))
                {
                    // 已在组中，更新其名称并记录关联
                    if (g.NickName != expectedGroupName)
                    {
                        g.NickName = expectedGroupName;
                        // 可能需要刷新，因为我们更改了现有组的名称
                        // doc.ScheduleSolution(1); // 取决于是否希望立即看到名称更改
                    }
                    _associatedGroupId = g.InstanceGuid;
                    _isGroupCreated = true;
                    return; // 找到并处理了所在组
                }
            }

            // 如果没有关联的组且不在任何组中，创建新组
            // 再次检查 _isGroupCreated 避免并发问题或重复创建
            if (!_isGroupCreated && !_associatedGroupId.HasValue)
            {
                GH_Group group = new GH_Group();
                group.CreateAttributes();
                group.Colour = Color.FromArgb(60, 150, 150, 150);
                group.AddObject(this.InstanceGuid);
                group.NickName = expectedGroupName;
                group.Border = GH_GroupBorder.Blob;
                doc.AddObject(group, false);

                _associatedGroupId = group.InstanceGuid;
                _isGroupCreated = true;
                doc.ScheduleSolution(1); // 创建了新对象，需要刷新
            }
        }


        // SetIntervalOutput 方法保持不变
        private void SetIntervalOutput(IGH_DataAccess DA, Interval baseInterval)
        {
            int startOffset = 0;
            int endOffset = 0;
            // 从输入参数 1 和 2 获取偏移量
            DA.GetData(1, ref startOffset);
            DA.GetData(2, ref endOffset);

            // 应用偏移并设置输出参数 0
            DA.SetData(0, new Interval(baseInterval.T0 - startOffset, baseInterval.T1 + endOffset));
        }
    }
}