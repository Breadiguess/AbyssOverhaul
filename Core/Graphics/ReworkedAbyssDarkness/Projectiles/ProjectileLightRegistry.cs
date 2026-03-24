using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Projectiles
{
    internal static class ProjectileLightRegistry
    {
        public delegate bool ProjectileLightEmitter(Projectile projectile);

        private static readonly Dictionary<int, ProjectileLightEmitter> _emitters = new();

        public static bool Contains(int ProjectileType)
        {
            return _emitters.ContainsKey(ProjectileType);
        }
        public static void Register(int ProjectileType, ProjectileLightEmitter emitter)
        {
            _emitters[ProjectileType] = emitter;
        }

        public static bool TryEmit(Projectile projectile)
        {
            if (_emitters.TryGetValue(projectile.type, out var emitter))
                return emitter(projectile);

            return false;
        }

        public static void Clear()
        {
            _emitters.Clear();
        }

    
    }
}
