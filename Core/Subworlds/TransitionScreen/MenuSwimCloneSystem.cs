using Microsoft.Xna.Framework.Input;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Subworlds.TransitionScreen
{
    public class MenuSwimCloneSystem : ModSystem
    {
        public static Vector2 ScreenCenter;
        public static Vector2 ScreenVelocity;
        public static int Direction = 1;
        public static double BodyFrameCounter;
        public static double LegFrameCounter;
        public static bool Initialized;

        public override void OnWorldUnload()
        {
            Initialized = false;
            ScreenCenter = Vector2.Zero;
            ScreenVelocity = Vector2.Zero;
            Direction = 1;
            BodyFrameCounter = 0d;
            LegFrameCounter = 0d;
        }
        public override void Load()
        {
            //On_Main.DrawMenu += On_Main_DrawMenu;
        }

        private void On_Main_DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);


            Player p = Main.LocalPlayer;
            if (p is null || !p.active)
            {
                Initialized = false;
                return;
            }

            if (!Initialized)
            {
                ScreenCenter = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.58f);
                ScreenVelocity = Vector2.Zero;
                Direction = 1;
                Initialized = true;
            }

            KeyboardState k = Main.keyState;
            KeyboardState old = Main.oldKeyState;

            Vector2 accel = Vector2.Zero;

            if (k.IsKeyDown(Keys.A))
                accel.X -= 0.28f;

            if (k.IsKeyDown(Keys.D))
                accel.X += 0.28f;

            if (k.IsKeyDown(Keys.W) || k.IsKeyDown(Keys.Space))
                accel.Y -= 0.22f;

            if (k.IsKeyDown(Keys.S))
                accel.Y += 0.22f;

            float t = (float)Main.GlobalTimeWrappedHourly;
            accel.X += MathF.Cos(t * 1.35f) * 0.01f;
            accel.Y += MathF.Sin(t * 1.7f) * 0.008f;

            ScreenVelocity += accel;
            ScreenVelocity *= 0.955f;

            float maxSpeed = 4.75f;
            if (ScreenVelocity.LengthSquared() > maxSpeed * maxSpeed)
                ScreenVelocity = Vector2.Normalize(ScreenVelocity) * maxSpeed;

            ScreenCenter += ScreenVelocity;

            float marginX = 80f;
            float marginY = 70f;
            float minX = marginX;
            float maxX = Main.screenWidth - marginX;
            float minY = marginY;
            float maxY = Main.screenHeight - marginY;

            if (ScreenCenter.X < minX)
            {
                ScreenCenter.X = minX;
                ScreenVelocity.X *= -0.35f;
            }
            else if (ScreenCenter.X > maxX)
            {
                ScreenCenter.X = maxX;
                ScreenVelocity.X *= -0.35f;
            }

            if (ScreenCenter.Y < minY)
            {
                ScreenCenter.Y = minY;
                ScreenVelocity.Y *= -0.35f;
            }
            else if (ScreenCenter.Y > maxY)
            {
                ScreenCenter.Y = maxY;
                ScreenVelocity.Y *= -0.35f;
            }

            if (MathF.Abs(ScreenVelocity.X) > 0.08f)
                Direction = ScreenVelocity.X > 0f ? 1 : -1;
            else if (k.IsKeyDown(Keys.A))
                Direction = -1;
            else if (k.IsKeyDown(Keys.D))
                Direction = 1;

            float animSpeed = 0.45f + ScreenVelocity.Length() * 0.55f;
            BodyFrameCounter += animSpeed;
            LegFrameCounter += animSpeed * 1.08f;

            // Example edge detection if you need it later:
            bool justPressedSpace = k.IsKeyDown(Keys.Space) && !old.IsKeyDown(Keys.Space);

        }

        //laggy as shit, move somehwere else

   

        public override void PostUpdatePlayers()
        {
          
        }
    }
}
