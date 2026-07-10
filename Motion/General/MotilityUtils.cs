using Grasshopper.GUI.Canvas;
using Grasshopper;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Motion.Animation;

namespace Motion.General
{
    public static class MotilityUtils
    {
        // Special override of the method to handle expiring the solution in copy/paste scenarios.
        // If scheduleNew is false it just does the same thing as the default method.
        // public static void connectMatchingParams(GH_Document doc,bool scheduleNew)
        // {
        //     try
        //     {
        //         // 检查 doc 是否为 null
        //         if (doc == null)
        //         {
        //             return;
        //         }
        //
        //         connectMatchingParams(doc);
        //
        //         // 检查 ActiveCanvas 和其 Document 是否为 null
        //         if (scheduleNew && 
        //             Grasshopper.Instances.ActiveCanvas != null && 
        //             Grasshopper.Instances.ActiveCanvas.Document != null)
        //         {
        //             Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Rhino.RhinoApp.WriteLine($"Error in connectMatchingParams: {ex.Message}");
        //     }
        // }
        //
        //
        // the main method that does the work - checks all receivers and senders for matches,
        // and rewires accordingly.
        // public static void connectMatchingParams(GH_Document doc)
        // {
        //     try
        //     {
        //         // 获取活动画布和文档
        //         var activeCanvas = Grasshopper.Instances.ActiveCanvas;
        //         if (activeCanvas == null)
        //         {
        //             return;
        //         }
        //
        //         var activeDoc = activeCanvas.Document;
        //         if (activeDoc == null)
        //         {
        //             return;
        //         }
        //
        //         var activeObjects = activeDoc.ActiveObjects();
        //         if (activeObjects == null || !activeObjects.Any())
        //         {
        //             return;
        //         }
        //
        //         // 获取所有接收器和发送器
        //         var allReceivers = activeObjects
        //             .Where(x => x is Param_RemoteReceiver)
        //             .Cast<Param_RemoteReceiver>()
        //             .ToList();
        //
        //         var allSenders = activeObjects
        //             .Where(x => x is MotionSender)
        //             .Cast<MotionSender>()
        //             .ToList();
        //
        //         // 如果没有接收器，直接返回
        //         if (allReceivers == null || !allReceivers.Any())
        //         {
        //             return;
        //         }
        //
        //         // 处理每个接收器
        //         foreach (var receiver in allReceivers)
        //         {
        //             if (receiver != null)
        //             {
        //                 ProcessReceiver(allSenders ?? new List<MotionSender>(), receiver);
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         // 可以选择记录错误或显示给用户
        //         Rhino.RhinoApp.WriteLine($"Error in connectMatchingParams: {ex.Message}");
        //     }
        // }
        //
        //
        //  this method wires up a receiver to all matching senders. 
        // public static void ProcessReceiver(List<MotionSender> allSenders, Param_RemoteReceiver receiver)
        // {
        //     //get the key
        //     string key = receiver.NickName;
        //     //stop if it's empty
        //     if (string.IsNullOrEmpty(key)) return;
        //
        //     //check if the existing sources match the key, throw em out otherwise
        //     List<IGH_Param> sourcesToRemove = new List<IGH_Param>();
        //     foreach (IGH_Param param in receiver.Sources)
        //     {
        //         //if the source does not match, remove it
        //         if (!LikeOperator.LikeString(param.NickName, key, Microsoft.VisualBasic.CompareMethod.Binary))
        //         {
        //             sourcesToRemove.Add(param);
        //         }
        //     }
        //
        //     //a custom method to remove all sources at once - calling RemoveSource in a loop
        //     //was giving me trouble because it kept expiring the solution repeatedly.
        //     RemoveSources(receiver, sourcesToRemove);
        //
        //
        //
        //     //get all the senders whose nickname matches the key
        //     var matchingSenders = allSenders.Where(s => LikeOperator.LikeString(s.NickName,key,Microsoft.VisualBasic.CompareMethod.Binary));
        //
        //     //for all the matching senders
        //     foreach (MotionSender sender in matchingSenders)
        //     {
        //         //if the matching sender is not currently a source, add it
        //         if (!receiver.Sources.Contains(sender))
        //         {
        //             receiver.AddSource(sender);
        //
        //         }
        //     }
        // }
        
