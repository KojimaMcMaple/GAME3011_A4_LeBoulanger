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
    public enum LineTileType
    {
        Nub,
        Line,
        Corner,
        Threeway,
        Cross,
        NUM_OF_TYPES
    };

    public enum RotationType
    {
        Rotation0,
        Rotation90,
        Rotation180,
        Rotation270,
        NUM_OF_TYPES
    };

    public enum TileConnectionType
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
