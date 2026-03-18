using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wayfarer.API;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class WayfarerNavMeshSensor : INpcSensor<NpcContext>
    {
        public void Update(NpcContext context)
        {
            NpcPathAgent agent = context.PathAgent;
            if (agent is null)
                return;

            Point[] feetTiles = GetTilesBelowFeet(context.Self);
            if (feetTiles.Length == 0)
                return;

            bool contains = false;
            foreach (Point tile in feetTiles)
            {
                if (WayfarerAPI.PointIsInNavMesh(agent.Handle, tile))
                {
                    contains = true;
                    break;
                }
            }

            if (!contains && !agent.WaitingForPath)
            {
                Point startTile = GetNavStartTile(context.Self);
                WayfarerAPI.RecalculateNavMesh(agent.Handle, startTile);
                agent.CurrentPath = null;
                agent.DebugStatus = $"NavMeshRebuilt from {startTile}";
            }
        }

        private static Point GetNavStartTile(NPC npc)
        {
            return new Point(
                npc.Center.ToTileCoordinates().X,
                (int)((npc.Bottom.Y + 2f) / 16f)
            );
        }

        private static Point[] GetTilesBelowFeet(NPC npc)
        {
            int left = npc.Left.ToTileCoordinates().X;
            int right = npc.Right.ToTileCoordinates().X;
            int y = (int)((npc.Bottom.Y + 2f) / 16f);

            List<Point> points = new();
            for (int x = left; x <= right; x++)
                points.Add(new Point(x, y));

            return points.ToArray();
        }
    }
}
