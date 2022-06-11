namespace MapAssist.Types
{
    public enum Difficulty : ushort
    {
        普通 = 0,
        惡夢 = 1,
        地獄 = 2
    }

    public static class DifficultyExtension
    {
        public static bool IsValid(this Difficulty difficulty)
        {
            return (ushort)difficulty >= 0 && (ushort)difficulty <= 2;
        }
    }
}
