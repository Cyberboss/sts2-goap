using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vakuu.Engine
{
#if DEBUG
    [DebuggerDisplay("{Name,nq}")]
#endif
    public sealed class Reducer : IReducer
    {
#if DEBUG
        public string? Name { get; }
#endif

        public delegate float ReduceVariables(IReadOnlyDictionary<string, float> currentVariables, float input);
        public delegate float Reduce(float input);

        readonly ReduceVariables invocation;
        readonly string? variable;
        readonly ActionVariableTag tag;

        public Reducer(Reduce invocation, string variable, string? name = null)
        {
            this.invocation = ConvertBasicReduce(invocation ?? throw new ArgumentNullException(nameof(invocation)));
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
#if DEBUG
            Name = name;
#endif
        }

        public Reducer(Reduce invocation, ActionVariableTag tag, string? name = null)
        {
            this.invocation = ConvertBasicReduce(invocation ?? throw new ArgumentNullException(nameof(invocation)));
            this.tag = tag;
#if DEBUG
            Name = name;
#endif
        }

        public Reducer(ReduceVariables invocation, string variable, string? name = null)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
#if DEBUG
            Name = name;
#endif
        }

        public Reducer(ReduceVariables invocation, ActionVariableTag tag, string? name = null)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
            this.tag = tag;
#if DEBUG
            Name = name;
#endif
        }

        public void Apply(Func<ActionVariableTag, IReadOnlyList<string>> taggedVariables, Dictionary<string, float> variables)
        {
            if (variable != null)
                ApplyToVariable(variables, variable);
            else
                foreach (var variable in taggedVariables(tag))
                    ApplyToVariable(variables, variable);
        }

        static ReduceVariables ConvertBasicReduce(Reduce invocation)
            => (_, input) => invocation(input);

        void ApplyToVariable(Dictionary<string, float> variables, string variable)
        {
            if (variables.TryGetValue(variable, out float input))
                variables[variable] = invocation(variables, input);
        }
    }
}
