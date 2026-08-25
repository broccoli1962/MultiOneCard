using Game.Rules;

namespace Backend.App
{
    /// <summary>
    /// 손패 미리보기용 특수 카드 능력 한 줄 설명.
    /// </summary>
    public static class CardAbilityText
    {
        /// <summary>
        /// defId 기준 설명. 특수·공격·랭크 효과가 없으면 빈 문자열.
        /// </summary>
        public static string Describe(string defId)
        {
            if (string.IsNullOrEmpty(defId))
            {
                return string.Empty;
            }

            var catalog = CardCatalog.BuildOfficial();
            if (!catalog.TryGetDef(defId, out var def))
            {
                return string.Empty;
            }

            return Describe(def);
        }

        /// <summary>카드 정의 기준 설명.</summary>
        public static string Describe(CardDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            switch (def.Spec)
            {
                case SpecKind.JokerColor:
                    return "적색 조커. 같은 색일 때 공격(+값). 조커 위에서는 색 무관.";
                case SpecKind.JokerBw:
                    return "흑색 조커. 같은 색일 때 공격(+값). 조커 위에서는 색 무관.";
                case SpecKind.JokerMoon:
                    return "청색 조커. 같은 색일 때 공격(+값). 조커 위에서는 색 무관.";
                case SpecKind.Spear:
                    return "죽창. 공격 +5. top일 때만 3·4로 막을 수 없음.";
                case SpecKind.Pass:
                    return "패스. 공격 스택을 유지한 채 다음 사람에게 넘김.";
                case SpecKind.ReverseJoker:
                    return "리버스 조커. 조커 공격값을 순환.";
                case SpecKind.Counter:
                    return "역날검. 공격 스택을 2배로 직전 상대에게.";
                case SpecKind.Mirror:
                    return "미러. 다른 사람의 손패 장수를 내 장수에 맞춤.";
                case SpecKind.Pill:
                    return "알약. 1장 뽑고 해당 색만 낼 수 있게 함.";
            }

            switch (def.Rank)
            {
                case Rank.Two:
                    return "공격 +2. 같은 랭크(2) 또는 같은 문양의 A로 이어가기.";
                case Rank.Ace:
                    return "공격 +3. 같은 랭크(A) 또는 같은 문양의 2로 이어가기.";
                case Rank.Three:
                case Rank.Four:
                    return "방어. 공격 중 같은 문양(조커는 같은 색), Q 지급은 같은 문양으로 막음.";
                case Rank.Seven:
                    return "문양 지정. 다음 사람은 고른 문양을 따라야 함.";
                case Rank.Jack:
                    return "스킵. 다음 사람 한 명을 건너뜀.";
                case Rank.Queen:
                    return "리버스(방향 반전) 또는 기브(다음에게 손패 1장. 같은 문양 3·4로 막을 수 있음).";
                case Rank.King:
                    return "엑스트라(합법 1장 더) 또는 하이드(한 장 숨김).";
                default:
                    return string.Empty;
            }
        }
    }
}
