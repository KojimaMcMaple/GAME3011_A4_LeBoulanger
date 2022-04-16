/// <summary>
///  The Source file name: GlobalEnums.cs
///  Author's name: Trung Le (Kyle Hunter)
///  Student Number: 101264698
///  Program description: Global game manager script
///  Date last Modified: See GitHub
///  Revision History: See GitHub
/// </summary>
public static class GlobalEnums
{
    // https://gamedevelopment.tutsplus.com/tutorials/how-to-match-puzzle-shapes-using-bitmasks--gamedev-11759
    // Here are the bits I assigned each side of the tile:
    // ===== 1 =====
    // |           |
    // |           |
    // 8           2
    // |           |
    // |           |
    // ===== 4 =====

    // 1 == 0001 in binary
    // 2 == 0010 in binary
    // 4 == 0100 in binary
    // 8 == 1000 in binary

    public const int kBitmaskNone = 0;
    public const int kBitmaskTop = 1;
    public const int kBitmaskRight = 2;
    public const int kBitmaskBottom = 4;
    public const int kBitmaskLeft = 8;

    public enum LineTileType
    {
        Nub,
        Line,
        Corner,
        Threeway,
        Cross,
        NUM_OF_TYPES
    };

    public enum RotType
    {
        Rot0,
        Rot90,
        Rot180,
        Rot270,
        NUM_OF_TYPES
    };

    public enum PipeMatchType
    {
        Invalid, //mismatch
        ValidWithOpenSide, // The tiles don't directly connect, but not because of an unmatched edge.
        ValidWithSolidMatch, // The tiles directly connect.
        NUM_OF_TYPES
    };

    public enum VfxType
    {
        DEFAULT,
        HIT,
        GEM_CLEAR,
        BOMB,
        NUM_OF_TYPES
    };
}
