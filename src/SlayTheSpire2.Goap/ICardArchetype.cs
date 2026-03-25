using System;
using System.Collections.Generic;
using System.Linq;

using MountainGoap;

namespace SlayTheSpire2.Goap
{
    public interface ICardArchetype
    {
        string Name { get; }

        CardType Type { get; }
        CardPool Pool { get; }

        bool EnemyTargeted { get; }

        IEnumerable<CardModifier> Modifiers(bool upgraded) => Enumerable.Empty<CardModifier>();
        void ApplyPreConditions(Action<string, object> preCondition, Action<string, ComparisonValuePair> comparativePreCondition);
        ushort Damage(bool upgraded) => 0;

        IEnumerable<IStatus> TargetStatuses(bool upgraded) => Enumerable.Empty<IStatus>();
        IEnumerable<IStatus> InvokerStatuses(bool upgraded) => Enumerable.Empty<IStatus>();
    }
}