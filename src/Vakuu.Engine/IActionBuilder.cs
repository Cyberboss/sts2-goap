using System;
using System.Collections.Generic;

namespace Vakuu.Engine
{
    public interface IActionBuilder
    {
        void AddStaticPostCondition(string stateTarget, object result);
        void AddVariable<T>(string stateTarget, float initialValue, Func<float, T> resultConversion, IReadOnlySet<ActionVariableTag>? tags = null);
        void Reduce(IReducer reducer);
        void RepeatTaggedReducers(ActionVariableTag actionVariableTag, float multiplier);
        public void ApplyCombatBuffers(bool skipEnemyBlock);
    }
}