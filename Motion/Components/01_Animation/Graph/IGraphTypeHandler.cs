using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Motion.Components
{
    public interface IGraphTypeHandler
    {
        Guid ComponentGuid { get; }           // 要创建哪个组件
        PointF PositionOffset { get; }        // 放在什么位置
        int InputPortIndex { get; }           // 连到哪个输入端口
        bool NeedsBezierGraph { get; }        // 是否需要默认贝塞尔图
        void PostConfigure(GH_Document doc, IGH_Component graphComponent);  // 额外的配置
    }
}