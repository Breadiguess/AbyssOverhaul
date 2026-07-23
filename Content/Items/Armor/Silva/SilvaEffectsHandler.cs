using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Armor.Silva;
using MonoMod.Cil;
using System.Reflection;

namespace AbyssOverhaul.Content.Items.Armor.Silva
{
	public class SilvaEffectsHandler : ILoadable
	{
		public void Load(Mod mod)
		{
			MonoModHooks.Modify(
					typeof(CalamityPlayer).GetMethod(nameof(CalamityPlayer.MiscEffects), BindingFlags.Instance | BindingFlags.NonPublic),
					EditSilvaReviveEffects
				);

			MonoModHooks.Modify(
					typeof(CalamityPlayer).GetMethod(nameof(CalamityPlayer.PreKill), BindingFlags.Instance | BindingFlags.Public),
					EditSilvaActivationEffect
				);
		}

		public void Unload() { }

		internal static void EditSilvaActivationEffect(ILContext il)
		{
			ILCursor c = new(il);

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchNop(),
				i => i.MatchLdsflda<SilvaArmor>(nameof(SilvaArmor.ActivationSound))))
			{
				LogILError("Failed to locate silva revive start sound");
				return;
			}

			c.EmitLdarg(0);
			c.EmitDelegate(SilvaEffectsEditor.SilvaReviveStartEffect);
		}

		internal static void EditSilvaReviveEffects(ILContext il)
		{
			ILCursor c = new(il);

			int cooldownDustLoop_varNum = -1;

			if (!c.TryGotoNext(MoveType.Before, 
				i => i.MatchNop(), 
				i => i.MatchLdsflda<SilvaArmor>(nameof(SilvaArmor.DispelSound))))
			{
				LogILError("Failed to locate silva revive end sound");
				return;
			}

			// Technically ends 1 frame before the main effects, but thats on calamity developers being actually stupid
			c.EmitLdarg(0);
			c.EmitDelegate(SilvaEffectsEditor.SilvaReviveEndEffect);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdsfld<SilvaArmor>(nameof(SilvaArmor.ReviveCooldown)),
				i => i.MatchLdcI4(out _),
				i => i.MatchCall("CalamityMod.CalamityUtils", nameof(CalamityUtils.AddCooldown)),
				i => i.MatchPop(),
				i => i.MatchNop(),
				i => i.MatchLdcI4(out _),
				i => i.MatchStloc(out cooldownDustLoop_varNum)))
			{
				LogILError("Failed to locate the silva cooldown revive cooldown code");
				return;
			}

			c.EmitLdarg(0);
			c.EmitDelegate(SilvaEffectsEditor.SilvaReviveEffects);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdloc(cooldownDustLoop_varNum),
				i => i.MatchNop(),
				i => i.MatchNop(),
				i => i.MatchLdcI4(out _)))
			{
				LogILError("Failed to locate the silva revive dust effects max loop count");
				return;
			}

			c.EmitDelegate((int dustLoopMax) =>
			{
				return SilvaEffectsEditor.SilvaReviveSpawnsChloroDusts() ? dustLoopMax : 0;
			});
		}
	}
}
