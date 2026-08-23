using Backend.Net;
using Game.Rules;

namespace Backend.App
{
    /// <summary>
    /// 공개 매치 뷰 + 손패 def 로 <see cref="LegalMove"/> 힌트를 계산한다.
    /// 판정은 호스트만 한다.
    /// </summary>
    public static class LegalHint
    {
        /// <summary>
        /// 이 def 를 현재 공개 상태에 낼 수 있는지. 없으면 false.
        /// </summary>
        public static bool CanPlay(PublicMatchView match, string defId)
        {
            if (match == null || string.IsNullOrEmpty(defId) || string.IsNullOrEmpty(match.discardTop))
            {
                return false;
            }

            var catalog = CardCatalog.BuildOfficial();
            if (!catalog.TryGetDef(defId, out var card) || !catalog.TryGetDef(match.discardTop, out var top))
            {
                return false;
            }

            return LegalMove.CanPlay(
                card,
                top,
                match.attackStack,
                match.queenStack,
                ParseSuit(match.requiredSuit),
                ParseColor(match.requiredColor),
                match.spearInStack,
                counterUsedInChain: false);
        }

        /// <summary>SuitCode 문자열을 룰 Suit 로 바꾼다.</summary>
        public static Suit? ParseSuit(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            switch (code)
            {
                case SuitCode.Spade:
                    return Suit.Spade;
                case SuitCode.Heart:
                    return Suit.Heart;
                case SuitCode.Diamond:
                    return Suit.Diamond;
                case SuitCode.Club:
                    return Suit.Club;
                case SuitCode.Star:
                    return Suit.Star;
                case SuitCode.Moon:
                    return Suit.Moon;
                default:
                    return null;
            }
        }

        /// <summary>ColorCode 문자열을 룰 ColorGroup 으로 바꾼다.</summary>
        public static ColorGroup? ParseColor(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            switch (code)
            {
                case ColorCode.Black:
                    return ColorGroup.Black;
                case ColorCode.Red:
                    return ColorGroup.Red;
                case ColorCode.Blue:
                    return ColorGroup.Blue;
                default:
                    return null;
            }
        }
    }
}
