using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class RangeSelector : GH_Param<IGH_Goo>, IGH_PreviewObject, IGH_BakeAwareObject, IGH_StateAwareObject
    {
        protected GH_Structure<IGH_Goo> collectedData;

        private GH_ValueListMode m_listMode;

        private readonly List<Motion_ValueListItem> m_userItems;

        private bool m_hidden;

        private List<GH_NumberSlider> _trackedSliders = new List<GH_NumberSlider>();
        private const string EXCLUDED_NICKNAME = "TimeLine(Union)";

        public RangeSelector()
            : base((IGH_InstanceDescription)new GH_InstanceDescription("Range Selector", "Range", "Allows you to select multiple ranges from TimeLine sliders", "Motion", "01_Animation"))
        {
            collectedData = new GH_Structure<IGH_Goo>();
            m_listMode = GH_ValueListMode.CheckList;
            m_userItems = new List<Motion_ValueListItem>();
            m_hidden = false;

        }
        public override Guid ComponentGuid => new Guid("{956fc4f3-221a-4037-9b30-61167eee17a4}");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override Bitmap Icon => Properties.Resources.RangeSelector;

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NickName))
                {
                    return null;
                }
                if (NickName.Equals("Range", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return NickName;
            }
        }

        public GH_ValueListMode ListMode
        {
            get
            {
                return m_listMode;
            }
            set
            {
                m_listMode = value;
                if (m_attributes != null)
                {
                    m_attributes.ExpireLayout();
                }
            }
        }

        public List<Motion_ValueListItem> ListItems => m_userItems;

        public List<Motion_ValueListItem> SelectedItems
        {
            get
            {
                List<Motion_ValueListItem> list = new List<Motion_ValueListItem>();
                if (m_userItems.Count == 0)
                {
                    return list;
                }
                if (ListMode == GH_ValueListMode.CheckList)
                {
                    foreach (Motion_ValueListItem userItem in m_userItems)
                    {
                        if (userItem.Selected)
                        {
                            list.Add(userItem);
                        }
                    }
                    return list;
                }
                foreach (Motion_ValueListItem userItem2 in m_userItems)
                {
                    if (userItem2.Selected)
                    {
                        list.Add(userItem2);
                        return list;
                    }
                }
                m_userItems[0].Selected = true;
                list.Add(m_userItems[0]);
                return list;
            }
        }

        public Motion_ValueListItem FirstSelectedItem
        {
            get
            {
                if (m_userItems.Count == 0)
                {
                    return null;
                }
                foreach (Motion_ValueListItem userItem in m_userItems)
                {
                    if (userItem.Selected)
                    {
                        return userItem;
                    }
                }
                return m_userItems[0];
            }
        }

        public bool Hidden
        {
            get
            {
                return m_hidden;
            }
            set
            {
                m_hidden = value;
            }
        }

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        public bool IsBakeCapable => !m_data.IsEmpty;



        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsAdded += Document_ObjectsAdded;
            document.ObjectsDeleted += Document_ObjectsDeleted;
            
            // 初始扫描画布上的所有滑块
            ScanForTimeLineSliders();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            // 清理事件订阅
            foreach (var slider in _trackedSliders)
            {
                UnsubscribeFromSlider(slider);
            }
            document.ObjectsAdded -= Document_ObjectsAdded;
            document.ObjectsDeleted -= Document_ObjectsDeleted;
            _trackedSliders.Clear();
            base.RemovedFromDocument(document);
        }

        private bool ShouldTrackSlider(GH_NumberSlider slider)
        {
            return slider.NickName != EXCLUDED_NICKNAME 
                   && slider.GetType().Name == "pOd_TimeLineSlider"|| slider.GetType().Name == "MotionSlider";
        }

        private void Document_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            bool needUpdate = false;
            foreach (var obj in e.Objects)
            {
                if (obj is GH_NumberSlider slider && ShouldTrackSlider(slider))
                {
                    if (!_trackedSliders.Contains(slider))
                    {
                        _trackedSliders.Add(slider);
                        SubscribeToSlider(slider);
                        needUpdate = true;
                    }
                }
            }
            if (needUpdate)
            {
                UpdateRangeItems();
                // 通知所有 UnionSlider 更新区间
                var doc = OnPingDocument();
                if (doc != null)
                {
                    foreach (var obj in doc.Objects)
                    {
                        if (obj is MotionUnionSlider unionSlider)
                        {
                            unionSlider.UpdateUnionRange();
                        }
                    }
                }
            }
        }

        private void Document_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            bool needUpdate = false;
            foreach (var obj in e.Objects)
            {
                if (obj is GH_NumberSlider slider && _trackedSliders.Contains(slider))
                {
                    _trackedSliders.Remove(slider);
                    UnsubscribeFromSlider(slider);
                    needUpdate = true;
                }
            }
            if (needUpdate)
            {
                UpdateRangeItems();
                // 通知所有 UnionSlider 更新区间
                var doc = OnPingDocument();
                if (doc != null)
                {
                    foreach (var obj in doc.Objects)
                    {
                        if (obj is MotionUnionSlider unionSlider)
                        {
                            unionSlider.UpdateUnionRange();
                        }
                    }
                }
            }
        }

        private void SubscribeToSlider(GH_NumberSlider slider)
        {
            slider.ObjectChanged += Slider_Changed;
            slider.AttributesChanged += Slider_Changed;
            slider.Slider.ValueChanged += Slider_ValueChanged;
        }

        private void UnsubscribeFromSlider(GH_NumberSlider slider)
        {
            slider.ObjectChanged -= Slider_Changed;
            slider.AttributesChanged -= Slider_Changed;
            slider.Slider.ValueChanged -= Slider_ValueChanged;
        }

        private void Slider_Changed(object sender, EventArgs e)
        {
            UpdateRangeItems();
        }

        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            UpdateRangeItems();
        }

        private void ScanForTimeLineSliders()
        {
            // 清除现有滑块
            foreach (var slider in _trackedSliders)
            {
                UnsubscribeFromSlider(slider);
            }
            _trackedSliders.Clear();

            if (OnPingDocument() == null) return;

            // 扫描新滑块
            foreach (var obj in OnPingDocument().Objects)
            {
                if (obj is GH_NumberSlider slider && ShouldTrackSlider(slider))
                {
                    _trackedSliders.Add(slider);
                    SubscribeToSlider(slider);
                }
            }

            UpdateRangeItems();
        }

        private void UpdateRangeItems()
        {
            // 保存当前选择状态
            Dictionary<string, bool> selectedStates = new Dictionary<string, bool>();
            foreach (var item in m_userItems)
            {
                selectedStates[item.Expression] = item.Selected;
            }

            m_userItems.Clear();

            if (_trackedSliders.Count == 0)
            {
                m_userItems.Add(new Motion_ValueListItem("No TimeLine sliders found", "null", null));
                ExpireSolution(true);
                return;
            }

            // 为每个滑块创建区间项
            foreach (var slider in _trackedSliders)
            {
                double min = (double)slider.Slider.Minimum;
                double max = (double)slider.Slider.Maximum;
                var interval = new Interval(min, max);
                string expression = interval.ToString();
                
                var item = new Motion_ValueListItem(
                    $"{min}-{max}", 
                    expression, 
                    new GH_Interval(interval));

                // 恢复选择状态
                if (selectedStates.ContainsKey(expression))
                {
                    item.Selected = selectedStates[expression];
                }

                m_userItems.Add(item);
            }

            ExpireSolution(true);
        }

        public override void CreateAttributes()
        {
            m_attributes = new Motion_ValueListAttributes(this);
        }

        public void ToggleItem(int index)
        {
            if (index >= 0 && index < m_userItems.Count)
            {
                RecordUndoEvent("Toggle: " + m_userItems[index].Name);
                m_userItems[index].Selected = !m_userItems[index].Selected;
                ExpireSolution(recompute: true);
            }
        }

        public void SelectItem(int index)
        {
            if (index < 0 || index >= m_userItems.Count)
            {
                return;
            }
            bool flag = false;
            int num = m_userItems.Count - 1;
            for (int i = 0; i <= num; i++)
            {
                if (i == index)
                {
                    if (!m_userItems[i].Selected)
                    {
                        flag = true;
                        break;
                    }
                }
                else if (m_userItems[i].Selected)
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                RecordUndoEvent("Select: " + m_userItems[index].Name);
                int num2 = m_userItems.Count - 1;
                for (int j = 0; j <= num2; j++)
                {
                    m_userItems[j].Selected = j == index;
                }
                ExpireSolution(recompute: true);
            }
        }

        public void NextItem()
        {
            if (ListMode == GH_ValueListMode.CheckList || ListMode == GH_ValueListMode.DropDown || m_userItems.Count < 2)
            {
                return;
            }
            int num = 0;
            int num2 = m_userItems.Count - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (m_userItems[i].Selected)
                {
                    num = i;
                    break;
                }
            }
            int num3 = m_userItems.Count - 1;
            for (int j = 0; j <= num3; j++)
            {
                m_userItems[j].Selected = false;
            }
            num++;
            if (num == m_userItems.Count)
            {
                switch (ListMode)
                {
                    case GH_ValueListMode.Sequence:
                        num = m_userItems.Count - 1;
                        break;
                    case GH_ValueListMode.Cycle:
                        num = 0;
                        break;
                }
            }
            m_userItems[num].Selected = true;
            ExpireSolution(recompute: true);
        }

        public void PrevItem()
        {
            if (ListMode == GH_ValueListMode.CheckList || ListMode == GH_ValueListMode.DropDown || m_userItems.Count < 2)
            {
                return;
            }
            int num = 0;
            int num2 = m_userItems.Count - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (m_userItems[i].Selected)
                {
                    num = i;
                    break;
                }
            }
            int num3 = m_userItems.Count - 1;
            for (int j = 0; j <= num3; j++)
            {
                m_userItems[j].Selected = false;
            }
            num--;
            if (num == -1)
            {
                switch (ListMode)
                {
                    case GH_ValueListMode.Sequence:
                        num = 0;
                        break;
                    case GH_ValueListMode.Cycle:
                        num = m_userItems.Count - 1;
                        break;
                }
            }
            m_userItems[num].Selected = true;
            ExpireSolution(recompute: true);
        }

        protected override IGH_Goo InstantiateT()
        {
            return new GH_ObjectWrapper();
        }

        protected override void CollectVolatileData_FromSources()
        {
            base.CollectVolatileData_FromSources();
            collectedData.Clear();
            collectedData = m_data.Duplicate();
            List<Motion_ValueListItem> list = new List<Motion_ValueListItem>(m_userItems);
            m_userItems.Clear();
            List<IGH_Goo> list2 = new List<IGH_Goo>(collectedData);
            for (int i = 0; i < list2.Count; i++)
            {
                IGH_Goo iGH_Goo = list2[i];
                m_userItems.Add(new Motion_ValueListItem(iGH_Goo.ToString(), "\"" + iGH_Goo.ToString() + "\"", iGH_Goo));
                if (i < list.Count)
                {
                    m_userItems[i].Selected = list[i].Selected;
                }
            }
            CollectVolatileData_Custom();
        }

        protected override void CollectVolatileData_Custom()
        {
            m_data.Clear();
            if (_trackedSliders.Count == 0)
            {
                this.Description = "No TimeLine sliders found";
                return;
            }

            // 获取所有选中的区间
            var selectedItems = SelectedItems;
            if (selectedItems.Count == 0)
            {
                this.Description = "No ranges selected";
                return;
            }

            // 输出所有选中的区间到同一个分支
            foreach (var item in selectedItems)
            {
                if (item.Value != null)
                {
                    m_data.Append(item.Value, new GH_Path(0));  // 所有数据都放在路径 0
                }
            }

            this.Description = $"{selectedItems.Count} range(s)";
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Preview_DrawMeshes(args);
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);
        }

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, null, obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }
            foreach (object datum in m_data)
            {
                if (datum != null && datum is IGH_BakeAwareData && ((IGH_BakeAwareData)datum).BakeGeometry(doc, att, out var obj_guid))
                {
                    obj_ids.Add(obj_guid);
                }
            }
        }

        public void LoadState(string state)
        {
            foreach (Motion_ValueListItem userItem in m_userItems)
            {
                userItem.Selected = false;
            }
            if (int.TryParse(state, out var result))
            {
                if (result >= 0 && result < m_userItems.Count)
                {
                    m_userItems[result].Selected = true;
                }
                return;
            }
            int num = Math.Min(state.Length, m_userItems.Count) - 1;
            for (int i = 0; i <= num; i++)
            {
                m_userItems[i].Selected = state[i].Equals('Y');
            }
        }

        public string SaveState()
        {
            StringBuilder stringBuilder = new StringBuilder(m_userItems.Count);
            foreach (Motion_ValueListItem userItem in m_userItems)
            {
                if (userItem.Selected)
                {
                    stringBuilder.Append('Y');
                }
                else
                {
                    stringBuilder.Append('N');
                }
            }
            return stringBuilder.ToString();
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ListMode", (int)ListMode);
            writer.SetInt32("ListCount", m_userItems.Count);
            int num = m_userItems.Count - 1;
            for (int i = 0; i <= num; i++)
            {
                GH_IWriter obj = writer.CreateChunk("ListItem", i);
                obj.SetString("Name", m_userItems[i].Name);
                obj.SetString("Expression", m_userItems[i].Expression);
                obj.SetBoolean("Selected", m_userItems[i].Selected);
            }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int listMode = 1;
            reader.TryGetInt32("UIMode", ref listMode);
            reader.TryGetInt32("ListMode", ref listMode);
            ListMode = (GH_ValueListMode)listMode;
            int @int = reader.GetInt32("ListCount");
            int num = 0;
            reader.TryGetInt32("CacheCount", ref num);
            m_userItems.Clear();
            int num2 = @int - 1;
            for (int i = 0; i <= num2; i++)
            {
                GH_IReader val = reader.FindChunk("ListItem", i);
                if (val == null)
                {
                    ((GH_IChunk)reader).AddMessage("Missing chunk for List Value: " + i, (GH_Message_Type)2);
                    continue;
                }
                string @string = val.GetString("Name");
                string string2 = val.GetString("Expression");
                bool selected = false;
                val.TryGetBoolean("Selected", ref selected);
                Motion_ValueListItem motionValueListItem = new Motion_ValueListItem(@string, string2);
                motionValueListItem.Selected = selected;
                m_userItems.Add(motionValueListItem);
            }
            return base.Read(reader);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);

            // 添加列表模式切换选项
            Menu_AppendItem(menu, "Multi-select mode", (s, e) =>
            {
                ListMode = GH_ValueListMode.CheckList;
                ExpireSolution(true);
            }, true, ListMode == GH_ValueListMode.CheckList);

            Menu_AppendItem(menu, "Single-select mode", (s, e) =>
            {
                ListMode = GH_ValueListMode.DropDown;
                ExpireSolution(true);
            }, true, ListMode == GH_ValueListMode.DropDown);

            Menu_AppendSeparator(menu);

            // 为每个区间添加复选框菜单项
            foreach (var item in m_userItems)
            {
                if (item.Value == null) continue;  // 跳过无效项

                Menu_AppendItem(menu, item.Name, (s, e) =>
                {
                    item.Selected = !item.Selected;  // 切换选中状态
                    ExpireSolution(true);
                }, true, item.Selected);
            }

            Menu_AppendSeparator(menu);

            // 添加全选和取消全选选项
            Menu_AppendItem(menu, "Select All", (s, e) =>
            {
                foreach (var item in m_userItems)
                {
                    item.Selected = true;
                }
                ExpireSolution(true);
            }, m_userItems.Count > 0);

            Menu_AppendItem(menu, "Deselect All", (s, e) =>
            {
                foreach (var item in m_userItems)
                {
                    item.Selected = false;
                }
                ExpireSolution(true);
            }, m_userItems.Count > 0);
        }
    }
}