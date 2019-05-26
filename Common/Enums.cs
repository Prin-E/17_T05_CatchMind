namespace T05.CatchMind.Common
{
    /// <summary>
    /// 색상 종류
    /// </summary>
    public enum ColorType : byte
    {
        Black = 0,
        Red,
        Yellow,
        Green,
        Blue,
        White
    }

    /// <summary>
    /// 마우스 (또는 터치) 이벤트 종류
    /// </summary>
    public enum Phase : byte
    {
        Began,  // Down
        Moved,  // Move
        Ended   // Up, Cancelled
    }
}