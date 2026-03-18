namespace AbyssOverhaul.Common.OldBrain
{
    public interface IAction
    {
        void Tick(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
        bool IsFinished(NpcContext ctx, NpcMemory memory, NpcNeeds needs);
    }
}