using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System.Linq;
using System.Windows.Forms;
using System;
using GH_IO.Serialization;
using Motion.Utils;
using System.Collections.Generic;

namespace Motion.Motility
{
    public abstract class RemoteParam : Param_GenericObject, IGH_InitCodeAware
    {
        internal bool _hideWhenEmpty = false;
        internal bool _lockWhenEmpty = false;

        internal List<IGH_DocumentObject> affectedObjects = new List<IGH_DocumentObject>();

        private bool isUpdating = false;
        private bool? lastEmptyState = null;

        public bool HideButtonEnabled
        {
            get => _hideWhenEmpty;
            set
            {
                if (_hideWhenEmpty != value)
                {
                    _hideWhenEmpty = value;
                    UpdateGroupVisibilityAndLock();
                }
            }
        }

        public bool LockButtonEnabled
        {
            get => _lockWhenEmpty;
            set
            {
                if (_lockWhenEmpty != value)
                {
                    _lockWhenEmpty = value;
                    UpdateGroupVisibilityAndLock();
                }
            }
        }

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
            document.ScheduleSolution(5, callback => MotilityUtils.connectMatchingParams(callback, true));
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

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);

            ToolStripMenuItem recentKeyMenu = GH_DocumentObject.Menu_AppendItem(menu, "Keys");
            foreach (string key in MotilityUtils.GetAllKeys(Grasshopper.Instances.ActiveCanvas.Document).OrderBy(s => s))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    ToolStripMenuItem keyitem = Menu_AppendItem(recentKeyMenu.DropDown, key, new EventHandler(Menu_KeyClicked));
                }
            }
        }

        protected void Menu_KeyClicked(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripMenuItem keyItem = (System.Windows.Forms.ToolStripMenuItem)sender;
            this.NickName = keyItem.Text;
            this.Attributes.ExpireLayout();
        }

        public void UpdateGroupVisibilityAndLock()
        {
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                var doc = OnPingDocument();
                if (doc == null) return;

                // 检查数据是否为空
                bool isEmpty = this.VolatileData.IsEmpty;

                // 如果状态没有改变，不需要更新
                if (lastEmptyState.HasValue && lastEmptyState.Value == isEmpty)
                    return;

                lastEmptyState = isEmpty;

                // 根据数据状态更新组件状态
                if (affectedObjects != null && affectedObjects.Any())
                {
                    foreach (var obj in affectedObjects)
                    {
                        if (obj is IGH_PreviewObject previewObj && _hideWhenEmpty)
                        {
                            previewObj.Hidden = isEmpty;
                        }
                        if (obj is IGH_ActiveObject activeObj && _lockWhenEmpty)
                        {
                            activeObj.Locked = isEmpty;
                        }
                    }
                    doc.ScheduleSolution(5);
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;
            
            try
            {
                writer.SetBoolean("HideWhenEmpty", _hideWhenEmpty);
                writer.SetBoolean("LockWhenEmpty", _lockWhenEmpty);
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
                if (reader.ItemExists("HideWhenEmpty"))
                    _hideWhenEmpty = reader.GetBoolean("HideWhenEmpty");
                if (reader.ItemExists("LockWhenEmpty"))
                    _lockWhenEmpty = reader.GetBoolean("LockWhenEmpty");
            }
            catch
            {
                return false;
            }
            
            return true;
        }
    }
}