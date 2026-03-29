using System;
using System.Collections.Generic;

namespace Vakuu.Engine
{
    public interface IReducer
    {
        void Apply(Func<ActionVariableTag, IReadOnlyList<string>> taggedVariables, Dictionary<string, float> variables);
    }
}