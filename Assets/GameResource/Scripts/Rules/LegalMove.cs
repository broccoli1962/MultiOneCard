namespace Game.Rules
{
    /// <summary>
    /// Official 합법 수. 클라 힌트와 서버가 같은 함수를 쓴다.
    /// 소유·턴 검사는 포함하지 않는다. 그 검사는 accept 직전에 한다.
    /// </summary>
    public static class LegalMove
    {
        /// <summary>
        /// 이 장을 현재 버림 상태에 낼 수 있는지 판정한다.
        /// 공격 응답·Q 응답·알약 색 락·일반 매칭을 구분한다.
        /// </summary>
        public static bool CanPlay(MatchState state, CardInstance card)
        {
            return CanPlay(state, card.Def);
        }

        /// <summary>
        /// instanceId(0..90) 장을 현재 버림 상태에 낼 수 있는지 판정한다.
        /// </summary>
        public static bool CanPlay(MatchState state, int instanceId)
        {
            if (!state.Catalog.TryGetInstance(instanceId, out var card))
            {
                return false;
            }

            return CanPlay(state, card.Def);
        }

        /// <summary>
        /// 이 정의를 현재 버림 상태에 낼 수 있는지 판정한다.
        /// </summary>
        public static bool CanPlay(MatchState state, CardDef card)
        {
            if (state.AttackStack > 0)
            {
                return CanPlayAttackResponse(state, card);
            }

            if (state.QueenStack > 0)
            {
                return CanPlayQueenResponse(card);
            }

            if (state.RequiredColor.HasValue)
            {
                return CanPlayColorLock(state.RequiredColor.Value, card);
            }

            return CanPlayNormal(state, card);
        }

        /// <summary>
        /// 공격 응답: PASS, COUNTER, SPEAR, (죽창 없으면) 3·4, 또는 공격 카드(2/A/조커).
        /// </summary>
        private static bool CanPlayAttackResponse(MatchState state, CardDef card)
        {
            if (card.Spec == SpecKind.Pass
                || card.Spec == SpecKind.Counter
                || card.Spec == SpecKind.Spear)
            {
                return true;
            }

            if (IsThreeOrFour(card))
            {
                return !state.SpearInStack;
            }

            return card.Rank == Rank.Two || card.Rank == Rank.Ace || card.IsJoker;
        }

        /// <summary>
        /// Q 응답: Q 또는 3·4.
        /// </summary>
        private static bool CanPlayQueenResponse(CardDef card)
        {
            return card.Rank == Rank.Queen || IsThreeOrFour(card);
        }

        /// <summary>
        /// 알약 락: 그 색 문양, 또는 같은 색 알약. 조커·다른 무색 불가.
        /// </summary>
        private static bool CanPlayColorLock(ColorGroup requiredColor, CardDef card)
        {
            if (card.Color != requiredColor)
            {
                return false;
            }

            return card.IsTrump || card.Spec == SpecKind.Pill;
        }

        /// <summary>
        /// 일반 수: 와일드(패스·역날검 제외)·7, 또는 top이 와일드이고 무늬 지정이 없으면 아무 장.
        /// 아니면 requiredSuit(없으면 top 무늬) 또는 top 랭크. 7 이후는 지정 무늬 또는 7 또는 와일드.
        /// </summary>
        private static bool CanPlayNormal(MatchState state, CardDef card)
        {
            if (IsNormalTurnWild(card) || card.Rank == Rank.Seven)
            {
                return true;
            }

            var top = state.DiscardTop.Def;
            if (top.IsWild && state.RequiredSuit == null)
            {
                return !IsAttackResponseOnly(card);
            }

            var requiredSuit = state.RequiredSuit ?? top.Suit;
            if (card.Suit == requiredSuit && requiredSuit != Suit.None)
            {
                return true;
            }

            return card.Rank == top.Rank && card.Rank != Rank.None;
        }

        private static bool IsThreeOrFour(CardDef card)
        {
            return card.Rank == Rank.Three || card.Rank == Rank.Four;
        }

        private static bool IsAttackResponseOnly(CardDef card)
        {
            return card.Spec == SpecKind.Pass || card.Spec == SpecKind.Counter;
        }

        private static bool IsNormalTurnWild(CardDef card)
        {
            return card.IsWild && !IsAttackResponseOnly(card);
        }
    }
}
