namespace Game.Rules
{
    /// <summary>
    /// Official 덱의 한 장. InstanceId 는 0..90.
    /// </summary>
    public readonly struct CardInstance
    {
        internal CardInstance(int instanceId, CardDef def)
        {
            InstanceId = instanceId;
            Def = def;
        }

        /// <summary>고정 인스턴스 번호 0..90.</summary>
        public int InstanceId { get; }

        /// <summary>이 장이 가리키는 고유 정의.</summary>
        public CardDef Def { get; }

        /// <summary>
        /// instanceId:defId 형식으로 반환한다.
        /// </summary>
        public override string ToString() => $"{InstanceId}:{Def.Id}";
    }
}
