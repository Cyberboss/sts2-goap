namespace Vakuu.Engine
{
    using System.Collections.Generic;
    using System.Linq;

    using MountainGoap;

    using Serilog;
    using Serilog.Core;

    public class DefaultLogger
    {
        public readonly Logger logger;

        public DefaultLogger()
        {
            var config = new LoggerConfiguration();
            System.IO.File.Delete("S:/workspace/code/cs/vakuu-engine/test.txt");
            config.WriteTo.Async(x => x.File("S:/workspace/code/cs/vakuu-engine/test.txt"));
            Agent.OnAgentActionSequenceCompleted += OnAgentActionSequenceCompleted;
            Agent.OnAgentStep += OnAgentStep;
            Agent.OnPlanningStarted += OnPlanningStarted;
            Agent.OnPlanningStartedForSingleGoal += OnPlanningStartedForSingleGoal;
            Agent.OnPlanningFinished += OnPlanningFinished;
            Agent.OnPlanningFinishedForSingleGoal += OnPlanningFinishedForSingleGoal;
            Agent.OnPlanUpdated += OnPlanUpdated;
            Agent.OnEvaluatedActionNode += OnEvaluatedActionNode;
            Action.OnBeginExecuteAction += OnBeginExecuteAction;
            Action.OnFinishExecuteAction += OnFinishExecuteAction;
            Sensor.OnSensorRun += OnSensorRun;
            logger = config.CreateLogger();
        }

        static ushort LastSeenDepth = 0;
        private void OnEvaluatedActionNode(ActionNode node)
        {
            if (node.StepsSoFar > LastSeenDepth)
            {
                LastSeenDepth = node.StepsSoFar;

                var cameFromList = new List<ActionNode>();
                var traceback = node;
                do
                {
                    if (traceback == null)
                        break;

                    cameFromList.Add(traceback);
                    traceback = traceback.CameFrom;
                }
                while (traceback != node);

                cameFromList.Reverse();

                if (cameFromList.Count != node.StepsSoFar)
                    throw new System.InvalidOperationException($"wtf {cameFromList.Count} {node.StepsSoFar}");

                logger.Information(
                    "Evaluating path ({n}):{new}\t- {node}{new1}Final State:{new2}\t- {states}",
                    cameFromList.Count,
                    System.Environment.NewLine,
                    string.Join($"{System.Environment.NewLine}\t- ", cameFromList.Select(cameFromNode => $" {cameFromNode.CostSoFar} - {cameFromNode.Action.Name}")),
                    System.Environment.NewLine,
                    string.Join($"{System.Environment.NewLine}\t- ", node.State.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key} - {kvp.Value}")));
            }
        }

        private void OnPlanUpdated(Agent agent, List<Action> actionList)
        {
            logger.Information("Agent {agent} has a new plan:", agent.Name);
            var count = 1;
            foreach (var action in actionList)
            {
                logger.Information("\tStep #{count}: {action}", count, action.Name);
                count++;
            }
        }

        private void OnAgentActionSequenceCompleted(Agent agent)
        {
            logger.Information("Agent {agent} completed action sequence.", agent.Name);
        }

        private void OnAgentStep(Agent agent)
        {
            logger.Information("Agent {agent} is working.", agent.Name);
        }

        private void OnBeginExecuteAction(Agent agent, Action action, Dictionary<string, object?> parameters)
        {
            logger.Information("Agent {agent} began executing action {action}.", agent.Name, action.Name);
            if (parameters.Count == 0) return;
            logger.Information("\tAction parameters:");
            foreach (var kvp in parameters) logger.Information("\t\t{key}: {value}", kvp.Key, kvp.Value);
        }

        private void OnFinishExecuteAction(Agent agent, Action action, ExecutionStatus status, Dictionary<string, object?> parameters)
        {
            logger.Information("Agent {agent} finished executing action {action} with status {status}.", agent.Name, action.Name, status);
        }

        private void OnPlanningFinished(Agent agent, BaseGoal? goal, float utility)
        {
            if (goal is null) logger.Warning("Agent {agent} finished planning and found no possible goal.", agent.Name);
            else logger.Information("Agent {agent} finished planning with goal {goal}, utility value {utility}.", agent.Name, goal.Name, utility);
        }

        private void OnPlanningStartedForSingleGoal(Agent agent, BaseGoal goal)
        {
            logger.Information("Agent {agent} started planning for goal {goal}.", agent.Name, goal.Name);
        }
        private void OnPlanningFinishedForSingleGoal(Agent agent, BaseGoal goal, float utility)
        {
            logger.Information("Agent {agent} finished planning for goal {goal}, utility value {utility}.", agent.Name, goal.Name, utility);
        }

        private void OnPlanningStarted(Agent agent)
        {
            logger.Information("Agent {agent} started planning.", agent.Name);
        }

        private void OnSensorRun(Agent agent, Sensor sensor)
        {
            logger.Information("Agent {agent} ran sensor {sensor}.", agent.Name, sensor.Name);
        }
    }
}
