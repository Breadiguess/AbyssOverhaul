using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Graphics
{
    public class AbyssLayerSceneEffect : ModSceneEffect
    {
        public override ModWaterStyle WaterStyle
        {
            get
            {
                if (!PresssureSystem.TryGetAbyssInfo(Main.LocalPlayer, out var info) || info.Layer is null)
                    return base.WaterStyle;

                return info.Layer.ModWaterStyle;
            }
        }
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            return PresssureSystem.TryGetAbyssInfo(player, out _);
        }

        public override int Music
        {
            get
            {
                if (!PresssureSystem.TryGetAbyssInfo(Main.LocalPlayer, out var info) || info.Layer is null)
                    return -1;

                return info.Layer.MusicSlot;
            }
        }

        public override void MapBackgroundColor(ref Color color)
        {
            if (!PresssureSystem.TryGetAbyssInfo(Main.LocalPlayer, out var info) || info.Layer is null)
                return;

            color = info.Layer.MapBackgroundColor;
        }

        public override float GetWeight(Player player)
        {
            if (!PresssureSystem.TryGetAbyssInfo(player, out var info))
                return 0f;

            // Gives deeper/more centered presence a bit more authority if anything competes.
            return MathHelper.Lerp(0.6f, 1f, info.LayerDepthInterpolant);
        }

     
    }
}