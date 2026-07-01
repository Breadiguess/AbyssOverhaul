using Daybreak.Common.Features.Rarities;
using ReLogic.Graphics;

namespace AbyssOverhaul.Content.Rarities
{
	public class PrimordialYellow : ModRarity, IRarityTextRenderer
	{
		public override Color RarityColor => new Color(251, 251, 120);

		public static Color TextColor = new Color(251, 251, 120);
		public static Color BackColor = new Color(228, 217, 142);

		public void RenderText(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1, float spread = 2)
		{
			//Requirements
			DynamicSpriteFont rarityFont = FontAssets.MouseText.Value;
			Texture2D shineTexture = Assets.Textures.Extra.PrimordialTextBack.Asset.Value;
			float time = Main.GlobalTimeWrappedHourly;
			Vector2 totalSize = rarityFont.MeasureString(text);

			//To get when the text will "show interest"
			float pulseTime = 6; //Amount of time in seconds
			float halfTime = pulseTime / 2f;
			float sixSecond = time % pulseTime;

			//the pulsating of the "interest"
			float pulseAmount = (sixSecond <= halfTime) ? (float)-Math.Log(-(sixSecond / halfTime) + 1f) / halfTime : (pulseTime - sixSecond) / halfTime;
			pulseAmount = Math.Clamp(pulseAmount, 0f, 1f);

			//scale of the "interest"
			float interestScale = 1f;
			interestScale += MathHelper.Lerp(0.01f, 0.35f / Math.Clamp(text.Length / 4, 1, 100), pulseAmount);
			totalSize *= interestScale;

			//origin and position
			Vector2 textSizeOrig = totalSize / 2;
			Vector2 textPos = new Vector2(position.X + totalSize.X / 2f, position.Y + totalSize.Y / 2f);

			//Rotating text in the background
			Color fadedColor = Color.Lerp(Color.Gray, BackColor, 0.3f) * MathHelper.Lerp(0.18f, 0.3f, pulseAmount);
			fadedColor.A = 64;
			fadedColor = Color.Multiply(fadedColor, 2f);

			int backInstances = 3;
			float fourPi = MathHelper.TwoPi * 2f;
			float time2 = time * 2.1f;
			for (int i = 0; i < backInstances; i++)
			{
				Vector2 drawOffset = (fourPi * i / backInstances + time2).ToRotationVector2() * 5f;
				spriteBatch.DrawString(
					rarityFont, 
					text, 
					textPos + drawOffset, 
					fadedColor, 
					rotation,
					textSizeOrig, 
					scale * interestScale, 
					effects, 
					0
				);
			}

			//Shines on either side of the text
			float shineLerp = Math.Clamp((pulseAmount - 0.5f) * 2f, 0f, 1f);
			Color shineColor = Color.Lerp(BackColor, Color.White, shineLerp);
			shineColor = Color.Lerp(shineColor, Color.Black, 1f - shineLerp);
			shineColor.A = 0;

			Vector2 shineCenter = new Vector2(shineTexture.Width / 2, shineTexture.Height / 2);
			//shineCenter *= scale;
			Vector2 shineScale = new Vector2(0.08f, totalSize.Y / shineTexture.Height) * pulseAmount;
			shineScale *= scale;

			//left shine
			spriteBatch.Draw(shineTexture, position + new Vector2(0, 10 * scale.Y), null, shineColor, rotation, shineCenter, shineScale, 0, 1);
			//right shine
			spriteBatch.Draw(shineTexture, position + new Vector2(totalSize.X / interestScale * scale.X, 10 * scale.Y), null, shineColor, rotation, shineCenter, shineScale, SpriteEffects.FlipHorizontally, 1);

			//Main text
			Color textColor = Color.Multiply(TextColor, 0.2f);
			textColor.A = byte.MaxValue;
			Color outlineColor = Color.Lerp(TextColor, Color.White, pulseAmount);

			Utils.DrawBorderStringFourWay(
				spriteBatch,
				rarityFont,
				text,
				textPos.X,
				textPos.Y,
				textColor,
				outlineColor,
				textSizeOrig,
				interestScale
			);
		}
	}
}
