using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Motion.Animation;
using Motion.General;
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
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error setting NickName to code in RemoteParam: {ex.Message}");
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

        public void UpdateGroupVisibilityAndLock(IEnumerable<IGH_DocumentObject> extraObjectsToUpdate = null)
        {
            var doc = OnPingDocument();
            if (doc == null) return;

            var targets = new List<IGH_DocumentObject>();
            if (affectedObjects != null)
            {
                targets.AddRange(affectedObjects);
            }
            if (extraObjectsToUpdate != null)
            {
                targets.AddRange(extraObjectsToUpdate);
            }

            var uniqueTargets = targets.Where(obj => obj != null).Distinct().ToList();
            if (!uniqueTargets.Any()) return;

            MotilityUtils.UpdateObjectsVisibilityAndLock(doc, uniqueTargets);
        }
    }
}