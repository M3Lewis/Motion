using Grasshopper.Kernel;
using System;
using System.Drawing;
using GH_IO.Serialization;

namespace Motion
{
    public class Param_RemoteReceiver : RemoteParam
    {
        public delegate void NickNameChangedEventHandler(IGH_DocumentObject sender, string newNickName);
        public event NickNameChangedEventHandler NickNameChanged;

        public Param_RemoteReceiver()
            : base()
        {
            nicknameKey = "";
            base.NickName = nicknameKey;
            base.WireDisplay = GH_ParamWireDisplay.hidden;
            base.Hidden = true;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ScheduleSolution(10, doc =>
            {
                UpdateGroupVisibilityAndLock();
            });
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            UpdateGroupVisibilityAndLock();
        }

        public override void ExpireSolution(bool recompute)
        {
            base.ExpireSolution(recompute);
            if (recompute)
            {
                UpdateGroupVisibilityAndLock();
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            if (!base.Write(writer)) return false;
            
            try
            {
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
                if (reader.ItemExists("NicknameKey"))
                    nicknameKey = reader.GetString("NicknameKey");
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        protected string nicknameKey = "";

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
                    ExpireSolution(true);
                }
            }
        }

        #region Overriding Name and Description
        public override string TypeName => "Motion Receiver";

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
            get => "Motion Receiver";
            set => base.Name = value;
        }
        #endregion

        public override Guid ComponentGuid => new Guid("{3f65d28a-8f48-4b85-9bc4-7ce36260d062}");

        protected override Bitmap Icon => Properties.Resources.Receiver;
    }
}
