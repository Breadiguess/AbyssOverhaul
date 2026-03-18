using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.Carcasses
{
    public class CarcassSnapshot
    {
        public int NPCType;
        public int Width;
        public int Height;
        public float Scale;
        public int SpriteDirection;
        public float Rotation;
        public Color ColorTint;
        public string GivenName = "";
        public int LifeMax;
        public Rectangle Hitbox;

        public static CarcassSnapshot FromNPC(NPC npc)
        {
            return new CarcassSnapshot
            {
                NPCType = npc.type,
                Width = npc.width,
                Height = npc.height,
                Scale = npc.scale,
                SpriteDirection = npc.spriteDirection,
                Rotation = npc.rotation,
                ColorTint = npc.color,
                GivenName = npc.GivenName ?? "",
                LifeMax = npc.lifeMax,
                Hitbox = npc.Hitbox
            };
        }

        public TagCompound Save()
        {
            return new TagCompound
            {
                ["NPCType"] = NPCType,
                ["Width"] = Width,
                ["Height"] = Height,
                ["Scale"] = Scale,
                ["SpriteDirection"] = SpriteDirection,
                ["Rotation"] = Rotation,
                ["ColorTintR"] = ColorTint.R,
                ["ColorTintG"] = ColorTint.G,
                ["ColorTintB"] = ColorTint.B,
                ["ColorTintA"] = ColorTint.A,
                ["GivenName"] = GivenName,
                ["LifeMax"] = LifeMax,
                ["Hitbox"] = Hitbox
            };
        }

        public static CarcassSnapshot Load(TagCompound tag)
        {
            return new CarcassSnapshot
            {
                NPCType = tag.GetInt("NPCType"),
                Width = tag.GetInt("Width"),
                Height = tag.GetInt("Height"),
                Scale = tag.GetFloat("Scale"),
                SpriteDirection = tag.GetInt("SpriteDirection"),
                Rotation = tag.GetFloat("Rotation"),
                ColorTint = new Color(
                    tag.GetByte("ColorTintR"),
                    tag.GetByte("ColorTintG"),
                    tag.GetByte("ColorTintB"),
                    tag.GetByte("ColorTintA")),
                GivenName = tag.GetString("GivenName"),
                LifeMax = tag.GetInt("LifeMax"),
                Hitbox = tag.Get<Rectangle>("Hitbox")
            };
        }

        public void NetSend(BinaryWriter writer)
        {
            writer.Write(NPCType);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Scale);
            writer.Write(SpriteDirection);
            writer.Write(Rotation);
            writer.Write(ColorTint.R);
            writer.Write(ColorTint.G);
            writer.Write(ColorTint.B);
            writer.Write(ColorTint.A);
            writer.Write(GivenName);
            writer.Write(LifeMax);

        }

        public static CarcassSnapshot NetReceive(BinaryReader reader)
        {
            return new CarcassSnapshot
            {
                NPCType = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                Scale = reader.ReadSingle(),
                SpriteDirection = reader.ReadInt32(),
                Rotation = reader.ReadSingle(),
                ColorTint = new Color(
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte()),
                GivenName = reader.ReadString(),
                LifeMax = reader.ReadInt32()
            };
        }
    }
}
