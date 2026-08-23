using Backend.AddressableKey;

namespace Backend.App
{
        /// <summary>
    /// CardDefId 를 Addressable 주소 Cards/{CardDefId} 로 매핑한다.
    /// CardView 가 Assets/GameResource/Data/Cards 앞면·뒷면 스프라이트를 로드한다.
    /// PNG 는 768×1080 으로 그린다.
    /// </summary>
    public static class CardArtKeys
    {
        public const string BackId = "BACK";
        public const string AddressPrefix = "Cards/";
        public const string DefAddressTemplate = "Cards/{CardDefId}";

        /// <summary>앞면·BACK PNG 가로 픽셀.</summary>
        public const int PixelWidth = 768;

        /// <summary>앞면·BACK PNG 세로 픽셀.</summary>
        public const int PixelHeight = 1080;

        /// <summary>
        /// 앞면 주소. 예: Cards/SA, Cards/JOKER:COLOR.
        /// </summary>
        public static string FrontAddress(string cardDefId)
        {
            if (string.IsNullOrEmpty(cardDefId))
            {
                return null;
            }

            var mapped = AddressableKeys.Cards.Get(cardDefId);
            return string.IsNullOrEmpty(mapped) ? AddressPrefix + cardDefId : mapped;
        }

        /// <summary>
        /// 공통 뒷면 주소 Cards/BACK.
        /// </summary>
        public static string BackAddress()
        {
            var mapped = AddressableKeys.Cards.Get(BackId);
            return string.IsNullOrEmpty(mapped) ? AddressPrefix + BackId : mapped;
        }
    }
}
