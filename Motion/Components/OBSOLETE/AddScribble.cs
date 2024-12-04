using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using System;
using System.Drawing;
using FontStyle = System.Drawing.FontStyle;
using System.Linq;
using System.Collections.Generic;

namespace Motion.Utils
{
    public class AddScribble : GH_Component
    {
        protected override Bitmap Icon => Properties.Resources.AddScribble;
        public override Guid ComponentGuid => new Guid("2F58BB1F-78B1-45EB-9F93-1E5037527233");

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public AddScribble()
          : base("Add Scribble", "Add Scribble",
              "Free to add a scribble.",
              "Motion", "03_Utils")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Add Time?", "T?", "Add time scribble?", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Add Text?", "Te?", "Add text scribble?", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Text Content", "T", "Scribble text content", GH_ParamAccess.item, "Hello!");
            pManager.AddIntegerParameter("Text Size", "S", "Scribble text size", GH_ParamAccess.item, 100);
            pManager.AddTextParameter("Font", "F", "Font type", GH_ParamAccess.item, "Arial");
            pManager.AddIntegerParameter("Font Style", "FS", "Font Style.\n 0=Regular \n 1=Bold \n 2=Italic \n 4=Underline \n 8=Strikeout", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Max Chars Per Line", "ML", "Maximum characters per line for text wrapping", GH_ParamAccess.item, 100);

            if (pManager[5] is Param_Integer param_Integer)
            {
                param_Integer.AddNamedValue("Regular", 0);
                param_Integer.AddNamedValue("Bold", 1);
                param_Integer.AddNamedValue("Italic", 2);
                param_Integer.AddNamedValue("Underline", 4);
                param_Integer.AddNamedValue("Strikeout", 8);
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iIsAddTime = false;
            bool iIsAddText = false;
            string iText = "";
            int iSize = 0;
            string iFont = "";
            int iFontStyleNum = 0;
            int iMaxCharsPerLine = 50;

            if (!DA.GetData(0, ref iIsAddTime)) return;
            if (!DA.GetData(1, ref iIsAddText)) return;
            if (!DA.GetData(2, ref iText)) return;
            if (!DA.GetData(3, ref iSize)) return;
            if (!DA.GetData(4, ref iFont)) return;
            if (!DA.GetData(5, ref iFontStyleNum)) return;
            if (!DA.GetData(6, ref iMaxCharsPerLine)) return;

            GH_Document ghDoc = Instances.ActiveCanvas.Document;

            // 获取所有选中的 GH_Group
            var selectedGroups = ghDoc.SelectedObjects().OfType<GH_Group>().ToList();

            if (selectedGroups.Any() && (iIsAddTime || iIsAddText))
            {
                // 为每个选中的组添加 Scribble
                foreach (var group in selectedGroups)
                {
                    GH_Scribble scribble = CreateScribble(iIsAddTime, iIsAddText, iText, iSize, iFont, (FontStyle)iFontStyleNum, iMaxCharsPerLine);
                    if (scribble != null)
                    {
                        // 先添加到文档
                        ghDoc.AddObject(scribble, false);

                        // 获取组的边界
                        var groupBounds = group.Attributes.Bounds;

                        // 设置 Scribble 位置在组的左上角
                        scribble.Attributes.Pivot = new PointF(
                            groupBounds.Left - 2*scribble.Attributes.Bounds.Width - 50,
                            groupBounds.Top - 2*scribble.Attributes.Bounds.Height - 50
                        );

                        RecordUndoEvent("groupScribble");
                        // 将 Scribble 添加到组中
                        group.AddObject(scribble.InstanceGuid);

                        scribble.Attributes.ExpireLayout();
                    }
                }
            }
            else if (iIsAddTime || iIsAddText)
            {
                // 原有的逻辑：如果没有选中的组，就在组件上方添加
                PointF comPivot = this.Attributes.Pivot;
                GH_Scribble scribble = CreateScribble(iIsAddTime, iIsAddText, iText, iSize, iFont, (FontStyle)iFontStyleNum, iMaxCharsPerLine);
                if (scribble != null)
                {
                    RecordUndoEvent("Scribble");
                    ghDoc.AddObject(scribble, false);
                    scribble.Attributes.Pivot = new PointF(comPivot.X, (comPivot.Y - 200));
                    scribble.Attributes.ExpireLayout();
                }
            }
        }
        private GH_Scribble CreateScribble(bool isAddTime, bool isAddText, string freeText, int size, string font, FontStyle fontStyle, int maxCharsPerLine)
        {
            GH_Scribble scribble = new GH_Scribble();

            if (!isAddTime && !isAddText) return null;

            string textToFormat = isAddText ? freeText : (isAddTime ? DateTime.Now.ToString() : "");
            
            if (!string.IsNullOrEmpty(textToFormat) && maxCharsPerLine > 0)
            {
                string formattedText = "";
                string[] separators = new string[] { "\r\n", "\r", "\n", " " }; // \r代表回车 \n代表换行
                string[] words = textToFormat.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                List<List<string>> formattedLines = new List<List<string>>();
                formattedLines.Add(new List<string>());
                int currentLineIndex = 0;
                int currentLineLength = 0;

                foreach (string word in words)
                {
                    string wordWithSpace = word + " ";

                    //小于设置的字数则一直加字，直到不满足条件，就换行
                    if (currentLineLength + wordWithSpace.Length <= maxCharsPerLine && wordWithSpace.Length < maxCharsPerLine) 
                    {
                        formattedLines[currentLineIndex].Add(wordWithSpace);
                        currentLineLength += wordWithSpace.Length;
                    }
                    else
                    {
                        formattedLines[currentLineIndex].Add(Environment.NewLine);
                        currentLineIndex++;
                        currentLineLength = wordWithSpace.Length;
                        formattedLines.Add(new List<string>());
                        formattedLines[currentLineIndex].Add(wordWithSpace);
                    }
                }

                foreach (List<string> line in formattedLines)
                {
                    foreach (string segment in line)
                    {
                        formattedText += segment;
                    }
                }

                scribble.Text = formattedText;
            }
            else
            {
                scribble.Text = textToFormat;
            }

            scribble.Font = new Font(font, size, fontStyle);
            return scribble;
        }
    }
}