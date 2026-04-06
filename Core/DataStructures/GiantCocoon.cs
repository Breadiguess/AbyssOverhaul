using BreadLibrary.Core.Verlet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
