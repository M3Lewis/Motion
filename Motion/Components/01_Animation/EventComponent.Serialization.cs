using Grasshopper.Kernel;
using GH_IO.Serialization;
using System;
using System.Linq;

namespace Motion.Animation
{
    public partial class EventComponent
    {
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

                // 延迟初始化到组件被添加到文档后
                Grasshopper.Instances.DocumentServer.DocumentAdded += OnDocumentAdded;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnDocumentAdded(GH_DocumentServer sender, GH_Document doc)
        {
            if (doc == null) return;

            // 确保只处理一次
            Grasshopper.Instances.DocumentServer.DocumentAdded -= OnDocumentAdded;

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

                        foreach (var obj in affectedObjects)
                        {
                            if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                            {
                                previewObj.Hidden = shouldHideOrLock;
                            }
                            if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                            {
                                activeObj.Locked = shouldHideOrLock;
                                if (shouldHideOrLock)
                                {
                                    activeObj.ClearData();
                                }
                            }
                        }

                        _lastHideOrLockState = shouldHideOrLock;
                    }

                    // 强制更新文档
                    doc.ScheduleSolution(5);
                }
            }
            catch
            {
            }
        }
    }
}
