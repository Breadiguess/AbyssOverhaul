using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Systems;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.ModPlayers
{
    public class AbyssPlayer : ModPlayer
    {
        /// <summary>
        /// Final darkness strength used by the overlay shader.
        /// 0 = no darkness, 1 = full darkness.
        /// </summary>
        public float darknessIntensity;

        /// <summary>
        /// Modifier bucket that reduces effective darkness.
        /// Higher negative values mean brighter conditions.
        /// This mirrors Calamity's style of accumulating reductions.
        /// </summary>
        public float abyssDarkness;

        /// <summary>
        /// Multiplier/additive bucket for player-centered glow sources.
        /// </summary>
        public float abyssPlayerGlowMultiplier;

        /// <summary>
        /// Multiplier for beam/flashlight width style effects.
        /// </summary>
        public float abyssFlashlightWidthMultiplier;

        /// <summary>
        /// Convenience state flags that other systems can read.
        /// </summary>
        public bool inAbyss;
        public bool underwater;
        public bool hasMiningHelmetLight;

        /// <summary>
        /// Optional cached result after all modifiers are applied.
        /// Useful if other systems need the same computed number.
        /// </summary>
        public float computedBrightnessFactor;

        /// <summary>
        /// Base darkness before gear / buffs / pets adjust it.
        /// Set this from your abyss pressure/layer system.
        /// Suggested range: 0f to 1f.
        /// </summary>
        public float baseAbyssDarkness;


        public bool InAbyss;
        public bool InHorizontalBounds;
        public bool Underwater;
        public bool HasMiningHelmetLight;

        public float BaseDarkness;
        public float GlobalDepthInterpolant;
        public float LayerDepthInterpolant;

        public int AbyssTopY;
        public int AbyssBottomY;
        public int TileX;
        public int TileY;

        public AbyssLayer CurrentLayer;

        /// <summary>
        /// Cached region representing the global abyss rectangle.
        /// </summary>
        public AbyssRegion Region;



        public override void ResetEffects()
        {
            // These are recalculated every tick.
            abyssDarkness = 0f;
            abyssPlayerGlowMultiplier = 0f;
            abyssFlashlightWidthMultiplier = 1f;

            underwater = Player.IsUnderwater();
            hasMiningHelmetLight = Player.head == ArmorIDs.Head.MiningHelmet || Player.head == ArmorIDs.Head.UltraBrightHelmet;

            // These can be overwritten by biome / pressure systems later in the tick.
            inAbyss = false;
            baseAbyssDarkness = 0f;

            computedBrightnessFactor = 0f;
            darknessIntensity = 0f;
        }

        public override void UpdateDead()
        {
            abyssDarkness = 0f;
            abyssPlayerGlowMultiplier = 0f;
            abyssFlashlightWidthMultiplier = 1f;
            baseAbyssDarkness = 0f;
            computedBrightnessFactor = 0f;
            darknessIntensity = 0f;
            inAbyss = false;
        }

        public override void PostUpdateMiscEffects()
        {
            UpdateAbyssStateFromWorld();
            


            Player.SetAbyssLightLevels();
            RecalculateDarknessIntensity();
        }

        private void ClearAbyssState()
        {
            InAbyss = false;
            InHorizontalBounds = false;
            darknessIntensity = 0f;
            BaseDarkness = 0f;
            GlobalDepthInterpolant = 0f;
            LayerDepthInterpolant = 0f;
            AbyssTopY = 0;
            AbyssBottomY = 0;
            TileX = 0;
            TileY = 0;
            CurrentLayer = null;
            Region = default;
        }

        private void UpdateAbyssStateFromWorld()
        {
            TileX = (int)(Player.Center.X / 16f);
            TileY = (int)(Player.Center.Y / 16f);

            if (!AbyssGenUtils.Initialized || !PresssureSystem.HasAnyLayers())
            {
                ClearAbyssState();
                return;
            }

            Region = new AbyssRegion(
                AbyssGenUtils.MinX,
                AbyssGenUtils.MaxX,
                AbyssGenUtils.TopY,
                AbyssGenUtils.BottomY
            );

            InHorizontalBounds = TileX >= AbyssGenUtils.MinX && TileX <= AbyssGenUtils.MaxX;

            if (!PresssureSystem.TryGetAbyssInfo(Player, out AbyssInfo info))
            {
                InAbyss = false;
                CurrentLayer = null;
                BaseDarkness = 0f;
                GlobalDepthInterpolant = 0f;
                LayerDepthInterpolant = 0f;
                AbyssTopY = AbyssGenUtils.TopY;
                AbyssBottomY = AbyssGenUtils.BottomY;
                darknessIntensity = 0f;
                return;
            }

            InAbyss = true;
            CurrentLayer = info.Layer;
            AbyssTopY = info.AbyssTopY;
            AbyssBottomY = info.AbyssBottomY;
            GlobalDepthInterpolant = info.GlobalDepthInterpolant;
            LayerDepthInterpolant = info.LayerDepthInterpolant;

            BaseDarkness = CalculateBaseDarkness(info);
        }

        private float CalculateBaseDarkness(AbyssInfo info)
        {
            // You can tune this however you want.
            // Right now this gives:
            // - noticeable darkness at the top
            // - much stronger darkness deeper down
            // - a little extra increase as you descend inside the current layer
            float global = MathHelper.Lerp(0.0f, 1.4f, info.GlobalDepthInterpolant);
            float layerBonus = MathHelper.Lerp(0f, 0.08f, info.LayerDepthInterpolant);

            return  MathHelper.Clamp(global, 0f, 1f);
        }

        public void RecalculateDarknessIntensity()
        {
            if (!InAbyss)
            {
                darknessIntensity = 0f;
                return;
            }

            float finalDarkness = BaseDarkness;

            // abyssDarkness is your modifier bucket.
                finalDarkness += abyssDarkness;

            // Extra local glow softens the darkness a bit.
            finalDarkness -= abyssPlayerGlowMultiplier * 0.05f;

            if (Underwater)
                finalDarkness -= 0.0f;

            darknessIntensity = MathHelper.Clamp(finalDarkness, 0f, 1f);
        }
    }


    public static class AbyssPlayerExtensions
    {
        public static AbyssPlayer Abyss(this Player player) => player.GetModPlayer<AbyssPlayer>();

        public static void SetAbyssLightLevels(this Player player)
        {
            AbyssPlayer ap = player.Abyss();
            CalamityPlayer mp = player.Calamity();

            bool underwater = player.IsUnderwater();
            bool miningHelmet = player.head == ArmorIDs.Head.MiningHelmet || player.head == ArmorIDs.Head.UltraBrightHelmet;

            // Movement-based camper glow.
            if (mp.camper)
                ap.abyssPlayerGlowMultiplier += Utils.Remap(player.velocity.Length(), 0f, 5f, 0.2f, 0f);

            if (miningHelmet)
                ap.abyssPlayerGlowMultiplier += 0.2f;

            if (player.nightVision)
                ap.abyssDarkness -= 0.4f;

            if (mp.giantPearl)
                ap.abyssPlayerGlowMultiplier += 0.2f;

            if (mp.fathomSwarmerVisage)
            {
                ap.abyssDarkness -= 0.2f;
                ap.abyssPlayerGlowMultiplier += 0.2f;
                ap.abyssFlashlightWidthMultiplier += 0.5f;
            }

            if (mp.aquaticHeart)
                ap.abyssDarkness -= 0.1f;

            if (mp.jellyfishNecklace && underwater)
            {

                ap.abyssPlayerGlowMultiplier += 0.2f;
                ap.abyssFlashlightWidthMultiplier += 0.2f;
            }

            if (mp.WarbanneroftheRighteous)
            {
                ap.abyssPlayerGlowMultiplier += mp.warbannerDamageMult;
            }

            if (mp.reaverExplore)
            {
                ap.abyssDarkness -= 0.2f;
                ap.abyssPlayerGlowMultiplier += 0.2f;
                ap.abyssFlashlightWidthMultiplier += 0.5f;
            }

            if (mp.shine)
                ap.abyssPlayerGlowMultiplier += 0.2f;

            if (mp.babyGhostBell && underwater)
                ap.abyssDarkness -= 0.1f;

            if (mp.sirenPet && underwater)
                ap.abyssDarkness -= 0.2f;

            if (mp.littleLightPet)
                ap.abyssDarkness -= 0.1f;

        }
    }
}