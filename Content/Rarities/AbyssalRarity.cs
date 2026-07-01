using Daybreak.Common.Features.Rarities;
using ReLogic.Graphics;

namespace AbyssOverhaul.Content.Rarities
{
    internal class AbyssalRarity : ModRarity, IRarityTextRenderer
    {
        public override Color RarityColor => Color.DeepSkyBlue; //seen in some rare cases

		private static readonly Color[] PulseColors =
		[
			Color.DeepSkyBlue,
			Color.Purple,
		];

		public void RenderText(SpriteBatch spritebatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1, float spread = 2)
		{
			bool uniqueFont = ModContent.GetInstance<AbyssOverhaulClientConfig>().UniqueRarityFont;
			var abyssalFont = uniqueFont ? FontRegistry.BlackSide : FontAssets.DeathText.Value;

			float baseScale = uniqueFont ? 1.25f : 0.5f;
			const float cycleDuration = 3f;
			const float transitionWidth = 78f;

			float time = Main.GlobalTimeWrappedHourly;

			float cycleRaw = time / cycleDuration;
			int cycleIndex = (int)MathF.Floor(cycleRaw);
			float cycleProgress = cycleRaw - cycleIndex;

			Color oldColor = PulseColors[cycleIndex % PulseColors.Length];
			Color newColor = PulseColors[(cycleIndex + 1) % PulseColors.Length];

			float totalWidth = abyssalFont.MeasureString(text).X * baseScale;
			float centerX = position.X + totalWidth * 0.5f;
			float maxDistance = MathF.Max(totalWidth * 0.5f, 1f);

			float rawProgress = cycleProgress;

			float easedProgress = rawProgress * rawProgress * rawProgress *
								  (rawProgress * (rawProgress * 6f - 15f) + 10f);

			float startDistance = -transitionWidth;
			float endDistance = maxDistance + transitionWidth;

			float frontDistance = MathHelper.Lerp(startDistance, endDistance, easedProgress);

			var cursor = position;

			spritebatch.End();
			spritebatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				SamplerState.PointClamp,
				default,
				Main.graphics.GraphicsDevice.RasterizerState,
				null,
				Main.UIScaleMatrix
			);

			for (int i = 0; i < text.Length; i++)
			{
				string letter = text[i].ToString();

				float letterWidth = abyssalFont.MeasureString(letter).X;
				bool empty = string.IsNullOrWhiteSpace(letter);

				float letterCenterX = cursor.X + letterWidth * baseScale * 0.5f;
				float distanceFromCenter = MathF.Abs(letterCenterX - centerX);

				float reached = frontDistance - distanceFromCenter;

				float swapAmount = MathHelper.Clamp(reached / transitionWidth, 0f, 1f);
				swapAmount = MathHelper.SmoothStep(0f, 1f, swapAmount);

				Color textColor = Color.Lerp(oldColor, newColor, swapAmount);
				textColor.A = 255;

				float bandCenterDistance = MathF.Abs(reached);

				float bandAmount = 1f - bandCenterDistance / transitionWidth;
				bandAmount = MathHelper.Clamp(bandAmount, 0f, 1f);

				float birthFade = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(rawProgress / 0.18f, 0f, 1f));

				bandAmount *= birthFade;
				bandAmount = MathHelper.Clamp(bandAmount, 0f, 1f);

				float wave = MathF.Cos(i * 0.35f + time * 3f);
				float charScale = baseScale + wave * 0.01f;

				if (!empty)
				{
					for (int x = 0; x < 10; x++)
					{
						float angle = x / 10f * MathHelper.TwoPi + time * 2f;
						Vector2 glowOffset = new Vector2(1f).RotatedBy(angle) * (1f + bandAmount * 3f);

						Utils.DrawBorderStringFourWay(
							spritebatch,
							abyssalFont,
							letter,
							cursor.X + glowOffset.X,
							cursor.Y + glowOffset.Y + wave,
							textColor,
							Color.Black,
							Vector2.Zero,
							charScale
						);
					}
				}

				cursor.X += letterWidth * charScale;
			}

			spritebatch.ResetToDefaultUI();

			cursor = position;

			for (int i = 0; i < text.Length; i++)
			{
				string letter = text[i].ToString();

				float letterWidth = abyssalFont.MeasureString(letter).X;
				bool empty = string.IsNullOrWhiteSpace(letter);

				float letterCenterX = cursor.X + letterWidth * baseScale * 0.5f;
				float distanceFromCenter = MathF.Abs(letterCenterX - centerX);

				float reached = frontDistance - distanceFromCenter;

				float swapAmount = MathHelper.Clamp(reached / transitionWidth, 0f, 1f);
				swapAmount = MathHelper.SmoothStep(0f, 1f, swapAmount);

				float wave = MathF.Cos(i * 0.35f + time * 3f);
				float charScale = baseScale + wave * 0.01f;

				if (!empty)
				{
					Utils.DrawBorderStringFourWay(
						spritebatch,
						abyssalFont,
						letter,
						cursor.X,
						cursor.Y + wave,
						Color.Black,
						Color.Transparent,
						Vector2.Zero,
						charScale
					);
				}

				cursor.X += letterWidth * charScale;
			}
		}
	}
}
