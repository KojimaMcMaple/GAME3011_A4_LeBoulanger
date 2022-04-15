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
    public enum ObjType
    {
        DEFAULT,
        PLAYER,
        ENEMY,
        BOSS,
        NUM_OF_TYPES
    };

    public enum FlinchType
    {
        NO_FLINCH = -1,
        DEFAULT,
        ABSOLUTE,
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

    public enum EnemyState
    {
        IDLE,
        MOVE_TO_TARGET,
        ATTACK,
        FLEE,
        STUNNED,
        DIE,
        NUM_OF_STATES
    };

    public enum MovingPlatformDir
    {
        HORIZONTAL,
        VERTICAL,
        UP_RIGHT,
        DOWN_RIGHT,
        UP_LEFT,
        DOWN_LEFT,
        NUM_OF_DIR,
    };
}
