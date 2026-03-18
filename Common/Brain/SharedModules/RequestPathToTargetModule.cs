using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Wayfarer.API;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class RequestPathToTargetModule : INpcModule<NpcContext>
    {
        public float Score = 30f;
        public float RepathDistanceThreshold = 24f;
        public string Name = "RequestPath";

        public NpcDirective Evaluate(NpcContext context)
        {
            if (!context.HasTargetPoint || context.PathAgent is null)
                return NpcDirective.None;

            NpcPathAgent agent = context.PathAgent;

            if (agent.RepathCooldown > 0)
                agent.RepathCooldown--;

            bool destinationChanged =
                !agent.HasDestination ||
                Vector2.Distance(agent.Destination, context.TargetPoint) > RepathDistanceThreshold;

            bool needsPath = agent.CurrentPath is null || destinationChanged;

            if (!needsPath)
                return NpcDirective.None;

            if (agent.WaitingForPath || agent.RepathCooldown > 0)
            {
                return new NpcDirective
                {
                    WantsControl = true,
                    Score = Score,
                    DesiredVelocity = Vector2.Zero,
                    DebugName = Name,
                    DebugInfo = "Waiting for path result"
                };
            }

            agent.Destination = context.TargetPoint;
            agent.HasDestination = true;
            agent.WaitingForPath = true;
            agent.DebugStatus = "RequestingPath";
            agent.RepathCooldown = 10;

            Point startTile = GetNavStartTile(context.Self);
            Point goalTile = agent.Destination.ToTileCoordinates();

            if (startTile == goalTile)
            {
                agent.CurrentPath = null;
                agent.WaitingForPath = false;
                agent.DebugStatus = $"Same support tile: {startTile}";
                return NpcDirective.None;
            }

            WayfarerAPI.RecalculateNavMesh(agent.Handle, startTile);
            WayfarerAPI.RecalculatePath(agent.Handle, [goalTile], result =>
            {
                agent.CurrentPath = result;
                agent.WaitingForPath = false;

                string msg;
                if (result is null)
                    msg = $"NULL path | start={startTile} | goal={goalTile}";
                else if (result.Current is null)
                    msg = $"EMPTY path | start={startTile} | goal={goalTile}";
                else
                    msg = $"READY {result.Current.GetType().Name} | start={startTile} | goal={goalTile}";

                Main.NewText(msg);
            });

            

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = Vector2.Zero,
                DebugName = Name,
                DebugInfo = $"Requesting path to {context.TargetPoint}"
            };
        }

        private static Point GetNavStartTile(NPC npc)
        {
            return new Point(
                npc.Center.ToTileCoordinates().X,
                (int)((npc.Bottom.Y + 2f) / 16f)
            );
        }
    }
}
