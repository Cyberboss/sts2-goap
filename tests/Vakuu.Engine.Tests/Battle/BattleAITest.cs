using System.Diagnostics.CodeAnalysis;

namespace Vakuu.Engine.Tests.Battle
{
    public abstract class BattleAITest : IDrawHelper
    {
        protected delegate void Draw<TCard>(byte amount);

        readonly List<ulong> draw = new List<ulong>();

        protected BattleAI? AI { get; private set; }

        protected abstract PlayerCharacter CreateCharacter();

        [TestCleanup]
        public void Cleanup()
            => AI = null;

        [MemberNotNull(nameof(AI))]
        protected void Setup(
            IEncounter encounter,
            PlayerCharacter character,
            Health health,
            Ascension ascension,
            params ushort[] enemyHPs)
        {
            AI = new BattleAI(
                enemyHPs,
                encounter,
                new Config(),
                character,
                health,
                ascension);
        }

        protected void DrawRun(Action<IDrawHelper> run)
        {
            run(this);
            Assert.IsNotNull(AI);
            AI.UpdateHand(draw);
        }

        void IDrawHelper.Draw<TCard>(byte amount)
        {
            Assert.IsNotNull(AI);
            var startCount = draw.Count;
            foreach (var card in AI.DeckCards)
            {
                if (card.Archetype.GetType() == typeof(TCard))
                {
                    draw.Add(card.ID);
                    if (draw.Count > startCount + amount)
                        break;
                }
            }
        }
    }
}
