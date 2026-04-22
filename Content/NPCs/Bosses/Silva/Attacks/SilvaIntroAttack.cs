using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks
{
    internal sealed class SilvaIntroAttack : SilvaAttack
    {
        public override SilvaBoss.State ID => SilvaBoss.State.Intro;

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

            Vector2 hoverDestination = target.Center + new Vector2(0f, -200f);
            Vector2 toDestination = hoverDestination - npc.Center;

            float speed = 14f;
            if (toDestination.Length() > speed)
                toDestination = toDestination.SafeNormalize(Vector2.UnitY) * speed;

            npc.velocity = Vector2.Lerp(npc.velocity, toDestination, 0.08f);

            if (boss.LocalTimer >= 90)
                Finish(boss);
        }
    }

    

   
}
