namespace Vakuu.Engine.Tests.Battle
{
    public interface IDrawHelper
    {
        void Draw<TCard>(byte amount)
            where TCard : ICardArchetype;
    }
}
