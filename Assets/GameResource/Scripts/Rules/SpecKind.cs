namespace Game.Rules
{
    /// <summary>
    /// 조커·무색 특수 종류. 트럼프 랭크 효과는 <see cref="Rank"/> 로 구분한다.
    /// Official: JOKER:COLOR/BW/MOON, SPEC:SPEAR/PASS/REVJOKER/COUNTER/MIRROR/PILL_*.
    /// </summary>
    public enum SpecKind
    {
        None = 0,
        JokerColor,
        JokerBw,
        JokerMoon,
        Spear,
        Pass,
        ReverseJoker,
        Counter,
        Mirror,
        Pill,
    }
}
