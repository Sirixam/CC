
public interface IActor
{
    string ID { get; }

    public static string GetPlayerID(int index)
    {
        return "Player_" + index;
    }

    public static string GetNpcID(int index)
    {
        return "Npc_" + index;
    }
}
