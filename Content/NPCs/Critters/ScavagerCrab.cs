using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using Luminance.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Wayfarer.API;
using Wayfarer.Data;


namespace AbyssOverhaul.Content.NPCs.Critters
{
    internal class ScavagerCrab : ModNPC
    {
        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;
        public ModularNpcBrain<NpcContext> Brain;
        public NpcPathAgent PathAgent;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.CountsAsCritter[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
        }
        public override void SetDefaults()
        {
            NPC.Size = new Vector2(35);
            NPC.lifeMax = 40;
            NPC.defense = 400;
            
        }
        public override void OnSpawn(IEntitySource source)
        {
            Brain = new ModularNpcBrain<NpcContext>(new NpcContext());
            PathAgent = new NpcPathAgent();

            Brain.Context.HomePosition = NPC.Center;
            Brain.Context.PathAgent = PathAgent;

            Point navCenter = GetSupportTileUnderNpc();

            NavMeshParameters navMeshParameters = new(
                navCenter,
                60,
                WayfarerPresets.DefaultIsTileValid
            );

            NavigatorParameters navigatorParameters = new(
                NPC.Hitbox,
                WayfarerPresets.DefaultJumpFunction,
                new Point(2, 2),
                () => NPC.gravity,
                SelectDestinationTile
            );

            if (!WayfarerAPI.TryCreatePathfindingInstance(navMeshParameters, navigatorParameters, out PathAgent.Handle))
            {
                // Optional fallback if instance allocation somehow fails.
            }

            
            Brain.Modules.Add(new RequestPathToTargetModule());
            Brain.Modules.Add(new FollowWayfarerPathModule());
            Brain.Sensors.Add(new WayfarerNavMeshSensor());
        }
        private Point SelectDestinationTile(IReadOnlySet<Point> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return NPC.Center.ToTileCoordinates();

            Point desired = PathAgent.HasDestination
                ? PathAgent.Destination.ToTileCoordinates()
                : Brain.Context.TargetPoint.ToTileCoordinates();

            Point best = desired;
            float bestDistSq = float.MaxValue;

            foreach (Point p in candidates)
            {
                float distSq = Vector2.DistanceSquared(p.ToVector2(), desired.ToVector2());
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = p;
                }
            }

            Dust.NewDustPerfect(best.ToWorldCoordinates(), DustID.Cloud, Vector2.Zero, newColor: Color.Yellow);

            return best;
        }
        private Point GetSupportTileUnderNpc()
        {
            return new Point(
                (int)(NPC.Center.X / 16f),
                (int)((NPC.Bottom.Y + 2f) / 16f)
            );
        }
        public override void AI()
        {
            if (Main.LocalPlayer.controlUseItem)
            {

                Brain.Context.HasTargetPoint = true;
                Brain.Context.TargetPoint = Main.MouseWorld;
            }
            Brain.Update(NPC);

            

            if (PathAgent.HasDestination)
            {
                //Dust a = Dust.NewDustPerfect(PathAgent.Destination, DustID.Cloud, Vector2.Zero);
            }
            NPC.spriteDirection = NPC.velocity.X >= 0f ? 1 : -1;
           
        }


        private static Point GetNavGroundTile(NPC npc)
        {
            return new Point(
                npc.Center.ToTileCoordinates().X,
                (int)((npc.Bottom.Y + 2f) / 16f)
            );
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {


            if (Brain is null) return false;


            string msg = "";
            msg += $"NPC tile loc: {NPC.Center.ToTileCoordinates()}";
            msg += PathAgent.DebugStatus + $"\n";
            msg += Brain.CurrentDebugInfo + $"\n";
            msg += Brain.Context.PathAgent.DebugStatus+$"\n";
            foreach(var a in Brain.Modules)
            {
                msg += a.ToString() + $"\n";
            }
            
            Utils.DrawBorderString(spriteBatch, msg, NPC.Center- screenPos, drawColor);

            WayfarerAPI.DebugRenderNavMesh(PathAgent.Handle, spriteBatch);

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
    }
}
