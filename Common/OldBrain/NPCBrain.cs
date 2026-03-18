using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Social.WeGame;

namespace AbyssOverhaul.Common.OldBrain
{
    public sealed class NpcBrain
    {
        public NpcContext Context;
        public NpcMemory Memory = new();
        public NpcNeeds Needs = new();

        public List<IGoal> Goals = new();

        public IGoal CurrentGoal;
        public IAction CurrentAction;

        public int DecisionCooldown;
        public int ReevaluateRate = 15;

        public void Update()
        {
            Context.Sense(Memory, Needs);

            Needs.Update(Context, Memory);

            if (DecisionCooldown <= 0 || ShouldInterruptCurrentGoal())
            {
                SelectBestGoal();
                DecisionCooldown = ReevaluateRate;
            }
            else
                DecisionCooldown--;

            CurrentAction?.Tick(Context, Memory, Needs);

            if (CurrentAction != null && CurrentAction.IsFinished(Context, Memory, Needs))
                AdvanceAction();
        }

        private void SelectBestGoal()
        {
            IGoal bestGoal = null;
            float bestScore = float.MinValue;

            foreach (var goal in Goals)
            {
                if (!goal.CanRun(Context, Memory, Needs))
                    continue;

                float score = goal.Score(Context, Memory, Needs);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGoal = goal;
                }
            }

            if (bestGoal != CurrentGoal)
            {
                CurrentGoal?.OnExit(Context, Memory, Needs);
                CurrentGoal = bestGoal;
                CurrentGoal?.OnEnter(Context, Memory, Needs);

                CurrentAction = CurrentGoal?.GetInitialAction(Context, Memory, Needs);
            }
        }

        private void AdvanceAction()
        {
            if (CurrentGoal == null)
            {
                CurrentAction = null;
                return;
            }

            CurrentAction = CurrentGoal.GetNextAction(Context, Memory, Needs, CurrentAction);
        }

        private bool ShouldInterruptCurrentGoal()
        {
            if (CurrentGoal == null)
                return true;

            return CurrentGoal.ShouldInterrupt(Context, Memory, Needs);
        }
    }
}
