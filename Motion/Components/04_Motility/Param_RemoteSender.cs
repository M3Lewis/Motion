using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using GH_IO.Serialization;
using System.Windows.Forms;

namespace Motion
{
    public class Param_RemoteSender : RemoteParam
    {
        private bool _autoRename = true;
        
        public Param_RemoteSender()
            : base()
        {
            nicknameKey = "LMAO";
            base.NickName = nicknameKey;
            base.Hidden = true;
        }

        protected string nicknameKey = "";

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            
            if (this.Sources.Count > 0) return;

            var doc = OnPingDocument();
            if (doc == null) return;

            var sliders = doc.Objects
                .Where(o => o.GetType().ToString() == "pOd_GH_Animation.L_TimeLine.pOd_TimeLineSlider")
                .Cast<GH_NumberSlider>()
                .ToList();

            if (sliders.Any())
            {
                var closestSlider = FindClosestSlider(sliders);
                if (closestSlider != null)
                {
                    this.AddSource(closestSlider);
                    
                    if (_autoRename)
                    {
                        UpdateNicknameFromSlider(closestSlider);
                        closestSlider.NickName = this.NickName;
                    }
                }
            }
        }

        private GH_NumberSlider FindClosestSlider(List<GH_NumberSlider> sliders)
        {
            var myPivot = this.Attributes.Pivot;
            var closestDist = double.MaxValue;
            GH_NumberSlider closestSlider = null;

            foreach (var slider in sliders)
            {
                var sliderPivot = slider.Attributes.Pivot;
                var dist = Math.Abs(myPivot.Y - sliderPivot.Y);
                if (dist < closestDist && dist < 100)
                {
                    closestDist = dist;
                    closestSlider = slider;
                }
            }

            return closestSlider;
        }

        private void UpdateNicknameFromSlider(GH_NumberSlider slider)
        {
            var range = new Interval((double)slider.Slider.Minimum, (double)slider.Slider.Maximum);
            var rangeStr = range.ToString();
            var splitStr = rangeStr.Split(',');
            var newNickname = string.Join("-", splitStr);
            
            this.NickName = newNickname;
            slider.NickName = newNickname;
            
            foreach (var recipient in this.Recipients)
            {
                if (recipient is Param_RemoteReceiver receiver)
                {
                    receiver.NickName = newNickname;
                }
            }
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            
            if (_autoRename && this.Sources.Count == 1)
            {
                var source = this.Sources[0];
                if (source is GH_NumberSlider slider)
                {
                    UpdateNicknameFromSlider(slider);
                    slider.NickName = this.NickName;
                }
            }
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Auto Rename From Slider", AutoRename_Clicked, true, _autoRename);
            Menu_AppendSeparator(menu);

            ToolStripMenuItem recentKeyMenu = GH_DocumentObject.Menu_AppendItem(menu, "Keys");
            foreach (string key in MotilityUtils.GetAllKeys(Grasshopper.Instances.ActiveCanvas.Document).OrderBy(s => s))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    Menu_AppendItem(recentKeyMenu.DropDown, key, new EventHandler(Menu_KeyClicked));
                }
            }
        }

        private void AutoRename_Clicked(object sender, EventArgs e)
        {
            _autoRename = !_autoRename;
            if (_autoRename && this.Sources.Count > 0)
            {
                var source = this.Sources[0];
                if (source is GH_NumberSlider slider)
                {
                    UpdateNicknameFromSlider(slider);
                    slider.NickName = this.NickName;
                }
            }
        }

        public override string NickName
        {
            get
            {
                nicknameKey = base.NickName;
                return nicknameKey;
            }
            set
            {
                nicknameKey = value;
                base.NickName = nicknameKey;

                GH_Document doc = this.OnPingDocument();
                if (doc != null) 
                {
                    doc.ScheduleSolution(10, MotilityUtils.connectMatchingParams);
                }
            }
        }

        #region Overriding Name and Description
        public override string TypeName => "Motion Sender";

        public override string Category
        {
            get => "Motion";
            set => base.Category = value;
        }
        public override string SubCategory
        {
            get => "04_Motility";
            set => base.SubCategory = value;
        }

        public override string Name
        {
            get => "Motion Sender";
            set => base.Name = value;
        }
        #endregion

        public override Guid ComponentGuid => new Guid("{28fb5992-ed75-4c89-ae8a-3cb4bb3c5227}");
        protected override Bitmap Icon => Properties.Resources.Sender;

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;
            
            try
            {
                writer.SetBoolean("AutoRename", _autoRename);
                writer.SetString("NicknameKey", nicknameKey);
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
                if (reader.ItemExists("AutoRename"))
                    _autoRename = reader.GetBoolean("AutoRename");
                if (reader.ItemExists("NicknameKey"))
                    nicknameKey = reader.GetString("NicknameKey");
            }
            catch
            {
                return false;
            }
            
            return true;
        }
    }
}
