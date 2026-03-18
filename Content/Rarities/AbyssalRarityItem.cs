using Luminance.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Rarities
{
    internal class AbyssalRarityItem : GlobalItem
    {
        public override bool InstancePerEntity => true;


        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.rare == ModContent.RarityType<AbyssalRarity>(); 
        }


        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName")
            {
                return true;
            }

            var text = item.AffixName();
            var position = new Vector2(line.X, line.Y);

            //SpawnDroplets(in position, text);
            //UpdateDroplets();
            //DrawDroplets();

            var font = FontAssets.MouseText.Value;
            var size = font.MeasureString(text);

            var offset = size / 2f;

            var center = position + offset;

            
            DrawText(in position, text);

            return false;
        }


        //todo:allow me to scale each letter without offsetting any other letters in the chain.
        private static void DrawText(in Vector2 position, string text)
        {
            var font = FontRegistry.BlackSide;
            var cursor = position;

           
            var batch = Main.spriteBatch;

            for (var i = 0; i < text.Length; i++)
            {
                var letter = text[i].ToString();

                var color = Color.Crimson * 0.5f;


                var offset = font.MeasureString(letter) / 1f;

                var empty = string.IsNullOrEmpty(letter) || string.IsNullOrWhiteSpace(letter);

              

                var wave = MathF.Cos(i/(float)(text.Length)+Main.GlobalTimeWrappedHourly);

                offset = new Vector2(0f, wave);

                color = Color.Lerp(Color.DeepSkyBlue, Color.Purple, 1 + MathF.Sin(Main.GlobalTimeWrappedHourly));
                color.A = 255;

                var scale = 1.2f + wave * 0.01f;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, default, Main.graphics.graphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                for (int x = 0; x < 10; x++)
                {
                    float thing =  x / 10f * MathHelper.TwoPi*2 + Main.GlobalTimeWrappedHourly*1f;
                    Utils.DrawBorderStringFourWay(batch, font, letter, cursor.X + (new Vector2(1).RotatedBy(thing)).X, cursor.Y+ (new Vector2(1).RotatedBy(thing)).Y, color, Color.Black, Vector2.zeroVector);

                    //Utils.DrawBorderString(Main.spriteBatch, letter, cursor + new Vector2(1).RotatedBy(thing), color, scale * 1.2f);
                }


                Main.spriteBatch.ResetToDefaultUI();
                Utils.DrawBorderStringFourWay(batch, font, letter, cursor.X, cursor.Y, Color.Black, Color.Transparent, Vector2.zeroVector);
                //Utils.DrawBorderString(Main.spriteBatch, letter, cursor + offset, Color.Black, scale);
                Utils.DrawLine(Main.spriteBatch, cursor, cursor + Vector2.UnitX * 140, Color.White);
                cursor.X += font.MeasureString(letter).X * scale;
            }
        }
    }
}
