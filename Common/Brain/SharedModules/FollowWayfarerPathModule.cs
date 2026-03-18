using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wayfarer.Edges;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class FollowWayfarerPathModule : INpcModule<NpcContext>
    {
        public float Score = 60f;
        public float MoveSpeed = 2.5f;
        public float NodeReachDistance = 12f;
        public string Name = "FollowPath";

        public NpcDirective Evaluate(NpcContext context)
        {
            NpcPathAgent agent = context.PathAgent;
            if (agent is null || agent.WaitingForPath||
  agent.CurrentPath is null)
        return NpcDirective.None;

            if (agent.CurrentPath.Current is not PathEdge edge)
            {
                agent.CurrentPath.Advance(out bool atGoal);

                if (atGoal)
                {
                    agent.CurrentPath = null;
                    return new NpcDirective
                    {
                        WantsControl = true,
                        Score = Score,
                        DesiredVelocity = Vector2.Zero,
                        DebugName = Name,
                        DebugInfo = "Reached destination"
                    };
                }

                if (agent.CurrentPath.Current is not PathEdge primedEdge)
                {
                    return new NpcDirective
                    {
                        WantsControl = true,
                        Score = Score,
                        DesiredVelocity = Vector2.Zero,
                        DebugName = Name,
                        DebugInfo = "Path could not be primed"
                    };
                }

                edge = primedEdge;
            }

            Vector2 edgeTarget = edge.To.ToWorldCoordinates(8f, 8f);
            float dist = Vector2.Distance(context.Self.Center, edgeTarget);

            if (dist <= NodeReachDistance)
            {
                agent.CurrentPath.Advance(out bool atGoal);

                if (atGoal)
                {
                    agent.CurrentPath = null;
                    return new NpcDirective
                    {
                        WantsControl = true,
                        Score = Score,
                        DesiredVelocity = Vector2.Zero,
                        DebugName = Name,
                        DebugInfo = "Reached destination"
                    };
                }

                if (agent.CurrentPath.Current is PathEdge nextEdge)
                    edge = nextEdge;
            }

            Vector2 desiredVelocity =
                (edge.To.ToWorldCoordinates(8f, 8f) - context.Self.Center).SafeNormalize(Vector2.Zero) * MoveSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = desiredVelocity,
                DebugName = Name,
                DebugInfo = $"{edge.GetType().Name} -> {edge.To}"
            };
        }
    }
}
