using Vakuu.Engine.Cards.Ironclad;
using Vakuu.Engine.Enemies.Encounters;

namespace Vakuu.Engine.Tests.Battle.Ironclad.ActI
{
    [TestClass]
    public sealed class TestBasicNibbit : IroncladBattleAITest
    {
        [TestMethod]
        public async Task FirstEncounter()
        {
            var logger = new DefaultLogger();
            Setup(
                new Nibbit(),
                CreateCharacter(),
                new Health(80),
                Ascension.None,
                42);
            DrawRun(helper =>
            {
                helper.Draw<Strike>(3);
                helper.Draw<Defend>(2);
            });

            while (true)
            {
                var play = AI.GetBestMove();
                if (play == null)
                    break;
            }

            await logger.logger.DisposeAsync();
        }
    }
}
