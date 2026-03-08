namespace Game.Consts
{
    public static class ResetType
    {
        public const int Permanent = 0;
        public const int Daily = 1;
        public const int Weekly = 2;
        public const int Monthly = 3;
    }

    public static class CurrencyId
    {
        public const int Gold = 1;
        public const int Diamond = 2;
    }

    public static class GameTimeConsts
    {
        // 每日重置的小时点 (0-23)
        public const int DailyResetHour = 4;
        
        // 每周重置是周几 (DayOfWeek, 1 是周一)
        public const int WeeklyResetDay = 1;
    }
}
