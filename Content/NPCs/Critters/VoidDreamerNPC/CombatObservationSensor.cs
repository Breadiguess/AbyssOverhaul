using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using Luminance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC
{
    public sealed class CombatObservationSensor : INpcSensor<ObservationNpcContext>
    {
        public float SearchRadius { get; set; } = 500f;

        public void Update(ObservationNpcContext context)
        {
            NPC self = context.Self;
            Vector2 center = self.Center;

            Player player = context.ClosestPlayer;
            if (player is not null && player.active && !player.dead)
            {
                float dist = Vector2.Distance(center, player.Center);
                if (dist <= SearchRadius)
                {

                    if (!Collision.CanHit(self, player))
                        return;
                    context.SawPlayer = true;


                    if (player.itemAnimation > 0 || player.ItemAnimationActive)
                        if(player.HeldItem.damage>0)
                        context.SawWeaponSwing = true;

                    if (dist < context.ObservedDistance)
                    {
                        context.ObservedObject = player;
                        context.HasObservedThing = true;
                        context.ObservedDistance = dist;
                        context.ObservedPosition = player.Center;
                        context.ObservedPlayer = player;
                    }
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.friendly)
                    continue;

                float dist = Vector2.Distance(center, proj.Center);
                if (dist > SearchRadius)
                    continue;

                if (!Collision.CanHit(self, proj))
                    continue;
                context.SawHostileProjectile = true;

                if (dist < context.ObservedDistance)
                {
                    context.ObservedObject = proj;
                    context.HasObservedThing = true;
                    context.ObservedDistance = dist;
                    context.ObservedPosition = proj.Center;
                    context.ObservedProjectile = proj;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.whoAmI == self.whoAmI)
                    continue;
                if (npc.type == ModContent.NPCType<VoidDreamer>())
                    continue;
                float dist = Vector2.Distance(center, npc.Center);
                if (dist > SearchRadius)
                    continue;

                if (!Collision.CanHit(self, npc))
                    return;
                context.SawNearbyNpc = true;


                if (dist < context.ObservedDistance)
                {
                    context.ObservedObject = npc;
                    context.HasObservedThing = true;
                    context.ObservedDistance = dist;
                    context.ObservedPosition = npc.Center;
                    context.ObservedNpc = npc;
                }
            }
        }
    }
}
