namespace Game.Rules
{
    /// <summary>
    /// Q 모드. Reverse=방향 반전 후 다음 활성, Give=다음 활성에게 누적 Q 수만큼 지급.
    /// </summary>
    public enum QueenMode
    {
        Reverse = 0,
        Give,
    }
}
