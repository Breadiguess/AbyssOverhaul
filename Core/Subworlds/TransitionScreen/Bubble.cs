using Terraria.GameContent;

namespace AbyssOverhaul.Core.Subworlds.TransitionScreen
{
    internal sealed class Bubble
    {
        public Vector2 Position;

        public bool ShouldBeRemoved { get; private set; }
        public bool Foreground { get; }

        private readonly float _riseSpeed;
        private readonly float _horizontalDrift;
        private readonly float _wobbleAmount;
        private readonly float _wobbleSpeed;
        private readonly float _scale;
        private readonly float _baseOpacity;

        private float _centerX;
        private float _time;

        public Bubble(
            Vector2 position,
            float scale,
            float riseSpeed,
            bool foreground)
        {
            Position = position;
            Foreground = foreground;

            _centerX = position.X;
            _scale = scale;
            _riseSpeed = riseSpeed;

            _horizontalDrift = Main.rand.NextFloat(-7f, 7f);
            _wobbleAmount = Main.rand.NextFloat(3f, 14f);
            _wobbleSpeed = Main.rand.NextFloat(1.2f, 2.8f);
            _baseOpacity = Main.rand.NextFloat(0.45f, 0.9f);

            // Prevent every bubble from wobbling in sync.
            _time = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Avoid an enormous movement jump after a lag spike.
            deltaTime = Math.Min(deltaTime, 1f / 20f);

            _time += deltaTime;

            _centerX += _horizontalDrift * deltaTime;

            Position.X =
                _centerX +
                MathF.Sin(_time * _wobbleSpeed) * _wobbleAmount;

            Position.Y -= _riseSpeed * deltaTime;

            float removalDistance = 32f * _scale;

            if (Position.Y < -removalDistance)
                ShouldBeRemoved = true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureAssets.Bubble.Value;

            Vector2 origin = new Vector2(
                texture.Width * 0.5f,
                texture.Height * 0.5f
            );

            // Fade out as the bubble reaches the top of the screen.
            float topFade = MathHelper.Clamp(
                (Position.Y + 60f) / 100f,
                0f,
                1f
            );

            float opacity = _baseOpacity * topFade;

            spriteBatch.Draw(
                texture,
                Position,
                null,
                Color.White * opacity,
                0f,
                origin,
                _scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
