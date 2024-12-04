using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Microsoft.VisualBasic.CompilerServices;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <exclude />
public class MotionSliderAnimatorOne : GH_SliderAnimator
{
    public string ViewName;
    public int ScreenWidth;
    public int ScreenHeight;
    public bool IsTransparent;
    public GH_NumberSlider Owner;
    public MotionSliderAnimatorOne(GH_NumberSlider myOwner,string viewName,int screenWidth,int screenHeight,bool isTransparent):base(myOwner)
    {
        Owner = myOwner;
        ViewName = viewName;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        IsTransparent = isTransparent;
    }


    public int MotionStartAnimation(ref GH_NumberSlider slider,double currentValue,out List<string> outputPath)
    {
        outputPath = new List<string>();
        slider = Owner;
        StoreSettingsAsDefault();
        if (m_fileTemplate == null)
        {
            throw new Exception("File name mask is not valid");
        }
        if (m_folder == null)
        {
            throw new Exception("Destination folder path is not valid");
        }
        if (!Directory.Exists(m_folder))
        {
            try
            {
                Directory.CreateDirectory(m_folder);
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw new Exception("Unable to create destination folder");
            }
        }

        if (m_owner == null)
        {
            throw new Exception("Number slider reference cannot be resolved");
        }
        if (m_frameCount < 1)
        {
            throw new Exception("Insufficient frames for animation");
        }
        m_resolution.Width = Math.Max(m_resolution.Width, 24);
        m_resolution.Height = Math.Max(m_resolution.Height, 24);
        m_frameIndex = 0;
        
        GH_Document gH_Document = m_owner.OnPingDocument();
        if (gH_Document == null)
        {
            return 0;
        }
        
        long ticks = DateTime.Now.Ticks;
        
        double maximum = Convert.ToDouble(m_owner.Slider.Maximum);

        while (currentValue <= maximum) 
        {
            m_currentValue = currentValue;

            RhinoApp.CommandPrompt = $"Generating frame {currentValue}.    Estimated time left: {EstimateTimeLeft(ticks)}s";
            if (GH_Document.IsEscapeKeyDown())
            {
                break;
            }
            slider.Slider.Value = Convert.ToDecimal(m_currentValue);

            // 强制 Rhino 视图更新
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

            //按了ESC viewport才会动，需要观察文件夹图片是不是在动的时候正确导出了？？？

            Bitmap bitmap = MotionCreateFrame(ViewName, ScreenWidth, ScreenHeight, IsTransparent);

            if (bitmap == null)
            {
                RhinoApp.WriteLine($"Frame {m_frameIndex} failed to render to DIB");
            }
            else
            {
                string arg = string.Format(m_fileTemplate, m_currentValue);
                string text = string.Format("{0}{2}{1}", m_folder, arg, Path.DirectorySeparatorChar);
                ImageFormat format = ImageFormat.Png;
                outputPath.Add(text);
                bitmap.Save(text, format);
                RhinoApp.WriteLine($"Frame {m_currentValue} saved to disk: {text}");
                m_frameIndex++;
                bitmap.Dispose();
            }
            currentValue++;
        }

        RhinoApp.WriteLine($"Animation saved to disk: {m_folder}\\");
        RhinoApp.CommandPrompt = string.Empty;
        return m_frameIndex;
    }

    public Bitmap MotionCreateFrame(string viewName,int width,int height,bool isTransparent)
    {
        var views = Rhino.RhinoDoc.ActiveDoc.Views;
        var myView = views.Find(viewName, true);
        Grasshopper.Instances.ActiveCanvas.Refresh();
        Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

        var viewCapture = new Rhino.Display.ViewCapture();
        viewCapture.Width = width;
        viewCapture.Height = height;
        viewCapture.ScaleScreenItems = false;
        viewCapture.DrawAxes = false;
        viewCapture.DrawGrid = false;
        viewCapture.DrawGridAxes = false;
        viewCapture.TransparentBackground = isTransparent;

        Bitmap bitmap = viewCapture.CaptureToBitmap(myView);
        if (bitmap == null)
        {
            bitmap.Dispose();
            return null;
        }
        
        return bitmap;
    }
}
