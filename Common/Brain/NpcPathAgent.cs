using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wayfarer.API;
using Wayfarer.Pathfinding;

namespace AbyssOverhaul.Common.Brain
{
    public sealed class NpcPathAgent
    {
        public WayfarerHandle Handle;
        public PathResult CurrentPath;
        public bool WaitingForPath;
        public Vector2 Destination;
        public bool HasDestination;

        public int RepathCooldown;

        public string DebugStatus { get; internal set; }
    }
}
