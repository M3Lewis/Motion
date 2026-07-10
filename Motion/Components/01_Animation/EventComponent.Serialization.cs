using System;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using System.Linq;
using Motion.General;

namespace Motion.Animation
{
    public partial class EventComponent
    {
        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;

            try
            {
                writer.SetBoolean("HideWhenEmpty", _hideWhenEmpty);
                writer.SetBoolean("LockWhenEmpty", _lockWhenEmpty);
                writer.SetBoolean("UseEmptyValueMode", UseEmptyValueMode);
                writer.SetString("NickNameKey", nicknameKey);

                WriteAffectedObjects(writer);

                // 序列化折叠状态
                var attrs = m_attributes as EventComponentAttributes;
                if (attrs != null)
                    writer.SetBoolean("IsCollapsed", attrs.IsCollapsed);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void WriteAffectedObjects(GH_IWriter writer)
        {
            if (affectedObjects == null) return;

            writer.SetInt32("AffectedCount", affectedObjects.Count);
            for (int i = 0; i < affectedObjects.Count; i++)
            {
                if (affectedObjects[i] == null) continue;
                writer.SetGuid($"Affected_{i}", affectedObjects[i].InstanceGuid);
            }
        }

        public override bool Read(GH_IReader reader)
        {
            if (!base.Read(reader)) return false;

            try
            {
                ReadBasicProperties(reader);
                ReadCollapsedState(reader);
                ReadPendingGuids(reader);

                Grasshopper.Instances.DocumentServer.DocumentAdded += OnDocumentAdded;
                return true;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[Motion] EventComponent.Read 出错: {ex.Message}");
                return false;
            }
        }

        private void ReadBasicProperties(GH_IReader reader)
        {
            if (reader.ItemExists("HideWhenEmpty"))
                _hideWhenEmpty = reader.GetBoolean("HideWhenEmpty");
            if (reader.ItemExists("LockWhenEmpty"))
                _lockWhenEmpty = reader.GetBoolean("LockWhenEmpty");
            if (reader.ItemExists("UseEmptyValueMode"))
                UseEmptyValueMode = reader.GetBoolean("UseEmptyValueMode");
            if (reader.ItemExists("NickNameKey"))
            {
                nicknameKey = reader.GetString("NickNameKey");
                base.NickName = nicknameKey;
            }
        }

        private void ReadCollapsedState(GH_IReader reader)
        {
            if (!reader.ItemExists("IsCollapsed")) return;

            var isCollapsed = reader.GetBoolean("IsCollapsed");
            if (m_attributes is EventComponentAttributes attrs)
                attrs.SetCollapsedState(isCollapsed);
        }

        private void ReadPendingGuids(GH_IReader reader)
        {
            _pendingGuids.Clear();
            affectedObjects.Clear();

            if (!reader.ItemExists("AffectedCount")) return;

            int count = reader.GetInt32("AffectedCount");
            for (int i = 0; i < count; i++)
            {
                if (reader.ItemExists($"Affected_{i}"))
                    _pendingGuids.Add(reader.GetGuid($"Affected_{i}"));
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
                RestoreAffectedObjects(doc);
                _isInitialized = false;
                InitializeAfterLoad();

                ApplyInitialHideLockState(doc);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[Motion] Doc_SolutionStart 出错: {ex.Message}");
            }
        }

        private void RestoreAffectedObjects(GH_Document doc)
        {
            affectedObjects.Clear();
            foreach (var guid in _pendingGuids)
            {
                var obj = doc.FindObject(guid, true);
                if (obj != null && !(obj is EventComponent))
                    affectedObjects.Add(obj);
            }

            _pendingGuids.Clear();
        }

        private void ApplyInitialHideLockState(GH_Document doc)
        {
            if (!affectedObjects.Any()) return;

            // 确保状态立即生效
            if (_timelineSlider?.CurrentValue == null) return;

            var currentValue = (double)_timelineSlider?.CurrentValue;
            if (!MotilityUtils.TryParseNickNameInterval(NickName, out double min, out double max))
                return;

            bool shouldHideOrLock = currentValue < (min - 0.0001) || currentValue > (max + 0.0001);

            foreach (var obj in affectedObjects)
            {
                if (obj is IGH_PreviewObject previewObj && HideWhenEmpty)
                    previewObj.Hidden = shouldHideOrLock;
                if (obj is IGH_ActiveObject activeObj && LockWhenEmpty)
                {
                    activeObj.Locked = shouldHideOrLock;
                    if (shouldHideOrLock)
                        activeObj.ClearData();
                }
            }

            _lastHideOrLockState = shouldHideOrLock;
            doc.ScheduleSolution(5);
        }
    }
}