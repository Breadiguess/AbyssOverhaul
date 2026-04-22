using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks
{
    internal sealed class PetalFusiladeAttack : SilvaAttack
    {
        public override SilvaBoss.State ID => SilvaBoss.State.PetalFusillade;

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

            Vector2 hoverDestination = target.Center + new Vector2(0f, -300f);
            Vector2 desiredVelocity = (hoverDestination - npc.Center).SafeNormalize(Vector2.Zero) * 8f;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.05f);

            if (boss.LocalTimer % 12 == 0 && boss.LocalTimer >= 30 && boss.LocalTimer <= 90 &&
                Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = npc.DirectionTo(target.Center) * 14f;
                // Projectile.NewProjectile(...);
            }

            if (boss.LocalTimer >= 120)
                Finish(boss);
        }
    }
}
