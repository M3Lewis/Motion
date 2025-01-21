namespace Motion.Widget
{
    // 关键帧数据结构
    internal class Keyframe
    {
        public int Frame { get; set; }
        public double Value { get; set; }
        public string Group { get; set; } = "Default";
        
        public Keyframe() { }
        
        public Keyframe(int frame, double value, string group = "Default")
        {
            Frame = frame;
            Value = value;
            Group = group;
        }
    }
}

