namespace Motion.Utils.MetroTile
{
    /// <summary>
    /// Defines the metro tile subdivision behavior for one grid cell.
    /// </summary>
    public enum TileType
    {
        /// <summary>
        /// Large tile: no subdivision.
        /// </summary>
        L,

        /// <summary>
        /// Medium tile: split into 2 horizontal parts.
        /// </summary>
        M,

        /// <summary>
        /// Small tile: split into 4 parts.
        /// </summary>
        S,

        /// <summary>
        /// Empty tile: produce no output.
        /// </summary>
        E
    }
}
