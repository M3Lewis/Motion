// Grasshopper.Kernel.Special.GH_SliderAnimator
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Microsoft.VisualBasic.CompilerServices;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

/// <exclude />
public class MotionSliderAnimatorNoUi : GH_SliderAnimator
{
    private double m_currentFrameValue;
    private int m_currentFrameIndex;

    public MotionSliderAnimatorNoUi(GH_NumberSlider nOwner) : base(nOwner)
    {
    }

    public int MotionStartAnimation(bool isTransparent, string viewName, out List<string> outputPathList)
    {
        outputPathList = new List<string>();
        StoreSettingsAsDefault();

        // 基本验证
        if (string.IsNullOrEmpty(m_fileTemplate)) throw new Exception("File name mask is not valid");
        if (string.IsNullOrEmpty(m_folder)) throw new Exception("Destination folder path is not valid");
        if (m_owner == null) throw new Exception("Number slider reference cannot be resolved");
        if (m_frameCount < 1) throw new Exception("Insufficient frames for animation");

        // 创建输出目录
        if (!Directory.Exists(m_folder))
        {
            try
            {
                Directory.CreateDirectory(m_folder);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to create destination folder", ex);
            }
        }

        // 初始化参数
        m_resolution.Width = Math.Max(m_resolution.Width, 24);
        m_resolution.Height = Math.Max(m_resolution.Height, 24);
        m_currentFrameIndex = 0;

        GH_Document doc = m_owner.OnPingDocument();
        if (doc == null) return 0;

        // 设置动画参数
        double minValue = Convert.ToDouble(m_owner.Slider.Minimum);
        double maxValue = Convert.ToDouble(m_owner.Slider.Maximum);
        m_currentFrameValue = minValue;

        long startTicks = DateTime.Now.Ticks;

        try
        {
            while (m_currentFrameValue <= maxValue)
            {
                RhinoApp.CommandPrompt = $"Generating frame {m_currentFrameIndex} of {m_frameCount}.    Estimated time left: {EstimateTimeLeft(startTicks)}s";

                if (GH_Document.IsEscapeKeyDown()) break;

                // 更新滑块值，但不触发基类的动画逻辑
                decimal newValue = Convert.ToDecimal(m_currentFrameValue);
                if (m_owner.Slider.Value != newValue)
                {
                    m_owner.Slider.Value = newValue;
                }

                if (doc.SolutionState == GH_ProcessStep.Aborted) break;

                RhinoApp.Wait();
                Application.DoEvents();
                doc.NewSolution(false);

                // 捕获和保存帧
                Bitmap bitmap = MotionCreateFrame(isTransparent, viewName);
                if (bitmap != null)
                {
                    string fileName = string.Format(m_fileTemplate, m_currentFrameIndex);
                    string fullPath = Path.Combine(m_folder, fileName);
                    bitmap.Save(fullPath, ImageFormat.Png);
                    outputPathList.Add(fullPath);
                    RhinoApp.WriteLine($"Frame {m_currentFrameIndex} saved to disk: {fullPath}");
                    bitmap.Dispose();
                    m_currentFrameIndex++;
                }
                else
                {
                    RhinoApp.WriteLine($"Frame {m_currentFrameIndex} failed to render to DIB");
                }

                m_currentFrameValue += 1.0;
            }
        }
        finally
        {
            RhinoApp.WriteLine($"Animation saved to disk: {m_folder}\\");
            RhinoApp.CommandPrompt = string.Empty;
        }

        return m_currentFrameIndex;
    }

    public Bitmap MotionCreateFrame(bool isTransparent, string viewName)
    {
        //if (m_viewport == null)
        //{
        //    return null;
        //}

        ViewCapture viewCapture = new Rhino.Display.ViewCapture();
        viewCapture.Width = Width;
        viewCapture.Height = Height;
        viewCapture.ScaleScreenItems = false;
        viewCapture.DrawAxes = false;
        viewCapture.DrawGrid = false;
        viewCapture.DrawGridAxes = false;
        viewCapture.TransparentBackground = isTransparent;

        var myView = Rhino.RhinoDoc.ActiveDoc.Views.Find(viewName, true);
        Bitmap bitmap = viewCapture.CaptureToBitmap(myView);
        if (bitmap == null)
        {
            return null;
        }

        //if (m_drawTagBar)
        //{
        //    string text = string.Format(m_tagTemplate, m_frameIndex, m_currentValue);
        //    SizeF sizeF = GH_FontServer.MeasureString(text, GH_FontServer.Standard);
        //    Rectangle rect = new Rectangle(-1, Convert.ToInt32((float)bitmap.Height - sizeF.Height) - 5, bitmap.Width + 2, Convert.ToInt32(sizeF.Height) + 10);
        //    Graphics graphics = Graphics.FromImage(bitmap);
        //    graphics.SmoothingMode = SmoothingMode.None;
        //    graphics.TextRenderingHint = GH_TextRenderingConstants.GH_CrispText;
        //    SolidBrush solidBrush = new SolidBrush(Color.FromArgb(150, Color.White));
        //    graphics.FillRectangle(solidBrush, rect);
        //    solidBrush.Dispose();
        //    graphics.DrawLine(Pens.Black, rect.Left, rect.Y, rect.Right, rect.Y);
        //    graphics.DrawString(text, GH_FontServer.Standard, Brushes.Black, 2f, rect.Y + 2);
        //    graphics.Dispose();
        //}

        return bitmap;
    }
}
