using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;

namespace Motion
{
    #region TabIcon
    public class MotionCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Motion", Properties.Resources.Motion_Logo);
            Instances.ComponentServer.AddCategorySymbolName("Motion", 'M');
            return GH_LoadingInstruction.Proceed;
        }
    }
    #endregion
    public class MotionInfo : GH_AssemblyInfo
    {
        public static string WorkingFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Motion");
        public override string Name => "Motion";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.Motion_Logo;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Making GH Animation much easier";

        public override Guid Id => new Guid("06d887a9-24f8-4b07-9095-d77986aaaedd");

        //Return a string identifying you or your company.
        public override string AuthorName => "M3_Lewis";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "997629445@qq.com";

        public override string Version => "0.8.0";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => "0.8.0";
    }
}