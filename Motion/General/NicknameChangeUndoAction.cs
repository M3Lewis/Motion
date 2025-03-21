using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using Motion.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Motion.General
{
    // 自定义撤销动作类
    public class NicknameChangeUndoAction : IGH_UndoAction
    {
        private readonly MotionSender _sender;
        private readonly string _oldNickname;
        private readonly string _newNickname;

        public NicknameChangeUndoAction(MotionSender sender, string oldNickname, string newNickname)
        {
            _sender = sender;
            _oldNickname = oldNickname;
            _newNickname = newNickname;
        }

        public string Name => "修改 Motion Sender 区间";

        public bool ExpiresSolution => true;

        public bool ExpiresDisplay => true;

        public GH_UndoState State => GH_UndoState.undo;

        public bool Read(GH_IReader reader)
        {
            throw new NotImplementedException();
        }

        public bool Redo()
        {
            _sender.NickName = _newNickname;
            var doc = _sender.OnPingDocument();
            if (doc != null) doc.ExpireSolution();
            return true;
        }

        public void Redo(GH_Document doc)
        {
            throw new NotImplementedException();
        }

        public bool Undo()
        {
            _sender.NickName = _oldNickname;
            var doc = _sender.OnPingDocument();
            if (doc != null) doc.ExpireSolution();
            return true;
        }

        public void Undo(GH_Document doc)
        {
            throw new NotImplementedException();
        }

        public bool Write(GH_IWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
