using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using MountainGoap;

namespace Vakuu.Engine
{
    sealed class ActionBuilder : IActionBuilder
    {
        readonly System.Action executor;

        readonly Dictionary<string, object?> preconditions;
        readonly Dictionary<string, ComparisonValuePair> arithmeticPreconditions;
        readonly Dictionary<string, object?> postconditions;
        readonly Dictionary<string, (float? InitialValue, Func<float, object?>? PostConversion)> variables;
        readonly List<IReducer> reducers;
        readonly List<Func<IReadOnlyDictionary<string, object?>, bool>> dynamicPreconditions;
        readonly List<string>[] taggedVariables;

        float? cost;

        public ActionBuilder(IEnumerable<Enemy> enemies, PlayerCharacter character, System.Action executor, string name, float? cost)
        {
            this.executor = executor;
            this.cost = cost;
            Name = name;

            preconditions = new Dictionary<string, object?>();
            arithmeticPreconditions = new Dictionary<string, ComparisonValuePair>();
            postconditions = new Dictionary<string, object?>();
            dynamicPreconditions = new List<Func<IReadOnlyDictionary<string, object?>, bool>>();
            variables = new Dictionary<string, (float?, Func<float, object?>?)>();
            reducers = new List<IReducer>();
            var totalTags = ((IEnumerable<ActionVariableTag>)Enum.GetValues(typeof(ActionVariableTag))).Cast<int>().Max() + 1;
            taggedVariables = new List<string>[totalTags];
            for (var i = 0; i < totalTags; ++i)
                taggedVariables[i] = new List<string>();

            AddDefaultVariables(enemies, character);
        }

        public string Name { get; }

        public void AddStaticPostCondition(string stateTarget, object result)
        {
            if (variables.ContainsKey(stateTarget))
                throw new InvalidOperationException($"Variable already present for \"{stateTarget}\" in action builder \"{Name}\"!");

            postconditions.Add(stateTarget, result);
        }

        public void AddStaticPrecondition(string stateSource, object? value)
            => preconditions.Add(stateSource, value);

        public void AddPrecondition(string stateSource, ComparisonValuePair comparator)
            => arithmeticPreconditions.Add(stateSource, comparator);

        public void AddDynamicPrecondition(Func<IReadOnlyDictionary<string, object?>, bool> stateChecker)
            => dynamicPreconditions.Add(stateChecker);

        public void AddVariable<T>(string stateTarget, float initialValue, Func<float, T>? stateReduction, IReadOnlySet<ActionVariableTag>? tags = null)
            => InsertAndTagVariable(tags, (initialValue, ConvertResultConversion(stateReduction)), stateTarget);

        public void AddVariableFromState<T>(string stateSourceAndTarget, Func<float, T>? stateReduction, IReadOnlySet<ActionVariableTag>? tags = null)
            => InsertAndTagVariable(tags, (null, ConvertResultConversion(stateReduction)), stateSourceAndTarget);

        public void Reduce(IReducer reducer)
            => reducers.Add(reducer);

        public MountainGoap.Action Build()
        {
            if (!cost.HasValue)
                throw new InvalidOperationException($"ActionBuilder \"{Name}\" does not have a cost set on Build!");

            void Mutator(MountainGoap.Action _, ConcurrentDictionary<string, object?> currentState)
            {
                Dictionary<string, float> workingVariables = new Dictionary<string, float>(variables.Count);
                foreach (var kvp in variables)
                {
                    float workingState;
                    if (kvp.Value.InitialValue.HasValue)
                        workingState = kvp.Value.InitialValue.Value;
                    else
                    {
                        if (!currentState.TryGetValue(kvp.Key, out var stateObject))
                            throw new InvalidOperationException($"Variable registered for non-existent state \"{kvp.Key}\" in action builder \"{Name}\"!");

                        workingState = Convert.ToSingle(stateObject);
                    }

                    workingVariables.Add(kvp.Key, workingState);
                }

                foreach (var reducer in reducers)
                    reducer.Apply(GetVariablesNameFromActionTag, workingVariables);

                foreach (var kvp in variables)
                {
                    var postConversion = kvp.Value.PostConversion;
                    if (postConversion != null)
                        currentState[kvp.Key] = postConversion(workingVariables[kvp.Key]);
                }
            }

            bool StateChecker(MountainGoap.Action _, ConcurrentDictionary<string, object?> currentState)
            {
                foreach (var dynamicPrecondition in dynamicPreconditions)
                    if (!dynamicPrecondition(currentState))
                        return false;

                return true;
            }

            return new MountainGoap.Action(
                Name,
                null,
                (_, _) =>
                {
                    executor();
                    return ExecutionStatus.Succeeded;
                },
                cost.Value,
                null,
                preconditions,
                comparativePreconditions: arithmeticPreconditions,
                postconditions: postconditions,
                stateMutator: Mutator,
                stateChecker: StateChecker);
        }

        public void SetCost(float cost)
        {
            if (this.cost.HasValue)
                throw new InvalidOperationException("Cost already set!");

            this.cost = cost;
        }

        static Func<float, object?>? ConvertResultConversion<T>(Func<float, T>? source)
            => source != null
                ? (result => source(result))
                : null;

        void InsertAndTagVariable(IReadOnlySet<ActionVariableTag>? tags, (float? initialValue, Func<float, object?>?) value, string stateTarget)
        {
            if (postconditions.ContainsKey(stateTarget))
                throw new InvalidOperationException($"Boolean postcondition already present for \"{stateTarget}\" in action builder \"{Name}\""!);

            variables.Add(stateTarget, value);

            if (tags == null)
                return;

            foreach (var tag in tags)
                taggedVariables[(int)tag].Add(stateTarget);
        }

        IReadOnlyList<string> GetVariablesNameFromActionTag(ActionVariableTag tag)
            => taggedVariables[(int)tag];

        void AddDefaultVariables(IEnumerable<Enemy> enemies, PlayerCharacter character)
        {
            IEnumerable<Combatant> Combatants()
            {
                yield return character;
                foreach (var enemy in enemies)
                    yield return enemy;
            }

            foreach (var combatant in Combatants())
            {
                AddVariableFromState(combatant.HealthState, result => (ushort)result);
                AddVariableFromState(combatant.MaxHealthState, result => (ushort)result);
                AddVariable<ushort>(combatant.BlockGainVariable, 0.0f, null);
            }

            foreach (var enemy in enemies)
            {
                AddVariableFromState(enemy.AttackCountState, result => (ushort)Math.Floor(result));
                AddVariableFromState(enemy.AttackAmountVariable, result => (ushort)Math.Floor(result));
            }

            StatusRepository.Apply(status =>
            {
                AddVariableFromState(character.StatusState(status), result => (int)Math.Floor(result));
                foreach (var enemy in enemies)
                    AddVariableFromState(enemy.StatusState(status), result => (int)Math.Floor(result));
            });
        }

        public void RepeatTaggedReducers(ActionVariableTag actionVariableTag, float multiplier) => throw new NotImplementedException();
    }
}
