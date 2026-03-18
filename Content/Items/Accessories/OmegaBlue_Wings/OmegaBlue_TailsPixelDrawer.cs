using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings
{
    internal sealed class OmegaBlue_TailsPixelDrawer : IPlayerPixelatedDrawer
    {
        public PixelLayer PixelLayer => PixelLayer.AboveNPCs;

        public bool IsActive(Player player)
        {
            if (!player.active || player.dead)
                return false;

            OmegaBlue_Tails tailsPlayer = player.GetModPlayer<OmegaBlue_Tails>();
            return 
                   tailsPlayer.Tails is not null;
        }

        public void DrawPixelated(Player player, SpriteBatch spriteBatch)
        {
            if (!player.TryGetModPlayer(out OmegaBlue_Tails tailsPlayer))
                return;

            if (tailsPlayer.Tails is null)
                return;

            if (!tailsPlayer.Active)
                return;

            for (int t = 0; t < tailsPlayer.Tails.Length; t++)
            {
                var tail = tailsPlayer.Tails[t];
                if (tail?.Positions is null || tail.Positions.Length < 2)
                    continue;

                for (int i = 0; i < tail.Positions.Length - 1; i++)
                {
                    Utils.DrawLine(
                        spriteBatch,
                        tail.Positions[i],
                        tail.Positions[i + 1],
                        Color.White,
                        Color.White,
                        2f);
                }
            }
        }
    }
}