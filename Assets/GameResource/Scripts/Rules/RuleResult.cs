namespace Game.Rules
{
    /// <summary>
    /// RuleEngine 수 판정 결과. 거절 시 <see cref="Reject"/> 는 기획서 §6 코드다.
    /// </summary>
    public readonly struct RuleResult
    {
        private RuleResult(bool isAccepted, string reject)
        {
            IsAccepted = isAccepted;
            Reject = reject;
        }

        /// <summary>수가 받아들여졌는지.</summary>
        public bool IsAccepted { get; }

        /// <summary>거절 코드. 수락이면 null.</summary>
        public string Reject { get; }

        /// <summary>수락 결과를 만든다.</summary>
        public static RuleResult Accepted()
        {
            return new RuleResult(true, null);
        }

        /// <summary>기획서 §6 Reject 코드로 거절 결과를 만든다.</summary>
        public static RuleResult Rejected(string reject)
        {
            return new RuleResult(false, reject);
        }
    }
}
