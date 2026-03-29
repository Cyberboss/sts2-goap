using Vakuu.Engine.Cards.Ironclad;
using Vakuu.Engine.Enemies.Encounters;

namespace Vakuu.Engine.Tests.Battle.Ironclad.ActI
{
    [TestClass]
    public sealed class TestBasicNibbit : IroncladBattleAITest
    {
        [TestMethod]
        public void FirstEncounter()
        {
            new DefaultLogger();
            Setup(
                new Nibbit(),
                CreateCharacter(),
                new Health(80),
                Ascension.None,
                42);
            DrawRun(helper =>
            {
                helper.Draw<Strike>(3);
                helper.Draw<Defend>(amount: 2);
            });

            while (true)
            {
                var play = AI.GetBestMove();
                if (play == null)
                    break;
            }
        }
    }
}
