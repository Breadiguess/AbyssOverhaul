using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.TerritorySystem
{
    public class Territory
    {
        public int ID;


        public Entity Owner;
        public Vector2 Center => Bounds.Center();

        public Rectangle Bounds;

        public Territory(Vector2 Pos, Rectangle Bounds)
        {
            Bounds.Location = (Pos-Bounds.Size()/2).ToPoint();
            this.Bounds = Bounds;

            ID = TerritoryRegistry.Count;
            TerritoryRegistry.Register(this);
        }
        

      

        public void Draw()
        {
            string thing = Owner.ToString();

            

            Utils.DrawBorderString(Main.spriteBatch, Owner.ToString(), Bounds.TopLeft() - Main.screenPosition + Vector2.One*10, Color.White);
            Utils.DrawLine(Main.spriteBatch, Bounds.TopLeft() + Vector2.UnitY * 40, Bounds.TopLeft() + Vector2.UnitY * 40 + Vector2.UnitX * thing.Length*10, Color.White, Color.White, 5);
            Utils.DrawRect(Main.spriteBatch, Bounds, Color.White);


        }
    }
}
