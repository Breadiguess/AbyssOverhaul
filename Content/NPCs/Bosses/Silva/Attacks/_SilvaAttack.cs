using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks
{
    internal abstract class SilvaAttack
    {
        public abstract SilvaBoss.State ID { get; }

        public virtual void Enter(SilvaBoss boss) { }

        public abstract void Update(SilvaBoss boss);

        public virtual void Exit(SilvaBoss boss) { }

        public virtual void Draw(SilvaBoss boss, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }

        protected void Finish(SilvaBoss boss) => boss.MoveToNextState();
    }

}
