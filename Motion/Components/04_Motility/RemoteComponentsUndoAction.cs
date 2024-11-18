using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Motion.Motility
{
    public class RemoteComponentsUndoAction : IGH_UndoAction
    {
        private readonly List<IGH_DocumentObject> _components;
        private readonly GH_Document _document;
        private readonly Dictionary<Guid, List<Guid>> _outputConnections;
        private readonly Dictionary<Guid, List<Guid>> _inputConnections;
        private readonly Dictionary<Guid, List<Guid>> _mergeConnections;

        public RemoteComponentsUndoAction(List<IGH_DocumentObject> components, GH_Document document)
        {
            _components = components;
            _document = document;
            _outputConnections = new Dictionary<Guid, List<Guid>>();
            _inputConnections = new Dictionary<Guid, List<Guid>>();
            _mergeConnections = new Dictionary<Guid, List<Guid>>();

            // 保存所有连接关系
            foreach (var comp in components)
            {
                if (comp is IGH_Param param)
                {
                    // 保存输出连接
                    var outputGuids = new List<Guid>();
                    foreach (var recipient in param.Recipients)
                    {
                        outputGuids.Add(recipient.InstanceGuid);
                    }
                    if (outputGuids.Any())
                    {
                        _outputConnections[comp.InstanceGuid] = outputGuids;
                    }

                    // 保存输入连接
                    var inputGuids = new List<Guid>();
                    foreach (var source in param.Sources)
                    {
                        inputGuids.Add(source.InstanceGuid);
                    }
                    if (inputGuids.Any())
                    {
                        _inputConnections[comp.InstanceGuid] = inputGuids;
                    }

                    // 保存与 Merge 组件的连接
                    var mergeGuids = new List<Guid>();
                    if (comp is Param_RemoteTarget)
                    {
                        var mergeTargets = document.Objects
                            .Where(obj => obj.Name.Contains("MergeCameraTarget") && obj is IGH_Component)
                            .Cast<IGH_Component>();

                        foreach (var merge in mergeTargets)
                        {
                            foreach (var mergeParam in merge.Params.Input)
                            {
                                if (mergeParam.NickName == param.NickName)
                                {
                                    mergeGuids.Add(merge.InstanceGuid);
                                }
                            }
                        }
                    }
                    else if (comp is Param_RemoteLocation)
                    {
                        var mergeLocations = document.Objects
                            .Where(obj => obj.Name.Contains("MergeCameraLocation") && obj is IGH_Component)
                            .Cast<IGH_Component>();

                        foreach (var merge in mergeLocations)
                        {
                            foreach (var mergeParam in merge.Params.Input)
                            {
                                if (mergeParam.NickName == param.NickName)
                                {
                                    mergeGuids.Add(merge.InstanceGuid);
                                }
                            }
                        }
                    }

                    if (mergeGuids.Any())
                    {
                        _mergeConnections[comp.InstanceGuid] = mergeGuids;
                    }
                }
            }
        }

        public bool ExpiresSolution => true;
        public bool ExpiresDisplay => true;
        public GH_UndoState State { get; private set; }

        public void Undo(GH_Document doc)
        {
            // 先恢复所有组件
            foreach (var comp in _components)
            {
                doc.AddObject(comp, false);
            }

            // 立即恢复所有连接
            foreach (var comp in _components)
            {
                if (comp is IGH_Param param)
                {
                    // 恢复常规输出连接
                    if (_outputConnections.ContainsKey(comp.InstanceGuid))
                    {
                        foreach (var recipientGuid in _outputConnections[comp.InstanceGuid])
                        {
                            var recipient = doc.FindObject(recipientGuid, false) as IGH_Param;
                            if (recipient != null)
                            {
                                recipient.AddSource(param);
                            }
                        }
                    }

                    // 恢复常规输入连接
                    if (_inputConnections.ContainsKey(comp.InstanceGuid))
                    {
                        foreach (var sourceGuid in _inputConnections[comp.InstanceGuid])
                        {
                            var source = doc.FindObject(sourceGuid, false) as IGH_Param;
                            if (source != null)
                            {
                                param.AddSource(source);
                            }
                        }
                    }

                    // 恢复与 Merge 组件的连接
                    if (_mergeConnections.ContainsKey(comp.InstanceGuid))
                    {
                        foreach (var mergeGuid in _mergeConnections[comp.InstanceGuid])
                        {
                            var merge = doc.FindObject(mergeGuid, false) as IGH_Component;
                            if (merge != null)
                            {
                                foreach (var mergeParam in merge.Params.Input)
                                {
                                    if (mergeParam.NickName == param.NickName)
                                    {
                                        mergeParam.AddSource(param);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            State = GH_UndoState.undo;
        }

        public void Redo(GH_Document doc)
        {
            foreach (var comp in _components)
            {
                doc.RemoveObject(comp, false);
            }
            State = GH_UndoState.redo;
        }

        public bool Write(GH_IWriter writer)
        {
            // 写入组件
            writer.SetInt32("ComponentCount", _components.Count);
            for (int i = 0; i < _components.Count; i++)
            {
                writer.SetGuid($"Component_{i}", _components[i].InstanceGuid);
            }

            // 写入输出连接
            writer.SetInt32("OutputConnectionCount", _outputConnections.Count);
            int outIndex = 0;
            foreach (var kvp in _outputConnections)
            {
                writer.SetGuid($"OutputSource_{outIndex}", kvp.Key);
                writer.SetInt32($"OutputRecipientCount_{outIndex}", kvp.Value.Count);
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    writer.SetGuid($"OutputRecipient_{outIndex}_{i}", kvp.Value[i]);
                }
                outIndex++;
            }

            // 写入输入连接
            writer.SetInt32("InputConnectionCount", _inputConnections.Count);
            int inIndex = 0;
            foreach (var kvp in _inputConnections)
            {
                writer.SetGuid($"InputTarget_{inIndex}", kvp.Key);
                writer.SetInt32($"InputSourceCount_{inIndex}", kvp.Value.Count);
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    writer.SetGuid($"InputSource_{inIndex}_{i}", kvp.Value[i]);
                }
                inIndex++;
            }

            // 写入 Merge 连接
            writer.SetInt32("MergeConnectionCount", _mergeConnections.Count);
            int mergeIndex = 0;
            foreach (var kvp in _mergeConnections)
            {
                writer.SetGuid($"MergeSource_{mergeIndex}", kvp.Key);
                writer.SetInt32($"MergeCount_{mergeIndex}", kvp.Value.Count);
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    writer.SetGuid($"Merge_{mergeIndex}_{i}", kvp.Value[i]);
                }
                mergeIndex++;
            }

            return true;
        }

        public bool Read(GH_IReader reader)
        {
            _components.Clear();
            _outputConnections.Clear();
            _inputConnections.Clear();
            _mergeConnections.Clear();

            // 读取组件
            int componentCount = reader.GetInt32("ComponentCount");
            for (int i = 0; i < componentCount; i++)
            {
                var guid = reader.GetGuid($"Component_{i}");
                var obj = _document.FindObject(guid, false);
                if (obj != null)
                {
                    _components.Add(obj);
                }
            }

            // 读取输出连接
            int outputCount = reader.GetInt32("OutputConnectionCount");
            for (int i = 0; i < outputCount; i++)
            {
                var sourceGuid = reader.GetGuid($"OutputSource_{i}");
                int recipientCount = reader.GetInt32($"OutputRecipientCount_{i}");
                var recipients = new List<Guid>();
                for (int j = 0; j < recipientCount; j++)
                {
                    recipients.Add(reader.GetGuid($"OutputRecipient_{i}_{j}"));
                }
                _outputConnections[sourceGuid] = recipients;
            }

            // 读取输入连接
            int inputCount = reader.GetInt32("InputConnectionCount");
            for (int i = 0; i < inputCount; i++)
            {
                var targetGuid = reader.GetGuid($"InputTarget_{i}");
                int sourceReadCount = reader.GetInt32($"InputSourceCount_{i}");
                var sources = new List<Guid>();
                for (int j = 0; j < sourceReadCount; j++)
                {
                    sources.Add(reader.GetGuid($"InputSource_{i}_{j}"));
                }
                _inputConnections[targetGuid] = sources;
            }

            // 读取 Merge 连接
            int mergeCount = reader.GetInt32("MergeConnectionCount");
            for (int i = 0; i < mergeCount; i++)
            {
                var sourceGuid = reader.GetGuid($"MergeSource_{i}");
                int mergeReadCount = reader.GetInt32($"MergeCount_{i}");
                var merges = new List<Guid>();
                for (int j = 0; j < mergeReadCount; j++)
                {
                    merges.Add(reader.GetGuid($"Merge_{i}_{j}"));
                }
                _mergeConnections[sourceGuid] = merges;
            }

            return true;
        }
    }
} 