using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.NPCOverrides
{

    [Autoload(Side = ModSide.Both)]
    public sealed class NPCOverrideRegistry : ModSystem
    {
        public static Dictionary<int, NPCBehaviorOverride> _overrides;
        private static Dictionary<int, Asset<Texture2D>> _originalTextures;

        public static bool Loaded => _overrides is not null;

        public override void PostSetupContent()
        {
            BuildRegistry();

        }

        public override void Unload()
        {
            RestoreTextures();

            _overrides?.Clear();
            _overrides = null;

            _originalTextures?.Clear();
            _originalTextures = null;
        }

        private static void BuildRegistry()
        {
            _overrides = new();
            _originalTextures = new();

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
                instance.SetStaticDefaults();
                if (!string.IsNullOrEmpty(instance.TexturePath))
                    instance.ReplacementTexture = ModContent.Request<Texture2D>(instance.TexturePath);
                _overrides[npcType] = instance;

                if (instance.ReplacementTexture is not null)
                {
                    if (!_originalTextures.ContainsKey(npcType))
                        _originalTextures[npcType] = TextureAssets.Npc[npcType];

                    TextureAssets.Npc[npcType] = instance.ReplacementTexture;
                }
            }
        }

        private static void RestoreTextures()
        {
            if (_originalTextures is null)
                return;

            foreach (var pair in _originalTextures)
            {
                int npcType = pair.Key;
                TextureAssets.Npc[npcType] = pair.Value;
            }
        }

        public static NPCBehaviorOverride GetPrototype(NPC npc)
        {
            if (npc is null || _overrides is null)
                return null;

            if (!_overrides.TryGetValue(npc.type, out NPCBehaviorOverride ov))
                return null;

            return ov.ShouldOverride(npc) ? ov : null;
        }

        public static NPCBehaviorOverride CreateFor(NPC npc)
        {
            NPCBehaviorOverride prototype = GetPrototype(npc);
            if (prototype is null)
                return null;

            if (Activator.CreateInstance(prototype.GetType()) is not NPCBehaviorOverride instance)
                return null;

            return instance;
        }

        public static bool HasOverride(NPC npc) => GetPrototype(npc) is not null;
    }
}