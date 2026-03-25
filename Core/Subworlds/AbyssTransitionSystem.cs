using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Subworlds
{
    internal sealed class AbyssTransitionSystem : ModSystem
    {
        internal static bool SuppressPlayerSaving;

        private enum TransitionPhase
        {
            None,
            FadingOutToSubworld,
            FadingOutToMainWorld,
            WaitingForWorldLoad,
            WaitingToPlacePlayer,
            FadingIn
        }

        private sealed class PendingTransition
        {
            public TransitionPhase Phase;
            public float Fade;
            public int ArrivalDelay;
            public bool SwapIssued;

            public Vector2 TargetArrivalWorld;
            public Vector2 SavedMainWorldReturnWorld;
            public Vector2 SavedVelocity;
            public int SavedDirection;

            public bool HasSavedReturnPoint;
        }

        private static PendingTransition _pending = new();

        public static bool IsBusy => _pending.Phase != TransitionPhase.None;

        public static bool RequestEnter(Vector2 subworldArrivalWorld, Vector2? mainWorldReturnPoint = null)
        {
            if (Main.dedServ || Main.gameMenu || IsBusy)
                return false;

            Player player = Main.LocalPlayer;
            if (player is null || !player.active || player.dead)
                return false;



            // Disable player saving only for the expensive entry handoff.
            SuppressPlayerSaving = true;

            _pending = new PendingTransition
            {
                Phase = TransitionPhase.FadingOutToSubworld,
                Fade = 0f,
                ArrivalDelay = 0,
                SwapIssued = false,
                TargetArrivalWorld = subworldArrivalWorld,
                SavedMainWorldReturnWorld = mainWorldReturnPoint ?? player.Center,
                SavedVelocity = player.velocity,
                SavedDirection = player.direction,
                HasSavedReturnPoint = true
            };
            return true;
        }

        public static bool RequestExit()
        {
            if (Main.dedServ || Main.gameMenu || IsBusy)
                return false;

            Player player = Main.LocalPlayer;
            if (player is null || !player.active || player.dead)
                return false;

            if (!SubworldSystem.IsActive<AbyssSubworld>())
                return false;

            _pending.Phase = TransitionPhase.FadingOutToMainWorld;
            _pending.Fade = 0f;
            _pending.ArrivalDelay = 0;
            _pending.SwapIssued = false;
            _pending.SavedVelocity = player.velocity;
            _pending.SavedDirection = player.direction;

            if (!_pending.HasSavedReturnPoint)
                _pending.SavedMainWorldReturnWorld = player.Center;

            return true;
        }

        public override void OnWorldLoad()
        {
            if (_pending.Phase == TransitionPhase.WaitingForWorldLoad)
            {
                _pending.Phase = TransitionPhase.WaitingToPlacePlayer;
                _pending.ArrivalDelay = 3;
                _pending.Fade = 1f;
            }
        }

        public override void PostUpdatePlayers()
        {
            if (Main.dedServ || !IsBusy)
                return;

            Player player = Main.LocalPlayer;
            if (player is null || !player.active)
                return;

            switch (_pending.Phase)
            {
                case TransitionPhase.FadingOutToSubworld:
                    _pending.Fade = MathHelper.Clamp(_pending.Fade + 0.08f, 0f, 1f);

                    if (_pending.Fade >= 1f && !_pending.SwapIssued)
                    {
                        _pending.SwapIssued = true;

                        bool success = SubworldSystem.Enter<AbyssSubworld>();
                        if (success)
                            _pending.Phase = TransitionPhase.WaitingForWorldLoad;
                        else
                            Clear();
                    }
                    break;

                case TransitionPhase.FadingOutToMainWorld:
                    _pending.Fade = MathHelper.Clamp(_pending.Fade + 0.08f, 0f, 1f);

                    if (_pending.Fade >= 1f && !_pending.SwapIssued)
                    {
                        _pending.SwapIssued = true;
                        SubworldSystem.Exit();
                        _pending.Phase = TransitionPhase.WaitingForWorldLoad;
                    }
                    break;

                case TransitionPhase.WaitingToPlacePlayer:
                    if (_pending.ArrivalDelay > 0)
                    {
                        _pending.ArrivalDelay--;
                        break;
                    }

                    Vector2 destination = SubworldSystem.IsActive<AbyssSubworld>()
                        ? _pending.TargetArrivalWorld
                        : _pending.SavedMainWorldReturnWorld;

                    ApplyArrival(player, destination, _pending.SavedVelocity, _pending.SavedDirection);

                    // We are fully inside now. Re-enable normal player saving.
                    SuppressPlayerSaving = false;

                    _pending.Phase = TransitionPhase.FadingIn;
                    break;

                case TransitionPhase.FadingIn:
                    _pending.Fade = MathHelper.Clamp(_pending.Fade - 0.06f, 0f, 1f);

                    if (_pending.Fade <= 0f)
                        Clear();
                    break;
            }
        }

        private static void ApplyArrival(Player player, Vector2 center, Vector2 velocity, int direction)
        {
            player.Center = center;
            player.velocity = velocity * 0.35f;
            player.direction = direction;
            player.fallStart = (int)(player.position.Y / 16f);
        }

        private static void Clear()
        {
            Vector2 savedReturn = _pending.SavedMainWorldReturnWorld;
            bool hadReturn = _pending.HasSavedReturnPoint;

            _pending = new PendingTransition
            {
                SavedMainWorldReturnWorld = savedReturn,
                HasSavedReturnPoint = hadReturn
            };

            // Safety: never leave this stuck on.
            SuppressPlayerSaving = false;
        }

        public override void OnWorldUnload()
        {
            // Extra safety for failed transitions or menu returns.
            SuppressPlayerSaving = false;
        }
    }
}