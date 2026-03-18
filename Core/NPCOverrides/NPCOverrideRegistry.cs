using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.NPCOverrides
{
    [Autoload(Side = ModSide.Both)]
    public sealed class NPCOverrideRegistry : ModSystem
    {
        private static Dictionary<int, NPCBehaviorOverride> _overrides;

        public static bool Loaded => _overrides is not null;

        public override void PostSetupContent()
        {
            BuildRegistry();
        }

        public override void Unload()
        {
            _overrides?.Clear();
            _overrides = null;
        }

        private static void BuildRegistry()
        {
            _overrides = new();

            Assembly asm = typeof(AbyssOverhaul).Assembly;
            Type baseType = typeof(NPCBehaviorOverride);

            IEnumerable<Type> overrideTypes = asm
                .GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    baseType.IsAssignableFrom(t));

            Mod mod = ModContent.GetInstance<AbyssOverhaul>();

            foreach (Type t in overrideTypes)
            {
                if (Activator.CreateInstance(t) is not NPCBehaviorOverride instance)
                    continue;

                int npcType = instance.NPCType;
                if (npcType <= 0)
                {
                    mod.Logger.Warn($"Skipped NPC override {t.FullName} because NPCType was {npcType}.");
                    continue;
                }

                if (_overrides.TryGetValue(npcType, out NPCBehaviorOverride existing))
                {
                    mod.Logger.Warn($"Duplicate NPC override for type {npcType}: {existing.GetType().FullName} replaced by {t.FullName}");
                }

                instance.Load();
                _overrides[npcType] = instance;
            }
        }

        public static NPCBehaviorOverride Get(NPC npc)
        {
            if (npc is null || _overrides is null)
                return null;

            if (!_overrides.TryGetValue(npc.type, out NPCBehaviorOverride ov))
                return null;

            return ov.ShouldOverride(npc) ? ov : null;
        }

        public static bool HasOverride(NPC npc) => Get(npc) is not null;
    }
}