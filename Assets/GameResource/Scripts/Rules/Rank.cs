namespace Game.Rules
{
    /// <summary>
    /// 트럼프 랭크. CardDefId 접미: A 2..10 J Q K. 조커·무색은 <see cref="None"/>.
    /// </summary>
    public enum Rank
    {
        None = 0,
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
    }
}
