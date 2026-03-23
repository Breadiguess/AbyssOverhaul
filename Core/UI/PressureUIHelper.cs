using AbyssOverhaul.Core.ModPlayers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.UI
{
    public static class PressureUIHelper
    {
        public const float AmbientPressureBarMax = 140f;
        public const float DangerBarMax = 80f;

        public static PressureDangerInfo GetDangerInfo(float effectiveStress)
        {
            if (effectiveStress >= 80f)
                return new("CRUSHING", new Color(186, 85, 211), 36, 300);

            if (effectiveStress >= 70f)
                return new("LETHAL", new Color(255, 70, 70), 36, 300);

            if (effectiveStress >= 45f)
                return new("CRITICAL", new Color(255, 145, 60), 22, 160);

            if (effectiveStress >= 25f)
                return new("DANGEROUS", new Color(255, 215, 90), 12, 40);

            if (effectiveStress >= 10f)
                return new("ELEVATED", new Color(180, 230, 120), 5, 2);

            return new("STABLE", new Color(120, 210, 255), 0, 0);
        }

        public static float GetPressureFill(PressurePlayer pressure) =>
            MathHelper.Clamp(pressure.EffectiveAmbientPressure / AmbientPressureBarMax, 0f, 1f);

        public static float GetDangerFill(PressurePlayer pressure) =>
            MathHelper.Clamp(pressure.EffectivePressureStress / DangerBarMax, 0f, 1f);
    }
}
