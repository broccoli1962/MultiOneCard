// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162 // 접근할 수 없는 코드가 있습니다.

namespace Util
{
    public static class PlatformUtil
    {
        public static bool IsEditor()
        {
#if UNITY_EDITOR
            return true;
#endif
            return false;
        }

        public static bool IsDev()
        {
#if DEV
            return true;
#endif
            return false;
        }

        public static bool IsDevOrEditor()
        {
            return IsEditor() || IsDev();
        }

        public static bool IsReal()
        {
#if MARKET_BUILD
            return true;
#endif
            return false;
        }
    }
}
