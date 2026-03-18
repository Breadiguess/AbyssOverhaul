using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace AbyssOverhaul.Core.Graphics;

/// <summary>
/// lifted from https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/Graphics/RenderTargets/InstancedRequestableTarget.cs
/// Thank you Lucille!
/// </summary>
public class InstancedRequestableTarget : INeedRenderTargetContent
{
    private bool isReady;

    private readonly List<InstancedTargetData> targetInstances = [];

    private readonly List<int> requestedInstanceIdentifiers = [];

    public bool IsReady => isReady;

    /// <summary>
    /// Whether any render target instances are held in this requestable target.
    /// </summary>
    public bool AnyTargetsAllocated => targetInstances.Count >= 1;

    public void Request(int width, int height, int identifier, Action drawAction)
    {
        InstancedTargetData existingInstance = targetInstances.FirstOrDefault(n => n.Identifier == identifier);
        if (existingInstance is null)
            targetInstances.Add(new(drawAction, width, height, identifier));

        // Ensure that the requested target dimensions are valid.
        // If they aren't, reset them.
        else if (existingInstance.Target.Width != width || existingInstance.Target.Height != height)
        {
            existingInstance.Dispose();
            targetInstances.Remove(existingInstance);
            targetInstances.Add(new(drawAction, width, height, identifier));
        }

        if (existingInstance is not null)
            existingInstance.DrawAction = drawAction;

        if (!requestedInstanceIdentifiers.Contains(identifier))
            requestedInstanceIdentifiers.Add(identifier);
    }

    /// <summary>
    /// Attempts to acquire a given target with a given identifier, returning whether it is ready or not.
    /// </summary>
    /// <param name="identifier">The identifier to search for.</param>
    public bool TryGetTarget(int identifier, out RenderTarget2D target)
    {
        target = null;
        for (int i = 0; i < targetInstances.Count; i++)
        {
            if (targetInstances[i] is not null && targetInstances[i].Identifier == identifier)
            {
                target = targetInstances[i].Target;
                break;
            }
        }

        return target is not null;
    }

    public void PrepareRenderTarget(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        if (requestedInstanceIdentifiers.Count <= 0 || targetInstances.Count <= 0)
            return;

        isReady = false;

        for (int i = 0; i < targetInstances.Count; i++)
        {
            InstancedTargetData instance = targetInstances[i];
            if (!requestedInstanceIdentifiers.Contains(instance.Identifier))
                continue;

            device.SetRenderTarget(instance.Target);
            device.Clear(Color.Transparent);
            instance.DrawAction();
        }

        // Return to the backbuffer.
        device.SetRenderTarget(null);

        // Reset the set of requested instances.
        requestedInstanceIdentifiers.Clear();

        isReady = true;
    }

    /// <summary>
    /// Attempts to clear this target, disposing unmanaged data for all of its target sub-instances.
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < targetInstances.Count; i++)
            targetInstances[i].Target.Dispose();
        targetInstances.Clear();

        isReady = false;
        requestedInstanceIdentifiers.Clear();
    }
}
