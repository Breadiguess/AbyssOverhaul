using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.UI
{
    public readonly struct PressureDangerInfo
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly int DefenseLoss;
        public readonly int RegenPenalty;

        public PressureDangerInfo(string name, Color color, int defenseLoss, int regenPenalty)
        {
            Name = name;
            Color = color;
            DefenseLoss = defenseLoss;
            RegenPenalty = regenPenalty;
        }
    }
}
