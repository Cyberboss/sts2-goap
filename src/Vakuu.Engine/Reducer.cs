using System;
using System.Collections.Generic;

namespace Vakuu.Engine
{
    public sealed class Reducer : IReducer
    {
        public delegate float ReduceVariables(IReadOnlyDictionary<string, float> currentVariables, float input);
        public delegate float Reduce(float input);

        readonly ReduceVariables invocation;
        readonly string? variable;
        readonly ActionVariableTag tag;

        public Reducer(Reduce invocation, string variable)
        {
            this.invocation = ConvertBasicReduce(invocation ?? throw new ArgumentNullException(nameof(invocation)));
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public Reducer(Reduce invocation, ActionVariableTag tag)
        {
            this.invocation = ConvertBasicReduce(invocation ?? throw new ArgumentNullException(nameof(invocation)));
            this.tag = tag;
        }

        public Reducer(ReduceVariables invocation, string variable)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public Reducer(ReduceVariables invocation, ActionVariableTag tag)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
            this.tag = tag;
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
