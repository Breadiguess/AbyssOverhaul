using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace AbyssOverhaul.Common.Brain.Contexts
{
    public class NpcContext
    {
        public NPC Self;

        public Player ClosestPlayer;
        public Vector2 HomePosition;

        public bool HasTargetPoint;
        public Vector2 TargetPoint;

        public bool HasFoundTile;
        public Point FoundTile;
        public Vector2 FoundTileWorld;


        public NpcPathAgent PathAgent;

        public virtual void Update(NPC npc)
        {
            Self = npc;
            ClosestPlayer = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
        }

       
    }
}