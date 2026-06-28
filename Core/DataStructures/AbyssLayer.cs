using AbyssOverhaul.Core.WorldGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.WorldBuilding;

namespace AbyssOverhaul.Core.DataStructures
{
    public abstract class AbyssLayer : ModType
    {
        public virtual string MusicPath => null;

        private int? cachedMusicSlot;

        public int MusicSlot
        {
            get
            {
                if (MusicPath is null)
                    return -1;

                cachedMusicSlot ??= MusicLoader.GetMusicSlot(Mod, MusicPath);
                return cachedMusicSlot.Value;
            }
        }

        public virtual ModWaterStyle ModWaterStyle=> null;
        public virtual SceneEffectPriority ScenePriority => SceneEffectPriority.BiomeHigh;

        // Key used with Player.ManageSpecialBiomeVisuals(...)
        public virtual string VisualKey => "AbyssOverhaul:GenericAbyss";


        public virtual Color MapBackgroundColor => Color.Black;
        public virtual Color LightTint => Color.White;

        public virtual void OnSceneActive(Player player, ref AbyssSceneContext context) { }

        internal Dictionary<string, Action<AbyssLayer, GenerationProgress, GameConfiguration>> Tasks = new();

        protected void AddGenTask(string name, Action<AbyssLayer, GenerationProgress, GameConfiguration> task)
        {
            Tasks[name] = task;
        }

        protected sealed override void Register()
        {
            AbyssLayerRegistry.Register(this);
        }

        public abstract int StartHeight { get; }
        public abstract int EndHeight { get; }

        public int StartY => StartHeight;
        public int EndY => EndHeight;

        public bool ContainsY(int yTile) => yTile >= StartY && yTile <= EndY;

        public virtual Dictionary<int, float> NPCSpawnPool => new();

        public virtual void ModifySpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            foreach (var npc in NPCSpawnPool)
                pool[npc.Key] = npc.Value;
        }

        public virtual bool AppliesToSpawn(NPCSpawnInfo spawnInfo)
        {
            return ContainsY(spawnInfo.SpawnTileY);
        }

        public virtual void ModifyGenTasks() { }
    }

    public struct AbyssSceneContext
    {
        public float GlobalDepthInterpolant;
        public float LayerDepthInterpolant;
        public float Darkness;
        public Color MapColor;
    }
}
