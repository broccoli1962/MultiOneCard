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
        /// 공격 응답·알약 색 락·일반 매칭을 구분한다.
        /// Q Give 응답(지급 고르기 전)은 같은 문양 3·4만. 지급 고르기 중은 낼 수 없다.
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
            return CanPlay(
                card,
                state.DiscardTop.Def,
                state.AttackStack,
                state.QueenStack,
                state.RequiredSuit,
                state.RequiredColor,
                state.SpearInStack,
                state.CounterUsedInChain,
                state.AttackDefendSuit,
                state.AttackDefendColor,
                state.Rules.JokerDefendable,
                state.AttackDefendRank,
                state.PendingGiveSeat.HasValue);
        }

        /// <summary>
        /// 공개 필드만으로 합법 힌트를 판정한다. 서버 accept 와 같은 함수다.
        /// 체인 역날검 사용 여부는 공개 뷰에 없으면 false 로 둔다.
        /// </summary>
        public static bool CanPlay(
            CardDef card,
            CardDef discardTop,
            int attackStack,
            int queenStack,
            Suit? requiredSuit,
            ColorGroup? requiredColor,
            bool spearInStack,
            bool counterUsedInChain,
            Suit? attackDefendSuit = null,
            ColorGroup? attackDefendColor = null,
            bool jokerDefendable = true,
            Rank? attackDefendRank = null,
            bool pendingGive = false)
        {
            if (card == null || discardTop == null)
            {
                return false;
            }

            if (attackStack > 0)
            {
                return CanPlayAttackResponse(
                    card,
                    discardTop,
                    requiredSuit,
                    requiredColor,
                    spearInStack,
                    counterUsedInChain,
                    attackDefendSuit,
                    attackDefendColor,
                    jokerDefendable,
                    attackDefendRank);
            }

            if (queenStack > 0)
            {
                return !pendingGive && CanPlayQueenResponse(card, discardTop);
            }

            if (requiredColor.HasValue)
            {
                return CanPlayColorLock(requiredColor.Value, card);
            }

            return CanPlayNormal(card, discardTop, requiredSuit);
        }

        /// <summary>
        /// 공격 응답: PASS, COUNTER(체인 1회), SPEAR, (공개 top이 죽창이 아니면) 3·4 방어, 또는 공격 카드(2/A/조커).
        /// 2·A 이어가기는 같은 랭크(색·문양 무관) 또는 같은 문양의 2·A. 조커·죽창 위 2·A는 테이블 색 일치.
        /// 조커 위 조커는 색 무관. 2·A 위 조커는 테이블 색 일치.
        /// 3·4는 공개 top이 죽창일 때만 불가. 아니면 AttackDefendSuit(또는 지정/top 문양) 일치, 조커 공격이면 JokerDefendable 시 같은 색.
        /// 패스·역날검 top이어도 AttackDefend* 가 남으면 방어·이어가기 가능.
        /// 공격 응답 무색은 죽창·패스·역날검뿐.
        /// </summary>
        private static bool CanPlayAttackResponse(
            CardDef card,
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor,
            bool spearInStack,
            bool counterUsedInChain,
            Suit? attackDefendSuit,
            ColorGroup? attackDefendColor,
            bool jokerDefendable,
            Rank? attackDefendRank)
        {
            if (card.Spec == SpecKind.Pass || card.Spec == SpecKind.Spear)
            {
                return true;
            }

            if (card.Spec == SpecKind.Counter)
            {
                return !counterUsedInChain;
            }

            if (IsThreeOrFour(card))
            {
                return discardTop.Spec != SpecKind.Spear
                    && MatchesDefend(
                        card,
                        discardTop,
                        requiredSuit,
                        attackDefendSuit,
                        attackDefendColor,
                        jokerDefendable);
            }

            if (card.IsJoker)
            {
                if (IsJokerAttack(discardTop, attackDefendColor))
                {
                    return true;
                }

                return JokerMatchesTableColor(card, discardTop, requiredSuit, requiredColor);
            }

            if (card.Rank != Rank.Two && card.Rank != Rank.Ace)
            {
                return false;
            }

            return MatchesAttackStack(
                card,
                discardTop,
                requiredSuit,
                requiredColor,
                attackDefendRank,
                attackDefendSuit);
        }

        /// <summary>Q Give 응답: 낸 Q와 같은 문양 3·4만.</summary>
        private static bool CanPlayQueenResponse(CardDef card, CardDef discardTop)
        {
            if (!IsThreeOrFour(card) || discardTop == null || discardTop.Suit == Suit.None)
            {
                return false;
            }

            return card.Suit == discardTop.Suit;
        }

        /// <summary>
        /// requiredColor 락: 그 색 문양, 같은 색 조커, 또는 알약(락 색만 변경). 다른 무색은 낼 수 없다.
        /// </summary>
        private static bool CanPlayColorLock(ColorGroup requiredColor, CardDef card)
        {
            if (card.IsJoker)
            {
                return card.Color == requiredColor;
            }

            if (card.Spec == SpecKind.Pill)
            {
                return true;
            }

            if (card.IsColorless)
            {
                return false;
            }

            return card.IsTrump && card.Color == requiredColor;
        }

        /// <summary>
        /// 일반 수: 무색 와일드(패스·역날검 제외), 또는 본인 색 조커.
        /// 7은 와일드가 아니며, 다른 랭크 위에서는 테이블 색과 같을 때만 무늬·랭크 매칭.
        /// 7 위 7은 색과 무관하게 랭크 매칭 허용.
        /// 버림 top이 색 있는 조커면 그 색 락(다른 색 문양 불가).
        /// top이 무색 와일드이고 무늬 지정이 없으면 아무 장.
        /// 아니면 requiredSuit(없으면 top 무늬) 또는 top 랭크.
        /// </summary>
        private static bool CanPlayNormal(CardDef card, CardDef discardTop, Suit? requiredSuit)
        {
            if (card.IsJoker)
            {
                return JokerMatchesTableColor(card, discardTop, requiredSuit, null);
            }

            if (discardTop.IsJoker && discardTop.Color != ColorGroup.None)
            {
                return CanPlayColorLock(discardTop.Color, card);
            }

            if (card.Rank == Rank.Seven
                && discardTop.Rank != Rank.Seven
                && !MatchesTableColor(card, discardTop, requiredSuit, null))
            {
                return false;
            }

            if (IsNormalTurnWild(card))
            {
                return true;
            }

            if (discardTop.IsWild && requiredSuit == null)
            {
                return !IsAttackResponseOnly(card);
            }

            var suit = requiredSuit ?? discardTop.Suit;
            if (card.Suit == suit && suit != Suit.None)
            {
                return true;
            }

            return card.Rank == discardTop.Rank && card.Rank != Rank.None;
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
            return card.IsWild && !card.IsJoker && !IsAttackResponseOnly(card);
        }

        /// <summary>
        /// 공격 체인이 조커로 열린 상태. 패스·역날검 top이어도 AttackDefendColor 가 남으면 해당.
        /// </summary>
        private static bool IsJokerAttack(CardDef discardTop, ColorGroup? attackDefendColor)
        {
            if (attackDefendColor.HasValue && attackDefendColor.Value != ColorGroup.None)
            {
                return true;
            }

            return discardTop != null && discardTop.IsJoker;
        }

        /// <summary>
        /// 흑·적·청 조커는 테이블 색(락 → 지정 무늬 → 버림 top)과 본인 색이 같을 때만.
        /// 버림이 무색이면 조커를 낼 수 없다.
        /// </summary>
        private static bool JokerMatchesTableColor(
            CardDef joker,
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor)
        {
            if (joker == null || !joker.IsJoker || joker.Color == ColorGroup.None)
            {
                return false;
            }

            return MatchesTableColor(joker, discardTop, requiredSuit, requiredColor);
        }

        /// <summary>
        /// 3·4 방어: AttackDefendSuit → requiredSuit → top 문양 순.
        /// 조커 공격(AttackDefendColor 또는 top 조커)은 JokerDefendable 이면 같은 색.
        /// </summary>
        private static bool MatchesDefend(
            CardDef card,
            CardDef discardTop,
            Suit? requiredSuit,
            Suit? attackDefendSuit,
            ColorGroup? attackDefendColor,
            bool jokerDefendable)
        {
            if (card == null || card.Suit == Suit.None)
            {
                return false;
            }

            var suit = attackDefendSuit
                ?? requiredSuit
                ?? (discardTop != null ? discardTop.Suit : Suit.None);
            if (suit != Suit.None && card.Suit == suit)
            {
                return true;
            }

            if (!jokerDefendable)
            {
                return false;
            }

            var color = attackDefendColor;
            if (!color.HasValue && discardTop != null && discardTop.IsJoker && discardTop.Color != ColorGroup.None)
            {
                color = discardTop.Color;
            }

            return color.HasValue
                && color.Value != ColorGroup.None
                && card.Color == color.Value;
        }

        /// <summary>
        /// 2·A 공격 이어가기: 같은 랭크, 또는 AttackDefendSuit(또는 top 문양)와 같은 문양의 2·A.
        /// 조커·죽창 등 랭크가 없으면 테이블 색 일치(무색이면 제한 없음).
        /// </summary>
        private static bool MatchesAttackStack(
            CardDef card,
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor,
            Rank? attackDefendRank,
            Suit? attackDefendSuit)
        {
            var stackRank = attackDefendRank
                ?? (discardTop.Rank == Rank.Two || discardTop.Rank == Rank.Ace
                    ? discardTop.Rank
                    : (Rank?)null);
            var stackSuit = attackDefendSuit
                ?? (discardTop.Rank == Rank.Two || discardTop.Rank == Rank.Ace
                    ? discardTop.Suit
                    : (Suit?)null);
            if (stackRank.HasValue && stackRank.Value != Rank.None && card.Rank == stackRank.Value)
            {
                return true;
            }

            if (stackSuit.HasValue && stackSuit.Value != Suit.None && card.Suit == stackSuit.Value)
            {
                return true;
            }

            if (stackRank.HasValue && stackRank.Value != Rank.None)
            {
                return false;
            }

            return MatchesAttackColor(card, discardTop, requiredSuit, requiredColor);
        }

        private static bool MatchesAttackColor(
            CardDef card,
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor)
        {
            var table = TableColor(discardTop, requiredSuit, requiredColor);
            return table == ColorGroup.None || card.Color == table;
        }

        private static bool MatchesTableColor(
            CardDef card,
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor)
        {
            if (card == null || card.Color == ColorGroup.None)
            {
                return false;
            }

            var table = TableColor(discardTop, requiredSuit, requiredColor);
            return table != ColorGroup.None && card.Color == table;
        }

        private static ColorGroup TableColor(
            CardDef discardTop,
            Suit? requiredSuit,
            ColorGroup? requiredColor)
        {
            if (requiredColor.HasValue)
            {
                return requiredColor.Value;
            }

            if (requiredSuit.HasValue)
            {
                return ColorGroups.Of(requiredSuit.Value);
            }

            return discardTop != null ? discardTop.Color : ColorGroup.None;
        }
    }
}
