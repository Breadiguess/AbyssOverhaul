using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Systems;
using System.IO;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.ModPlayers
{
    public class PressurePlayer : ModPlayer
    {
        /// <summary>
        /// Flat amount of ambient pressure ignored by gear/buffs.
        /// </summary>
        public float PressureResistance;

        public float AmbientPressure;

        /// <summary>
        /// How adapted the player is to the current effective pressure.
        /// </summary>
        public float Adaptation;

        /// <summary>
        /// Final harmful stress value used by penalties.
        /// </summary>
        public float PressureStress;

        public AbyssLayer CurrentLayer;
        public float GlobalDepthInterpolant;
        public float LayerDepthInterpolant;
        public bool InPressureZone;

        public float PressureResidue;
        private bool _wasInPressureZone;

        public float EffectiveAmbientPressure => Math.Max(AmbientPressure - PressureResistance, 0f);
        public float Overpressure => Math.Max(EffectiveAmbientPressure - Adaptation, 0f);
        public float EffectivePressureStress => Math.Max(PressureStress, PressureResidue);

        public override void ResetEffects()
        {
            if (CurrentLayer is null)
                InPressureZone = false;

            CurrentLayer = null;
            GlobalDepthInterpolant = 0f;
            LayerDepthInterpolant = 0f;
            PressureResistance = 0f;
        }

        public override void PostUpdate()
        {
            UpdatePressureFromLayerSystem();
        }

        public override void PostUpdateMiscEffects()
        {
            ApplyPressureDefensePenalty();
        }

        public override void UpdateBadLifeRegen()
        {
            ApplyPressureRegenPenalty();
        }

        private void UpdatePressureFromLayerSystem()
        {
            if (!PresssureSystem.TryGetAbyssInfo(Player, out var info))
            {
                InPressureZone = false;
                CurrentLayer = null;
                AmbientPressure = 0f;

                Adaptation = MathHelper.Lerp(Adaptation, 0f, 0.04f);
                PressureStress = MathHelper.Lerp(PressureStress, 0f, 0.08f);
            }
            else
            {
                InPressureZone = true;
                CurrentLayer = info.Layer;
                GlobalDepthInterpolant = info.GlobalDepthInterpolant;
                LayerDepthInterpolant = info.LayerDepthInterpolant;

                // Make the environment much harsher with depth.
                AmbientPressure = MathHelper.Lerp(15f, 140f, GlobalDepthInterpolant);

                float effectiveAmbient = EffectiveAmbientPressure;

                float verticalSpeed = MathF.Abs(Player.velocity.Y);
                float ascentSpeed = Math.Max(-Player.velocity.Y, 0f);
                float descentSpeed = Math.Max(Player.velocity.Y, 0f);

                float adaptRate = 0.01f;
                if (verticalSpeed < 1.5f)
                    adaptRate = 0.016f;
                else if (verticalSpeed > 6f)
                    adaptRate = 0.004f;

                // Adapt to the resisted pressure, not raw ambient.
                Adaptation = MathHelper.Lerp(Adaptation, effectiveAmbient, adaptRate);

                float overpressure = Overpressure;

                // This is the important part:
                // deeper layers produce a baseline crushing force even while moving slowly.
                // gear offsets it by reducing effectiveAmbient first.
                float baselineDepthStress = Math.Max(effectiveAmbient - 22f, 0f) * 0.85f;

                // Extra punishment if your gear is lagging far behind the current depth.
                float gearDeficit = Math.Max(AmbientPressure - PressureResistance, 0f);
                float gearDeficitStress = Math.Max(gearDeficit - 18f, 0f) * 0.45f;

                // Adaptation mismatch still matters, especially during descent / ascent.
                float mismatchStress = overpressure * 1.15f;

                // Movement spikes.
                float descentStress = Math.Max(descentSpeed - 2.5f, 0f) * (1.2f + 1.3f * GlobalDepthInterpolant);
                float ascentStress = Math.Max(ascentSpeed - 2.5f, 0f) * (1.5f + 1.5f * GlobalDepthInterpolant);

                float targetStress =
                    baselineDepthStress +
                    gearDeficitStress +
                    mismatchStress +
                    descentStress +
                    ascentStress;

                PressureStress = MathHelper.Lerp(PressureStress, targetStress, 0.18f);
                PressureStress = MathHelper.Clamp(PressureStress, 0f, 160f);
            }

            // Leaving the abyss too quickly still hurts, but resistance helps.
            if (_wasInPressureZone && !InPressureZone)
            {
                float decompressionSeverity = Math.Max(Adaptation - PressureResistance * 0.5f, 0f);
                PressureResidue += decompressionSeverity * 0.75f;
            }

            if (InPressureZone)
                PressureResidue = Math.Max(PressureResidue, PressureStress * 0.9f);
            else
                PressureResidue = Math.Max(PressureResidue - 0.4f, 0f);

            PressureResidue = MathHelper.Clamp(PressureResidue, 0f, 160f);

            if (PressureResidue >= 80f && Main.myPlayer == Player.whoAmI)
            {
                Player.Hurt(PlayerDeathReason.ByCustomReason($"{Player.name} was crushed by the abyssal pressure."), 9999, 0, dodgeable: false);
                PressureResidue = 0f;
            }

            _wasInPressureZone = InPressureZone;
        }

        private void ApplyPressureDefensePenalty()
        {
            if (!InPressureZone)
                return;

            float stress = PressureStress;
            int defenseLoss = 0;

            if (stress >= 70f)
                defenseLoss = 36;
            else if (stress >= 45f)
                defenseLoss = 22;
            else if (stress >= 25f)
                defenseLoss = 12;
            else if (stress >= 10f)
                defenseLoss = 5;

            Player.statDefense -= defenseLoss;
        }

        private void ApplyPressureRegenPenalty()
        {
            if (!InPressureZone)
                return;

            float stress = PressureStress;

            if (stress >= 70f)
            {
                Player.lifeRegenTime = 0;
                Player.lifeRegen = Math.Min(Player.lifeRegen, 0);
                Player.lifeRegen -= 300;
            }
            else if (stress >= 45f)
            {
                Player.lifeRegenTime = 0;
                Player.lifeRegen = Math.Min(Player.lifeRegen, 0);
                Player.lifeRegen -= 160;
            }
            else if (stress >= 25f)
            {
                Player.lifeRegenTime = 0;
                Player.lifeRegen = Math.Min(Player.lifeRegen, 0);
                Player.lifeRegen -= 40;
            }
            else if (stress >= 10f)
            {
                Player.lifeRegenTime = 0;
                if (Player.lifeRegen > 0)
                    Player.lifeRegen -= 2;
            }
        }

        #region NetSyncing
        private static ushort Pack01(float value) => (ushort)(MathHelper.Clamp(value, 0f, 1f) * 65535f);
        private static float Unpack01(ushort value) => value / 65535f;

        private static ushort PackPressure(float value, float max = 120f) => (ushort)(MathHelper.Clamp(value, 0f, max) / max * 65535f);
        private static float UnpackPressure(ushort value, float max = 120f) => value / 65535f * max;

        public override void CopyClientState(ModPlayer targetCopy)
        {
            PressurePlayer clone = (PressurePlayer)targetCopy;
            clone.InPressureZone = InPressureZone;
            clone.PressureResistance = PressureResistance;
            clone.AmbientPressure = AmbientPressure;
            clone.Adaptation = Adaptation;
            clone.PressureStress = PressureStress;
            clone.GlobalDepthInterpolant = GlobalDepthInterpolant;
            clone.LayerDepthInterpolant = LayerDepthInterpolant;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            PressurePlayer old = (PressurePlayer)clientPlayer;

            bool changed =
                old.InPressureZone != InPressureZone ||
                MathF.Abs(old.AmbientPressure - AmbientPressure) >= 1f ||
                MathF.Abs(old.PressureResistance - PressureResistance) >= 1f ||
                MathF.Abs(old.Adaptation - Adaptation) >= 1f ||
                MathF.Abs(old.PressureStress - PressureStress) >= 1f ||
                MathF.Abs(old.GlobalDepthInterpolant - GlobalDepthInterpolant) >= 0.01f ||
                MathF.Abs(old.LayerDepthInterpolant - LayerDepthInterpolant) >= 0.01f;

            if (!changed)
                return;

            SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();

            packet.Write((byte)AbyssOverhaulMessageType.SyncPressurePlayer);
            packet.Write((byte)Player.whoAmI);

            packet.Write(InPressureZone);
            packet.Write(PackPressure(AmbientPressure, 120f));
            packet.Write(PackPressure(PressureResistance, 320f));
            packet.Write(PackPressure(Adaptation, 120f));
            packet.Write(PackPressure(PressureStress, 120f));
            packet.Write(Pack01(GlobalDepthInterpolant));
            packet.Write(Pack01(LayerDepthInterpolant));

            packet.Send(toWho, fromWho);
        }

        public void ReceiveSync(BinaryReader reader)
        {
            InPressureZone = reader.ReadBoolean();

            AmbientPressure = UnpackPressure(reader.ReadUInt16(), 120f);
            PressureResistance = UnpackPressure(reader.ReadUInt16(), 320f);
            Adaptation = UnpackPressure(reader.ReadUInt16(), 120f);
            PressureStress = UnpackPressure(reader.ReadUInt16(), 120f);

            GlobalDepthInterpolant = Unpack01(reader.ReadUInt16());
            LayerDepthInterpolant = Unpack01(reader.ReadUInt16());
        }
        #endregion
    }
}