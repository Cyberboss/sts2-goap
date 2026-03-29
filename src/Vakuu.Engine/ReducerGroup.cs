using System;
using System.Collections.Generic;

namespace Vakuu.Engine
{
    sealed class ReducerGroup : IReducer
    {
        readonly List<IReducer> reducers = new List<IReducer>();

        public void Add(IReducer reducer)
        {
            reducers.Add(reducer);
        }

        public void Apply(Func<ActionVariableTag, IReadOnlyList<string>> taggedVariables, Dictionary<string, float> variables)
        {
            foreach (var reducer in reducers)
                reducer.Apply(taggedVariables, variables);
        }
    }
}
