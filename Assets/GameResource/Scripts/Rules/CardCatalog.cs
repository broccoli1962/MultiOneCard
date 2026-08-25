using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Rules
{
    /// <summary>
    /// Official 91장 덱 카탈로그. 고유 def 89종, 패스 3장.
    /// 트럼프 6문양 78 + 조커 3 + 무색 특수 10(패스 3장 포함).
    /// This assembly does not reference UnityEngine.
    /// </summary>
    public sealed class CardCatalog : IReadOnlyList<CardInstance>
    {
        public const int OfficialInstanceCount = 91;
        public const int OfficialDefCount = 89;
        public const int OfficialTrumpCount = 78;
        public const int OfficialJokerCount = 3;
        public const int OfficialPassCount = 3;

        public const string IdJokerColor = "JOKER:COLOR";
        public const string IdJokerBw = "JOKER:BW";
        public const string IdJokerMoon = "JOKER:MOON";
        public const string IdSpear = "SPEC:SPEAR";
        public const string IdPass = "SPEC:PASS";
        public const string IdReverseJoker = "SPEC:REVJOKER";
        public const string IdCounter = "SPEC:COUNTER";
        public const string IdMirror = "SPEC:MIRROR";
        public const string IdPillBlack = "SPEC:PILL_BK";
        public const string IdPillRed = "SPEC:PILL_RD";
        public const string IdPillBlue = "SPEC:PILL_BL";

        public const int AttackTwo = 2;
        public const int AttackAce = 3;
        public const int AttackJokerColor = 10;
        public const int AttackJokerBw = 5;
        public const int AttackJokerMoon = 15;
        public const int AttackSpear = 5;

        public const int ScoreAce = 15;
        public const int ScoreFaceSpecial = 20;
        public const int ScoreColorBw = 30;
        public const int ScorePassPill = 25;
        public const int ScoreMoonRevMirror = 40;
        public const int ScoreCounter = 45;
        public const int ScoreSpear = 50;

        private static readonly Suit[] TrumpSuits =
        {
            Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club, Suit.Star, Suit.Moon,
        };

        private static readonly Rank[] TrumpRanks =
        {
            Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven,
            Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King,
        };

        private static readonly CardCatalog Official = CreateOfficial();

        private readonly CardInstance[] _instances;
        private readonly CardDef[] _defs;
        private readonly Dictionary<string, CardDef> _defsById;

        private CardCatalog(CardInstance[] instances, CardDef[] defs)
        {
            _instances = instances;
            _defs = defs;
            _defsById = new Dictionary<string, CardDef>(defs.Length);
            for (var i = 0; i < defs.Length; i++)
            {
                _defsById.Add(defs[i].Id, defs[i]);
            }
        }

        /// <summary>
        /// 트럼프 78 + 조커 3 + 무색 특수(패스 3장 포함) 91장을 반환한다.
        /// </summary>
        public static CardCatalog BuildOfficial() => Official;

        public CardInstance this[int index] => _instances[index];

        public int Count => _instances.Length;

        public IReadOnlyList<CardInstance> Instances => _instances;

        public IReadOnlyList<CardDef> Defs => _defs;

        /// <summary>
        /// Official 패스 장수(항상 3)를 센다.
        /// </summary>
        public int CountPass() => CountBySpec(SpecKind.Pass);

        /// <summary>
        /// spec 에 해당하는 인스턴스 수를 센다.
        /// </summary>
        public int CountBySpec(SpecKind spec)
        {
            var count = 0;
            for (var i = 0; i < _instances.Length; i++)
            {
                if (_instances[i].Def.Spec == spec)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// defId로 카드 정의를 조회한다.
        /// </summary>
        public CardDef GetDef(string defId)
        {
            if (!_defsById.TryGetValue(defId, out var def))
            {
                throw new KeyNotFoundException($"Unknown card def '{defId}'.");
            }

            return def;
        }

        /// <summary>
        /// defId로 카드 정의를 조회한다.
        /// </summary>
        public bool TryGetDef(string defId, out CardDef def)
        {
            return _defsById.TryGetValue(defId, out def);
        }

        /// <summary>
        /// instanceId(0..90)로 카드 인스턴스를 조회한다.
        /// </summary>
        public CardInstance GetInstance(int instanceId)
        {
            if (instanceId < 0 || instanceId >= _instances.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceId));
            }

            return _instances[instanceId];
        }

        /// <summary>
        /// instanceId(0..90)로 카드 인스턴스를 조회한다.
        /// </summary>
        public bool TryGetInstance(int instanceId, out CardInstance instance)
        {
            if (instanceId < 0 || instanceId >= _instances.Length)
            {
                instance = default;
                return false;
            }

            instance = _instances[instanceId];
            return true;
        }

        /// <summary>
        /// 트럼프 문양의 색 그룹을 반환한다. S·C=Black, H·D=Red, R·M=Blue.
        /// </summary>
        public static ColorGroup ColorOf(Suit suit)
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

        /// <summary>
        /// 트럼프 랭크의 Official 점수를 반환한다. 5~10=숫자, J=10, A=15, 2/3/4/7/Q/K=20.
        /// </summary>
        public static int ScoreOf(Rank rank)
        {
            switch (rank)
            {
                case Rank.Five:
                    return 5;
                case Rank.Six:
                    return 6;
                case Rank.Eight:
                    return 8;
                case Rank.Nine:
                    return 9;
                case Rank.Ten:
                case Rank.Jack:
                    return 10;
                case Rank.Ace:
                    return ScoreAce;
                case Rank.Two:
                case Rank.Three:
                case Rank.Four:
                case Rank.Seven:
                case Rank.Queen:
                case Rank.King:
                    return ScoreFaceSpecial;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "No official score for rank.");
            }
        }

        /// <summary>
        /// 카탈로그 인스턴스를 순회한다.
        /// </summary>
        public IEnumerator<CardInstance> GetEnumerator()
        {
            return ((IEnumerable<CardInstance>)_instances).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _instances.GetEnumerator();
        }

        private static CardCatalog CreateOfficial()
        {
            var defs = new List<CardDef>(OfficialDefCount);
            var instances = new List<CardInstance>(OfficialInstanceCount);
            var nextId = 0;

            for (var s = 0; s < TrumpSuits.Length; s++)
            {
                for (var r = 0; r < TrumpRanks.Length; r++)
                {
                    AddUnique(defs, instances, ref nextId, CreateTrump(TrumpSuits[s], TrumpRanks[r]));
                }
            }

            AddUnique(defs, instances, ref nextId, CreateSpecial(IdJokerColor, ColorGroup.Red, SpecKind.JokerColor, ScoreColorBw, AttackJokerColor));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdJokerBw, ColorGroup.Black, SpecKind.JokerBw, ScoreColorBw, AttackJokerBw));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdJokerMoon, ColorGroup.Blue, SpecKind.JokerMoon, ScoreMoonRevMirror, AttackJokerMoon));

            AddUnique(defs, instances, ref nextId, CreateSpecial(IdSpear, ColorGroup.None, SpecKind.Spear, ScoreSpear, AttackSpear));

            var pass = CreateSpecial(IdPass, ColorGroup.None, SpecKind.Pass, ScorePassPill, 0);
            defs.Add(pass);
            for (var i = 0; i < OfficialPassCount; i++)
            {
                instances.Add(new CardInstance(nextId, pass));
                nextId++;
            }

            AddUnique(defs, instances, ref nextId, CreateSpecial(IdReverseJoker, ColorGroup.None, SpecKind.ReverseJoker, ScoreMoonRevMirror, 0));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdCounter, ColorGroup.None, SpecKind.Counter, ScoreCounter, 0));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdMirror, ColorGroup.None, SpecKind.Mirror, ScoreMoonRevMirror, 0));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdPillBlack, ColorGroup.Black, SpecKind.Pill, ScorePassPill, 0));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdPillRed, ColorGroup.Red, SpecKind.Pill, ScorePassPill, 0));
            AddUnique(defs, instances, ref nextId, CreateSpecial(IdPillBlue, ColorGroup.Blue, SpecKind.Pill, ScorePassPill, 0));

            var trumpCount = 0;
            var jokerCount = 0;
            var passCount = 0;
            for (var i = 0; i < instances.Count; i++)
            {
                var def = instances[i].Def;
                if (def.IsTrump)
                {
                    trumpCount++;
                }

                if (def.IsJoker)
                {
                    jokerCount++;
                }

                if (def.Spec == SpecKind.Pass)
                {
                    passCount++;
                }
            }

            if (instances.Count != OfficialInstanceCount
                || defs.Count != OfficialDefCount
                || trumpCount != OfficialTrumpCount
                || jokerCount != OfficialJokerCount
                || passCount != OfficialPassCount
                || nextId != OfficialInstanceCount)
            {
                throw new InvalidOperationException(
                    $"Official catalog invariant failed. instances={instances.Count}, defs={defs.Count}, trump={trumpCount}, joker={jokerCount}, pass={passCount}.");
            }

            AssertOfficialScores(instances);
            return new CardCatalog(instances.ToArray(), defs.ToArray());
        }

        private static void AssertOfficialScores(List<CardInstance> instances)
        {
            for (var i = 0; i < instances.Count; i++)
            {
                var def = instances[i].Def;
                var expected = ExpectedOfficialScore(def);
                if (def.Score != expected)
                {
                    throw new InvalidOperationException(
                        $"Official score mismatch for '{def.Id}': {def.Score} != {expected}.");
                }
            }
        }

        private static int ExpectedOfficialScore(CardDef def)
        {
            if (def.IsTrump)
            {
                return ScoreOf(def.Rank);
            }

            switch (def.Spec)
            {
                case SpecKind.JokerColor:
                case SpecKind.JokerBw:
                    return ScoreColorBw;
                case SpecKind.JokerMoon:
                case SpecKind.ReverseJoker:
                case SpecKind.Mirror:
                    return ScoreMoonRevMirror;
                case SpecKind.Pass:
                case SpecKind.Pill:
                    return ScorePassPill;
                case SpecKind.Counter:
                    return ScoreCounter;
                case SpecKind.Spear:
                    return ScoreSpear;
                default:
                    throw new InvalidOperationException($"No official score for '{def.Id}'.");
            }
        }

        private static void AddUnique(
            List<CardDef> defs,
            List<CardInstance> instances,
            ref int nextId,
            CardDef def)
        {
            defs.Add(def);
            instances.Add(new CardInstance(nextId, def));
            nextId++;
        }

        private static CardDef CreateTrump(Suit suit, Rank rank)
        {
            return new CardDef(
                ToTrumpDefId(suit, rank),
                suit,
                rank,
                ColorOf(suit),
                SpecKind.None,
                ScoreOf(rank),
                AttackOf(rank));
        }

        private static CardDef CreateSpecial(
            string id,
            ColorGroup color,
            SpecKind spec,
            int score,
            int attackValue)
        {
            return new CardDef(id, Suit.None, Rank.None, color, spec, score, attackValue);
        }

        private static int AttackOf(Rank rank)
        {
            switch (rank)
            {
                case Rank.Two:
                    return AttackTwo;
                case Rank.Ace:
                    return AttackAce;
                default:
                    return 0;
            }
        }

        private static string ToTrumpDefId(Suit suit, Rank rank)
        {
            return SuitCode(suit) + RankCode(rank);
        }

        private static string SuitCode(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spade:
                    return "S";
                case Suit.Heart:
                    return "H";
                case Suit.Diamond:
                    return "D";
                case Suit.Club:
                    return "C";
                case Suit.Star:
                    return "R";
                case Suit.Moon:
                    return "M";
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit), suit, "Not a trump suit.");
            }
        }

        private static string RankCode(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ace:
                    return "A";
                case Rank.Two:
                    return "2";
                case Rank.Three:
                    return "3";
                case Rank.Four:
                    return "4";
                case Rank.Five:
                    return "5";
                case Rank.Six:
                    return "6";
                case Rank.Seven:
                    return "7";
                case Rank.Eight:
                    return "8";
                case Rank.Nine:
                    return "9";
                case Rank.Ten:
                    return "10";
                case Rank.Jack:
                    return "J";
                case Rank.Queen:
                    return "Q";
                case Rank.King:
                    return "K";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, "Not a trump rank.");
            }
        }
    }
}
