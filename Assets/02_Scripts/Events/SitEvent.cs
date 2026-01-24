
public interface ISitActor
{
    void ExecuteSit();
}

public static class SitEvent
{
    public static void Execute(ISitActor actor)
    {
        actor.ExecuteSit();
    }
}
