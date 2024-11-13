using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System.Linq;
using System.Windows.Forms;
using System;
using GH_IO.Serialization;
using Motion.Utils;

namespace Motion
{
    public abstract class RemoteParam : Param_GenericObject, IGH_InitCodeAware
    {
        internal bool _hideWhenEmpty = false;
        internal bool _lockWhenEmpty = false;

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

            ToolStripMenuItem hideItem = Menu_AppendItem(menu, "Hide Components When Empty", HideWhenEmpty_Clicked, true, _hideWhenEmpty);
            hideItem.ToolTipText = "Hide all components in the same group when this sender is empty";

            ToolStripMenuItem lockItem = Menu_AppendItem(menu, "Lock Components When Empty", LockWhenEmpty_Clicked, true, _lockWhenEmpty);
            lockItem.ToolTipText = "Lock all components in the same group when this sender is empty";

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

        private void HideWhenEmpty_Clicked(object sender, EventArgs e)
        {
            _hideWhenEmpty = !_hideWhenEmpty;
            
            if (!_hideWhenEmpty)
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    var currentGroup = doc.Objects.OfType<GH_Group>()
                        .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

                    if (currentGroup != null)
                    {
                        foreach (var objId in currentGroup.ObjectIDs)
                        {
                            var obj = doc.FindObject(objId, false) as GH_Component;
                            if (obj != null && obj.InstanceGuid != this.InstanceGuid)
                            {
                                obj.Hidden = false;
                            }
                        }
                    }
                }
            }
            
            ForceUpdateGroupState();
            ExpireSolution(true);
        }

        private void LockWhenEmpty_Clicked(object sender, EventArgs e)
        {
            _lockWhenEmpty = !_lockWhenEmpty;
            
            if (!_lockWhenEmpty)
            {
                var doc = OnPingDocument();
                if (doc != null)
                {
                    var currentGroup = doc.Objects.OfType<GH_Group>()
                        .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

                    if (currentGroup != null)
                    {
                        foreach (var objId in currentGroup.ObjectIDs)
                        {
                            var obj = doc.FindObject(objId, false) as GH_Component;
                            if (obj != null && obj.InstanceGuid != this.InstanceGuid)
                            {
                                obj.Locked = false;
                            }
                        }
                    }
                }
            }
            
            ForceUpdateGroupState();
            ExpireSolution(true);
        }

        private bool isUpdating = false;
        private bool? lastEmptyState = null;

        private void ForceUpdateGroupState()
        {
            lastEmptyState = null;
            UpdateGroupVisibilityAndLock();
        }

        public void UpdateGroupVisibilityAndLock()
        {
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                var doc = OnPingDocument();
                if (doc == null) return;

                var currentGroup = doc.Objects.OfType<GH_Group>()
                    .FirstOrDefault(g => g.ObjectIDs.Contains(this.InstanceGuid));

                if (currentGroup == null) return;

                bool isEmpty = VolatileData.IsEmpty;

                if (lastEmptyState == null || isEmpty != lastEmptyState)
                {
                    lastEmptyState = isEmpty;

                    foreach (var objId in currentGroup.ObjectIDs)
                    {
                        var obj = doc.FindObject(objId, false) as GH_Component;
                        if (obj != null && obj.InstanceGuid != this.InstanceGuid)
                        {
                            if (_hideWhenEmpty)
                            {
                                obj.Hidden = isEmpty;
                            }
                            if (_lockWhenEmpty)
                            {
                                obj.Locked = isEmpty;
                                
                                if (isEmpty && obj is IGH_Component component)
                                {
                                    foreach (var param in component.Params.Output)
                                    {
                                        param.ClearData();
                                    }
                                    
                                    component.ExpireSolution(true);
                                }
                            }
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