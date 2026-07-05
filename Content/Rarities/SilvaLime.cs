using Daybreak.Common.Features.Rarities;
using ReLogic.Graphics;

namespace AbyssOverhaul.Content.Rarities
{
	public class SilvaLime : ModRarity, IRarityTextRenderer
	{
		public override Color RarityColor => textGreen;

		// Pulse times
		private const float pulseDuration = 4; // Amount of time in seconds
		private const float quarterTime = pulseDuration / 4f;
		private const float glowDuration = pulseDuration / 2f;
		private static float Time => Main.GlobalTimeWrappedHourly;
		private static float pulseTime => Time % pulseDuration;

		public static readonly Color textGreen = new Color(181, 243, 21);
		public static readonly Color textYellow = new Color(244, 254, 117);
		public static readonly Color flowerGreen = textGreen;
		public static readonly Color flowerYellow = new Color(200, 255, 50);

		public void RenderText(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1, float spread = 2)
		{
			DynamicSpriteFont rarityFont = FontAssets.MouseText.Value;
			var crystalTextGlow = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/UI/CrystalTextGlow").Value;

			float pulseAmount = (pulseTime <= pulseDuration / 2f) ? pulseTime / quarterTime : (pulseDuration - pulseTime) / quarterTime;
			pulseAmount -= 0.5f;
			pulseAmount = Math.Clamp(pulseAmount, 0f, 1f);

			Vector2 textSize = rarityFont.MeasureString(text);
			Color textColor = Color.Lerp(textGreen, textYellow, pulseAmount);

			Color bloomColor = Color.Multiply(textColor, 0.5f);
			bloomColor.A = 0;

			spriteBatch.Draw(crystalTextGlow, new Vector2(position.X + (textSize.X / 2f), position.Y + (textSize.Y / 2f) / 1.5f), null, bloomColor, rotation + MathHelper.PiOver2, new Vector2(6f, 33f),
			   new Vector2(1.6f, textSize.X / crystalTextGlow.Height * 1.2f), SpriteEffects.None, 0f);

			Color flowerColor = Color.Lerp(flowerGreen, flowerYellow, pulseAmount);
			flowerColor.A = 0;
			float flowerScale = 0.075f;

			DrawFlowers(spriteBatch, position, textSize, flowerColor, flowerScale, false);
			DrawText(spriteBatch, rarityFont, text, position, textSize, textColor);
			DrawFlowers(spriteBatch, position, textSize, flowerColor, flowerScale);
		}

		private static void DrawFlowers(SpriteBatch spriteBatch, Vector2 position, Vector2 textSize, Color flowerColor, float flowerScale, bool isFront = true)
		{
			var flower = Assets.Textures.Extra.SilvaRarity.Flower.Asset.Value;
			var flowerInner = Assets.Textures.Extra.SilvaRarity.inner.Asset.Value;

			int flowerCount = 4;
			Color innerWhite = isFront ? new Color(100, 100, 100, 0) : new Color(50, 50, 50, 0);

			for (int i = 0; i < flowerCount / (isFront ? 1 : 2); i++)
			{
				Color colorToUse = Color.Multiply(flowerColor, isFront ? 0.85f : 0.5f);

				float flowerOffY = textSize.Y - (isFront ? 16f : 26f);
				float flowerOffX = isFront ? -8f : 4f;

				if (isFront)
				{
					if (i >= flowerCount / 2)
						flowerOffX += textSize.X - (flowerOffX * 2f);
					if (i % 2 == 1)
					{
						flowerOffY += 8f;
						flowerOffX += (i >= flowerCount / 2) ? -8 : 8;
					}
				}
				else
				{
					if (i % 2 == 1)
						flowerOffX += textSize.X - (flowerOffX * 2f);
				}

				float flowerTime = pulseTime + (i * (isFront ? 0.5f : 0.75f));
				float flowerRotation = (float)(flowerTime % 4 < 2 ? (flowerTime % 2 < 1 ? -(Math.Cos(Math.PI * flowerTime) - 1) / 2 : 1f) : (flowerTime % 2 < 1 ? -(Math.Cos(Math.PI * (flowerTime + 3f)) - 1) / 2 : 0f));
				Vector2 flowerPos = position + new Vector2(flowerOffX, flowerOffY);

				spriteBatch.Draw(flower, flowerPos, null, colorToUse, flowerRotation, new Vector2(flower.Width / 2, flower.Height / 2), flowerScale, 0, 1f);
				spriteBatch.Draw(flowerInner, flowerPos, null, innerWhite, flowerRotation, new Vector2(flowerInner.Width / 2, flowerInner.Height / 2), flowerScale, 0, 1f);
			}
		}

