using System.Collections.Generic;
using Motion.Components;

namespace Motion.General
{
    public class GraphTypeHandlerRegistry
    {
        public static readonly Dictionary<string, IGraphTypeHandler> Handlers = new()
        {
            ["Graph Mapper"] = new GraphMapperHandler(),
            ["Graph-Mapper +"] = new GraphMapperPlusHandler(),
            ["V-Ray Graph"] = new VRayGraphHandler(),
            ["Rich Graph Mapper"] = new RichGraphMapperHandler()
        };
    }
}