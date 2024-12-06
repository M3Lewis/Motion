using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.Animation;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public abstract class RemoteParam : Param_GenericObject, IGH_InitCodeAware
    {
        // 添加新的字段来控制模式
        public bool UseEmptyValueMode { get; set; } = false;  // 是否使用空值模式

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

        internal List<IGH_DocumentObject> affectedObjects = new List<IGH_DocumentObject>();

        // 添加一个临时存储GUID的列表
        private List<Guid> _pendingGuids = new List<Guid>();

        public void SetInitCode(string code)
        {
            if (code == "..")
            {
                this.NickName = MotilityUtils.GetLastUsedKey(Grasshopper.Instances.ActiveCanvas.Document);
                return;
            }
            try
            {
                this.NickName = code;
            }
            catch
            {
            }
        }
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            // 延迟执行以确保文档完全加载
            document.ScheduleSolution(15, doc =>
            {
                // 恢复组件引用
                if (_pendingGuids.Any())
                {
                    affectedObjects.Clear();
                    foreach (var guid in _pendingGuids)
                    {
                        var obj = doc.FindObject(guid, false);
                        if (obj != null)
                        {
                            affectedObjects.Add(obj);
                        }
                    }
                    _pendingGuids.Clear();
                }
                
                if (_hideWhenEmpty || _lockWhenEmpty)
                {
                    UpdateGroupVisibilityAndLock();
                }
            });
        }

        public override void CreateAttributes()
        {
            base.Attributes = new RemoteParamAttributes(this);
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        //public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        //{
        //    Menu_AppendSeparator(menu);

        //    ToolStripMenuItem recentKeyMenu = GH_DocumentObject.Menu_AppendItem(menu, "Keys");
        //    foreach (string key in MotilityUtils.GetAllKeys(Grasshopper.Instances.ActiveCanvas.Document).OrderBy(s => s))
        //    {
        //        if (!string.IsNullOrEmpty(key))
        //        {
        //            ToolStripMenuItem keyitem = Menu_AppendItem(recentKeyMenu.DropDown, key, new EventHandler(Menu_KeyClicked));
        //        }
        //    }
        //}

        //protected void Menu_KeyClicked(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.ToolStripMenuItem keyItem = (System.Windows.Forms.ToolStripMenuItem)sender;
        //    this.NickName = keyItem.Text;
        //    this.Attributes.ExpireLayout();
        //}



        //public override bool Write(GH_IWriter writer)
        //{
        //    if (!base.Write(writer)) return false;

        //    try
        //    {
        //        writer.SetBoolean("HideWhenEmpty", _hideWhenEmpty);
        //        writer.SetBoolean("LockWhenEmpty", _lockWhenEmpty);

        //        // 序列化折叠状态
        //        var attributes = this.Attributes as RemoteParamAttributes;
        //        if (attributes != null)
        //        {
        //            writer.SetBoolean("IsCollapsed", attributes.IsCollapsed);
        //        }

        //        // 序列化受影响组件的 GUID 列表
        //        var guidList = affectedObjects?.Select(obj => obj.InstanceGuid).ToList() ?? new List<Guid>();
        //        writer.SetInt32("AffectedCount", guidList.Count);
        //        for (int i = 0; i < guidList.Count; i++)
        //        {
        //            writer.SetGuid($"Affected_{i}", guidList[i]);
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //public override bool Read(GH_IReader reader)
        //{
        //    if (!base.Read(reader)) return false;

        //    try
        //    {
        //        if (reader.ItemExists("HideWhenEmpty"))
        //            _hideWhenEmpty = reader.GetBoolean("HideWhenEmpty");
        //        if (reader.ItemExists("LockWhenEmpty"))
        //            _lockWhenEmpty = reader.GetBoolean("LockWhenEmpty");

        //        // 读取折叠状态
        //        if (reader.ItemExists("IsCollapsed"))
        //        {
        //            var isCollapsed = reader.GetBoolean("IsCollapsed");
        //            var attributes = this.Attributes as RemoteParamAttributes;
        //            if (attributes != null)
        //            {
        //                attributes.SetCollapsedState(isCollapsed);
        //            }
        //        }

        //        // 先清空待处理的GUID列表
        //        _pendingGuids.Clear();

        //        // 只读取GUID并存储，不立即查找对象
        //        if (reader.ItemExists("AffectedCount"))
        //        {
        //            int count = reader.GetInt32("AffectedCount");
        //            for (int i = 0; i < count; i++)
        //            {
        //                if (reader.ItemExists($"Affected_{i}"))
        //                {
        //                    var guid = reader.GetGuid($"Affected_{i}");
        //                    _pendingGuids.Add(guid);
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        public void UpdateGroupVisibilityAndLock()
        {
            if (affectedObjects == null || !affectedObjects.Any()) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            bool shouldHideOrLock = false;

            if (UseEmptyValueMode)
            {
                // 使用空值模式
                shouldHideOrLock = !this.VolatileData.AllData(true).Any();
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