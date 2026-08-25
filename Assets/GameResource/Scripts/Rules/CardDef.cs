namespace Game.Rules
{
    /// <summary>
    /// 고정 카드 정의. Official 고유 89종. 런타임에 행을 추가하지 않는다.
    /// </summary>
    public sealed class CardDef
    {
        internal CardDef(
            string id,
            Suit suit,
            Rank rank,
            ColorGroup color,
            SpecKind spec,
            int score,
            int attackValue)
        {
            Id = id;
            Suit = suit;
            Rank = rank;
            Color = color;
            Spec = spec;
            Score = score;
            AttackValue = attackValue;
        }

        /// <summary>스펙 CardDefId. 예: SA, S10, JOKER:COLOR, SPEC:PASS.</summary>
        public string Id { get; }

        /// <summary>트럼프 문양. 조커·무색은 <see cref="Suit.None"/>.</summary>
        public Suit Suit { get; }

        /// <summary>트럼프 랭크. 조커·무색은 <see cref="Rank.None"/>.</summary>
        public Rank Rank { get; }

        /// <summary>색 그룹. 무색 특수(알약 제외)는 <see cref="ColorGroup.None"/>.</summary>
        public ColorGroup Color { get; }

        /// <summary>조커·무색 특수. 트럼프는 <see cref="SpecKind.None"/>.</summary>
        public SpecKind Spec { get; }

        /// <summary>동률·자동 선택용 점수.</summary>
        public int Score { get; }

        /// <summary>공식 공격값. 조커는 매치의 jokerAttack 이 덮어쓴다.</summary>
        public int AttackValue { get; }

        /// <summary>컬러·흑백·문 조커 여부.</summary>
        public bool IsJoker =>
            Spec == SpecKind.JokerColor
            || Spec == SpecKind.JokerBw
            || Spec == SpecKind.JokerMoon;

        /// <summary>죽창·패스·리버스조커·역날검·미러·알약 여부.</summary>
        public bool IsColorless =>
            Spec == SpecKind.Spear
            || Spec == SpecKind.Pass
            || Spec == SpecKind.ReverseJoker
            || Spec == SpecKind.Counter
            || Spec == SpecKind.Mirror
            || Spec == SpecKind.Pill;

        /// <summary>조커 또는 무색 특수.</summary>
        public bool IsWild => IsJoker || IsColorless;

        /// <summary>6문양 트럼프.</summary>
        public bool IsTrump => Spec == SpecKind.None;

        /// <summary>
        /// CardDefId 문자열을 반환한다.
        /// </summary>
        public override string ToString() => Id;
    }
}
