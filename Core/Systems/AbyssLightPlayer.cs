using AbyssOverhaul.Core.Graphics;
using AbyssOverhaul.Core.ModPlayers;
using CalamityMod;
using CalamityMod.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Core.Systems
{
    internal class AbyssLightPlayer : ModSystem
    {
        public override void PostUpdatePlayers()
        {
            if(Main.netMode != NetmodeID.Server)
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    var abyssPlayer = player.Abyss();

                    Vector2 mouseWorld = player.Calamity().mouseWorld + player.position - player.oldPosition;
                    Vector2 toMouse = mouseWorld - player.Center;

                    // Prevent NaNs if mouse is exactly on player.
                    Vector2 aimDirection = toMouse.SafeNormalize(Vector2.UnitY);

                    // Distance-based zoom factor.
                    // Tune these two values to control where "close" and "far" begin/end.
                    float minDistance = 80f;
                    float maxDistance = 400f;

                    float mouseDistance = toMouse.Length();
                    float zoomFactor = Utils.GetLerpValue(minDistance, maxDistance, mouseDistance, true);
                    // zoomFactor:
                    // 0 = mouse very close
                    // 1 = mouse far away

                    // Big circular player glow
                    // Bigger when mouse is close, smaller when far.
                    float bigLightScale = MathHelper.Lerp(12.25f, 3.25f, zoomFactor) * abyssPlayer.abyssPlayerGlowMultiplier;

                  
                    var tex = ModContent.Request<Texture2D>("AbyssOverhaul/Assets/Textures/Glow_2").Value;
                    ReworkedAbyssLighting.lights.Add(
                        new()
                        {
                            center = player.Center,
                            rotation = player.DirectionTo(mouseWorld).ToRotation(),
                            vectorScale = new Vector2(4 * player.Abyss().abyssFlashlightWidthMultiplier * (zoomFactor) * bigLightScale, 1f* player.Abyss().abyssFlashlightWidthMultiplier * (zoomFactor)),
                            Origin = new Vector2(0, tex.Height/2),
                            texture = tex,
                        });

                    float beamOpacity = MathHelper.Lerp(0.35f, 1f, zoomFactor);
                    float beamWidth = 1.2f + 1.1f * zoomFactor;
                    float beamLength = MathHelper.Lerp(450f, 750f, zoomFactor);
                    zoomFactor = Math.Clamp(zoomFactor, 0, 0.8f);
                    ReworkedAbyssLighting.lights.Add(new(center: player.Center, scale: 20 *  (1-zoomFactor)*  player.Abyss().abyssPlayerGlowMultiplier));

                }
            }
        }
    }
}