		private static void DrawText(SpriteBatch spriteBatch, DynamicSpriteFont rarityFont, string text, Vector2 position, Vector2 textSize, Color textColor)
		{
			Vector2 textOrigin = textSize / 2f;
			Vector2 textPos = position + textOrigin;

			Color glowBaseColor = new Color(120, 120, 120, 0);
			float glowTime = Time % glowDuration;

			for (int i = 0; i < 2; i++)
			{
				float textXOff = glowTime % 2 * 5f;
				if (i == 1)
					textXOff = -textXOff;

				Color glowColor = Color.Multiply(glowBaseColor, 1f - Math.Clamp((glowTime / (glowDuration / 2f)) - 0.75f, 0f, 1f));

				spriteBatch.DrawString(
					rarityFont,
					text,
					new Vector2(textPos.X + textXOff, textPos.Y),
					glowColor,
					0f,
					textOrigin,
					1f,
					0,
					1f
				);
			}

			Utils.DrawBorderStringFourWay(
				spriteBatch,
				rarityFont,
				text,
				textPos.X,
				textPos.Y,
				Color.Black,
				textColor,
				textOrigin,
				1f
			);
		}

		#region Unused vine texture Idea that ended up not working out
		private void Lion8cake_DrawVines(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, float rotation, Vector2 textSize, Color textColor)
		{
			var flower = Assets.Textures.Extra.SilvaRarity.SilvaFlower.Asset.Value;
			var leaf = Assets.Textures.Extra.SilvaRarity.SilvaFlower.Asset.Value;
			var vineTexture = Assets.Textures.Extra.SilvaRarity.SilvaVine.Asset.Value;
			var vineTextureBack = Assets.Textures.Extra.SilvaRarity.SilvaVineBack.Asset.Value;

			int vineExtraWidth = 6;
			int vineExtraHeight = 4;
			Color vineColor = new Color(255, 255, 255, 0);
			float vineScale = 0.1f;

			float vineSpriteWidth = vineTexture.Width * vineScale;
			float totalTextWidth = textSize.X + (vineExtraWidth * 2);
			Vector2 vineStartPos = position - new Vector2(vineExtraWidth, 0);

			int vineAmount = (int)(totalTextWidth / vineSpriteWidth) + 1;
			int vineProgressTemp = (int)MathHelper.Lerp(0, totalTextWidth, pulseTime / quarterTime); //temp for testing purposes

			for (int i = 0; i < (vineAmount / 2); i++)
			{
				bool endTexture = (i + 1) * 2 == vineAmount;

				int testWidth = (int)Math.Clamp((vineProgressTemp / vineScale) - (vineTextureBack.Width * ((i * 2) + 1)), 0, vineTextureBack.Width);

				int textureWidth = (int)(totalTextWidth - (vineTextureBack.Width * (((i + 1) * 2) - 1)));

				Rectangle endFraming = new Rectangle(0, 0, (int)(Math.Clamp(testWidth, 0, totalTextWidth - (vineSpriteWidth * i * 2))), vineTexture.Height);

				spriteBatch.Draw(vineTextureBack, vineStartPos + new Vector2(vineSpriteWidth * ((i * 2) + 1), -vineExtraHeight),
					endTexture ? endFraming : new Rectangle(0, 0, testWidth, vineTexture.Height), 
					vineColor, rotation, Vector2.Zero,
					vineScale, SpriteEffects.None, 0f);
			}

			DrawText(spriteBatch, font, text, position, textSize, textColor);

			for (int i = 0; i < ((vineAmount - 1) / 2) + 1; i++)
			{
				bool endTexture = ((i + 1) * 2) - 1 == vineAmount;

				int testWidth = (int)Math.Clamp((vineProgressTemp / vineScale) - vineTexture.Width * i * 2, 0, vineTexture.Width);

				Rectangle endFraming = new Rectangle(0, 0, (int)(Math.Clamp(testWidth, 0, totalTextWidth - (vineSpriteWidth * i * 2))), vineTexture.Height);

				spriteBatch.Draw(vineTexture, vineStartPos + new Vector2(vineSpriteWidth * i * 2, -vineExtraHeight),
					endTexture ? endFraming : new Rectangle(0, 0, testWidth, vineTexture.Height),
					vineColor, rotation, Vector2.Zero, 
					vineScale, SpriteEffects.None, 0f);
			}
		}
		#endregion
	}
}
