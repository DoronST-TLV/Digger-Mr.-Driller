namespace Strata
{
    /// <summary>What a grid cell contains. Solid and Wall are virtual: the ungenerated floor and the side edges.</summary>
    public enum CellType
    {
        Empty = 0,
        Color0, Color1, Color2, Color3,
        Hard,      // X-block: dug alone, costs air, never part of a colour group
        Capsule,   // air capsule: dug alone, restores air
        Solid,     // rows not generated yet — always supports what is above
        Wall,      // outside the columns
    }

    public static class CellTypeExtensions
    {
        public static bool IsColor(this CellType t) => t >= CellType.Color0 && t <= CellType.Color3;
        public static bool IsBlock(this CellType t) => t.IsColor() || t == CellType.Hard || t == CellType.Capsule;
        public static int ColorIndex(this CellType t) => (int)t - (int)CellType.Color0;
        public static CellType FromColorIndex(int index) => (CellType)((int)CellType.Color0 + index);
    }
}
