using Grasshopper.GUI.Canvas;
using Grasshopper;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Motion.Animation;

namespace Motion.General
{
    public static class MotilityUtils
    {
        //Special override of the method to handle expiring the solution in copy/paste scenarios.
        //If scheduleNew is false it just does the same thing as the default method.
        //public static void connectMatchingParams(GH_Document doc,bool scheduleNew)
        //{
        //    try
        //    {
        //        // 检查 doc 是否为 null
        //        if (doc == null)
        //        {
        //            return;
        //        }

        //        connectMatchingParams(doc);

        //        // 检查 ActiveCanvas 和其 Document 是否为 null
        //        if (scheduleNew && 
        //            Grasshopper.Instances.ActiveCanvas != null && 
        //            Grasshopper.Instances.ActiveCanvas.Document != null)
        //        {
        //            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Rhino.RhinoApp.WriteLine($"Error in connectMatchingParams: {ex.Message}");
        //    }
        //}


        //the main method that does the work - checks all receivers and senders for matches,
        //and rewires accordingly.
        //public static void connectMatchingParams(GH_Document doc)
        //{
        //    try
        //    {
        //        // 获取活动画布和文档
        //        var activeCanvas = Grasshopper.Instances.ActiveCanvas;
        //        if (activeCanvas == null)
        //        {
        //            return;
        //        }

        //        var activeDoc = activeCanvas.Document;
        //        if (activeDoc == null)
        //        {
        //            return;
        //        }

        //        var activeObjects = activeDoc.ActiveObjects();
        //        if (activeObjects == null || !activeObjects.Any())
        //        {
        //            return;
        //        }

        //        // 获取所有接收器和发送器
        //        var allReceivers = activeObjects
        //            .Where(x => x is Param_RemoteReceiver)
        //            .Cast<Param_RemoteReceiver>()
        //            .ToList();

        //        var allSenders = activeObjects
        //            .Where(x => x is MotionSender)
        //            .Cast<MotionSender>()
        //            .ToList();

        //        // 如果没有接收器，直接返回
        //        if (allReceivers == null || !allReceivers.Any())
        //        {
        //            return;
        //        }

        //        // 处理每个接收器
        //        foreach (var receiver in allReceivers)
        //        {
        //            if (receiver != null)
        //            {
        //                ProcessReceiver(allSenders ?? new List<MotionSender>(), receiver);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // 可以选择记录错误或显示给用户
        //        Rhino.RhinoApp.WriteLine($"Error in connectMatchingParams: {ex.Message}");
        //    }
        //}


        // this method wires up a receiver to all matching senders. 
        //public static void ProcessReceiver(List<MotionSender> allSenders, Param_RemoteReceiver receiver)
        //{
        //    //get the key
        //    string key = receiver.NickName;
        //    //stop if it's empty
        //    if (string.IsNullOrEmpty(key)) return;

        //    //check if the existing sources match the key, throw em out otherwise
        //    List<IGH_Param> sourcesToRemove = new List<IGH_Param>();
        //    foreach (IGH_Param param in receiver.Sources)
        //    {
        //        //if the source does not match, remove it
        //        if (!LikeOperator.LikeString(param.NickName, key, Microsoft.VisualBasic.CompareMethod.Binary))
        //        {
        //            sourcesToRemove.Add(param);
        //        }
        //    }

        //    //a custom method to remove all sources at once - calling RemoveSource in a loop
        //    //was giving me trouble because it kept expiring the solution repeatedly.
        //    RemoveSources(receiver, sourcesToRemove);



        //    //get all the senders whose nickname matches the key
        //    var matchingSenders = allSenders.Where(s => LikeOperator.LikeString(s.NickName,key,Microsoft.VisualBasic.CompareMethod.Binary));

        //    //for all the matching senders
        //    foreach (MotionSender sender in matchingSenders)
        //    {
        //        //if the matching sender is not currently a source, add it
        //        if (!receiver.Sources.Contains(sender))
        //        {
        //            receiver.AddSource(sender);

        //        }
        //    }
        //}


        //this method safely handles removing multiple sources at a time. 
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

        // utility method to get the last added key for the purposes of the .. shortcut
        internal static string GetLastUsedKey(GH_Document doc)
        {
            return GetAllKeys(doc).Last();
        }

        //retrieve all keys in the current document
        public static List<string> GetAllKeys(GH_Document doc)
        {
            var allKeys = doc.ActiveObjects().Where(o => o is RemoteParam).Select(o => o.NickName).Distinct().ToList();
            return allKeys;
        }

        // iterate over all the remoteParams in the doc and find and replace text in their names. 
        internal static void FindReplace(string find, string replace, bool forceExact)
        {
            //get all the remote params
            var allKeys = Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects().Where(o => o is RemoteParam);
            //for all the remote params
            foreach (var key in allKeys)
            {
                // if the key matches the string
                if (forceExact ? key.NickName == find : key.NickName.Contains(find))
                {
                    //replace the text with the new string
                    key.NickName = key.NickName.Replace(find, replace);
                    //clean up component display
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


    }
}
