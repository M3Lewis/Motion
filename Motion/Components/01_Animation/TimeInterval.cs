using Grasshopper;
using Grasshopper.Kernel;
using Motion.General;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Motion.Animation
{
    public class TimeInterval : GH_Component
    {
        protected override System.Drawing.Bitmap Icon => Properties.Resources.TimeInterval;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("D2C75940-DF88-4BFD-B398-4A77A488AF27");
        public TimeInterval()
            : base("Time Interval", "Time Interval", "获取时间区间", "Motion", "01_Animation")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Input Data",
                "I",
                "输入数据，可接入Motion Sender/Event的输出端",
                GH_ParamAccess.item
            );
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntervalParameter("Range", "R", "时间区间", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            ToolStripMenuItem recentKeyMenu = Menu_AppendItem(menu, "选择区间");

            // 获取所有区间并排序
            var sortedKeys = MotilityUtils.GetAllKeys(Instances.ActiveCanvas.Document)
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k => {
                    var parts = k.Split('-');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double start) &&
                        double.TryParse(parts[1], out double end))
                    {
                        return new { Key = k, Start = start, End = end };
                    }
                    return new { Key = k, Start = double.MaxValue, End = double.MaxValue };
                })
                .OrderBy(x => x.Start)
                .ToList();

            // 创建多列布局
            const int maxItemsPerColumn = 20;
            int totalItems = sortedKeys.Count;
            int columnsNeeded = (int)Math.Ceiling((double)totalItems / maxItemsPerColumn);

            // 创建列菜单
            for (int col = 0; col < columnsNeeded; col++)
            {
                int startIdx = col * maxItemsPerColumn;
                int endIdx = Math.Min(startIdx + maxItemsPerColumn, totalItems);

                if (endIdx <= startIdx) break;

                var columnItems = sortedKeys.GetRange(startIdx, endIdx - startIdx);
                var firstItem = columnItems.First();
                var lastItem = columnItems.Last();

                // 创建列标题
                string columnTitle = $"{firstItem.Start:0}-{lastItem.End:0}";
                var columnMenu = new ToolStripMenuItem(columnTitle);

                // 添加区间项
                foreach (var item in columnItems)
                {
                    var menuItem = new ToolStripMenuItem(item.Key);
                    menuItem.Click += Menu_KeyClicked;
                    columnMenu.DropDownItems.Add(menuItem);
                }

                // 将列添加到主菜单
                recentKeyMenu.DropDownItems.Add(columnMenu);
            }
        }
        protected void Menu_KeyClicked(object sender, EventArgs e)
        {
            ToolStripMenuItem keyItem = (ToolStripMenuItem)sender;
            this.NickName = keyItem.Text;
            this.Attributes.ExpireLayout();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double iData = 0d;

            Interval oRange = Interval.Unset;

            DA.GetData(0, ref iData);

            IGH_DocumentObject obj = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.DocObject; //获取上一个电池
            string rangeStr = "";
            if (obj is MotionSender || obj is EventComponent)
            {
                rangeStr = obj.NickName;
            }
            else if (obj is MotionSlider)
            { 
                rangeStr = obj.NickName;
            }
            string[] splitStr = rangeStr.Split('-');
            double start = double.Parse(splitStr[0]);
            double end = double.Parse(splitStr[1]);
            oRange = new Interval(start, end);

            DA.SetData(0, oRange);
        }
    }
}