        public static void RemoveSources(IGH_Param target, List<IGH_Param> sources)
        {
            foreach (IGH_Param source in sources)
            {
                if (source == null) continue;
                if (!target.Sources.Contains(source))
                {
                    continue;
                }
                target.Sources.Remove(source);
                source.Recipients.Remove(target);

            }
            if (sources.Count > 0)
            {
                target.OnObjectChanged(GH_ObjectEventType.Sources);
                target.ExpireSolution(false);
            }


        }
        
        internal static string GetLastUsedKey(GH_Document doc)
        {
            return GetAllKeys(doc).Last();
        }
        
        public static List<string> GetAllKeys(GH_Document doc)
        {
            var allKeys = doc.ActiveObjects().Where(o => o is RemoteParam).Select(o => o.NickName).Distinct().ToList();
            return allKeys;
        }
        
        internal static void FindReplace(string find, string replace, bool forceExact)
        {
            var allKeys = Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects().Where(o => o is RemoteParam);
            
            foreach (var key in allKeys)
            {
                if (forceExact ? key.NickName == find : key.NickName.Contains(find))
                {
                    key.NickName = key.NickName.Replace(find, replace);
                    key.Attributes.ExpireLayout();
                }
            }
        }

        internal static void GoComponent(IGH_DocumentObject com)
        {
            PointF view_point = new PointF(com.Attributes.Pivot.X, com.Attributes.Pivot.Y);
            GH_NamedView gH_NamedView = new GH_NamedView("", view_point, 1.5f, GH_NamedViewType.center);
            foreach (IGH_DocumentObject item in com.OnPingDocument().SelectedObjects())
            {
                item.Attributes.Selected = false;
            }
            com.Attributes.Selected = true;
            gH_NamedView.SetToViewport(Instances.ActiveCanvas, 300);
        }

        internal static bool TryParseNickNameInterval(string nickname, out double min, out double max)
        {
            min = 0;
            max = 0;
            if (string.IsNullOrEmpty(nickname)) return false;

            string[] parts = nickname.Split(new char[] { '-' }, 2);
            if (parts.Length != 2) return false;

            return double.TryParse(parts[0], out min)
                   && double.TryParse(parts[1], out max);
        }
        
        public static void ShowTemporaryMessageAtLocation(GH_Canvas canvas, string message, PointF location)
        {
            GH_Canvas.CanvasPostPaintObjectsEventHandler canvasRepaint = null;
            canvasRepaint = (sender) =>
            {
                Graphics g = canvas.Graphics;
                if (g == null) return;

                var originalTransform = g.Transform;
                g.ResetTransform();

                SizeF textSize = GH_FontServer.MeasureString(message, GH_FontServer.Standard);
                RectangleF textBounds = new RectangleF(location, textSize);
                textBounds.Inflate(6, 3);

                GH_Capsule capsule = GH_Capsule.CreateTextCapsule(
                    textBounds,
                    textBounds,
                    GH_Palette.Pink,
                    message);

                capsule.Render(g, Color.LightSkyBlue);
                capsule.Dispose();

                g.Transform = originalTransform;
            };

            canvas.CanvasPostPaintObjects += canvasRepaint;
            canvas.Refresh();

            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += (s, args) =>
            {
                canvas.CanvasPostPaintObjects -= canvasRepaint;
                canvas.Refresh();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        public static void ShowTemporaryMessageAtTop(GH_Canvas canvas, string message)
        {
            // 固定位置：画布顶部居中，距离左边 330，顶部 50
            var location = new PointF(330, 50);
            ShowTemporaryMessageAtLocation(canvas, message, location);
        }
    }
}
