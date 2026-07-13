using System.Collections.Generic;

namespace Motion.Utils.MetroTile
{
    /// <summary>
    /// Represents one parsed tile rule.
    /// </summary>
    public sealed record TileRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileRule"/> class.
        /// </summary>
        /// <param name="type">Tile type.</param>
        /// <param name="targetColumn">Target column; <c>null</c> means wildcard.</param>
        /// <param name="targetRow">Target row; <c>null</c> means wildcard.</param>
        /// <param name="preservedIndices">
        /// Optional preserved sub-indices. If null, defaults are used for <see cref="TileType.M"/> and <see cref="TileType.S"/>.
        /// </param>
        public TileRule(TileType type, int? targetColumn, int? targetRow, IEnumerable<int> preservedIndices = null)
        {
            Type = type;
            TargetColumn = targetColumn;
            TargetRow = targetRow;
            PreservedIndices = BuildPreservedIndices(type, preservedIndices);
        }

        /// <summary>
        /// Gets the tile type.
        /// </summary>
        public TileType Type { get; }

        /// <summary>
        /// Gets the target column; <c>null</c> means wildcard (<c>*</c>).
        /// </summary>
        public int? TargetColumn { get; }

        /// <summary>
        /// Gets the target row; <c>null</c> means wildcard (<c>*</c>).
        /// </summary>
        public int? TargetRow { get; }

        /// <summary>
        /// Gets the set of preserved sub-indices.
        /// </summary>
        public HashSet<int> PreservedIndices { get; }

        private static HashSet<int> BuildPreservedIndices(TileType type, IEnumerable<int> input)
        {
            switch (type)
            {
                case TileType.L:
                case TileType.E:
                    return new HashSet<int>();
                case TileType.M:
                    return input == null ? new HashSet<int> { 0, 1 } : new HashSet<int>(input);
                case TileType.S:
                    return input == null ? new HashSet<int> { 0, 1, 2, 3 } : new HashSet<int>(input);
                default:
                    return new HashSet<int>();
            }
        }
    }
}
