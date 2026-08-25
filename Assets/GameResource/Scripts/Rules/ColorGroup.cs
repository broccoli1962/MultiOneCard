namespace Game.Rules
{
    /// <summary>
    /// 문양·알약·조커가 속하는 색 그룹. S·C=Black, H·D=Red, R·M=Blue.
    /// 별(R)과 달(M)은 둘 다 Blue 이며 실루엣으로 구분한다.
    /// </summary>
    public enum ColorGroup
    {
        None = 0,
        Black,
        Red,
        Blue,
    }

    /// <summary>
    /// 문양 → 색 그룹. 조커 합법 판정과 같은 매핑을 쓴다.
    /// </summary>
    public static class ColorGroups
    {
        /// <summary>
        /// 트럼프 문양의 색. None 은 무색.
        /// </summary>
        public static ColorGroup Of(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spade:
                case Suit.Club:
                    return ColorGroup.Black;
                case Suit.Heart:
                case Suit.Diamond:
                    return ColorGroup.Red;
                case Suit.Star:
                case Suit.Moon:
                    return ColorGroup.Blue;
                default:
                    return ColorGroup.None;
            }
        }
    }
}
