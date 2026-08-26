namespace Backend.Object.Management
{
    /// <summary>
    /// 브라우저 WebGL 플레이어인지. 에디터 Play 는 false.
    /// </summary>
    public static class WebBuild
    {
        /// <summary>실제 WebGL 빌드에서만 true.</summary>
        public static bool IsPlayer
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
