namespace AbyssOverhaul.Common.OldBrain
{
    public interface IGoal
    {
        string Name { get; }

        bool CanRun(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
        float Score(NpcContext ctx, NpcMemory memory, NpcNeeds needs);

        void OnEnter(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
        void OnExit(NpcContext ctx, NpcMemory memory, NpcNeeds needs);

        IAction GetInitialAction(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
        IAction GetNextAction(NpcContext ctx, NpcMemory memory, NpcNeeds needs, IAction previous);

        bool ShouldInterrupt(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
    }
}