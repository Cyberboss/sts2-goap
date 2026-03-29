namespace Vakuu.Engine.Tests.Battle.Ironclad
{
    public abstract class IroncladBattleAITest : BattleAITest
    {
        protected override PlayerCharacter CreateCharacter()
            => new PlayerCharacter(Character.Ironclad);
    }
}
