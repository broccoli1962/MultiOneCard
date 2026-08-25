namespace Backend.App
{
    /// <summary>
    /// PC 화면 형태. Unity <c>FullScreenMode</c> 와 1:1이 아니다.
    /// </summary>
    public enum DisplayWindowMode
    {
        /// <summary>테두리 있는 창.</summary>
        Windowed = 0,

        /// <summary>독점 전체화면.</summary>
        Fullscreen = 1,

        /// <summary>테두리 없는 창(전체 창).</summary>
        Borderless = 2,
    }
}
