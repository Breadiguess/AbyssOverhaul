using AbyssOverhaul.Content.NPCs.Bosses.Silva.Projectiles;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks
{
    internal sealed class WindingRootsAttack : SilvaAttack
    {

        public int EndTime = 120;
        public override SilvaBoss.State ID => SilvaBoss.State.WindingRoots;

        public override void Enter(SilvaBoss boss)
        {
            boss.LocalTimer = 0;
        }

        public override void Update(SilvaBoss boss)
        {
            NPC npc = boss.NPC;
            Player target = boss.Target;

            if (target is null)
            {
                npc.velocity *= 0.95f;
                return;
            }

            Vector2 hoverDestination = target.Center + new Vector2(250f * npc.direction, -100f);
            Vector2 desiredVelocity = (hoverDestination - npc.Center).SafeNormalize(Vector2.Zero) * 10f;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.06f);

            if (boss.LocalTimer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // spawn telegraph/projectile here
                // Projectile.NewProjectile(...);
            }

            if (boss.LocalTimer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Candidate start position to the side of the player, slightly above them.
                float sideOffset = 500f * -npc.direction;
                Vector2 candidateStart = target.Center + new Vector2(sideOffset, -140f);

                // Snap to a platform-like surface by searching downward for a solid tile.
                const int maxSearchTiles = 40;
                Vector2 start = candidateStart;
                int tileX = (int)(candidateStart.X / 16f);
                int tileY = (int)(candidateStart.Y / 16f);

                for (int i = 0; i < maxSearchTiles; i++)
                {
                    int checkY = tileY + i;
                    if (checkY < 0 || checkY >= Main.maxTilesY)
                        break;

                    Tile tile = Framing.GetTileSafely(tileX, checkY);
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        // place slightly above the tile to avoid clipping into the block
                        start.Y = checkY * 16f - 10f;
                        break;
                    }
                }

                // Horizontal velocity pointing toward the boss. Pure X velocity to create a linear path.
                float horizontalSpeed = 14f;
                float directionToBoss = Math.Sign(npc.Center.X - start.X);
                Vector2 projVelocity = new Vector2(directionToBoss * horizontalSpeed, 0f);

                int projType = ModContent.ProjectileType<WindingRootsProjectile>();
                int damage = 40;
                float knockBack = 0f;

                Projectile.NewProjectile(npc.GetSource_FromAI(), start, projVelocity, projType, damage, knockBack, Main.myPlayer);
            }

            if (boss.LocalTimer >= EndTime)
                Finish(boss);
        }
    }
}
