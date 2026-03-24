using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Projectiles
{
    internal class AbyssLightingGlobalProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return ProjectileLightRegistry.Contains(entity.type);
        }
        public override void PostAI(Projectile projectile)
        {
            ProjectileLightRegistry.TryEmit(projectile);
        }
    }
}
