using BreadLibrary.Core.Verlet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.DataStructures
{
    public sealed class GiantCocoon
    {
        public VerletChain[] Connectors;
        public Vector2 WorldCenter;
        public float BaseScale;
        public float Rotation;
        public bool Active = true;

        public GiantCocoon(Vector2 worldCenter, float baseScale = 1f, float rotation = 0f)
        {
            WorldCenter = worldCenter;
            BaseScale = baseScale;
            Rotation = rotation;
        }
        public TagCompound Save()
        {
            return new TagCompound
            {
                ["X"] = WorldCenter.X,
                ["Y"] = WorldCenter.Y,
                ["BaseScale"] = BaseScale,
                ["Rotation"] = Rotation,
                ["Active"] = Active
            };
        }
        public static GiantCocoon Load(TagCompound tag)
        {
            Vector2 worldCenter = new Vector2(
                tag.GetFloat("X"),
                tag.GetFloat("Y")
            );

            GiantCocoon cocoon = new GiantCocoon(
                worldCenter,
                tag.GetFloat("BaseScale"),
                tag.GetFloat("Rotation")
            );

            if (tag.ContainsKey("Active"))
                cocoon.Active = tag.GetBool("Active");

            return cocoon;
        }
    }
}
