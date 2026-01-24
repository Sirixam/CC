
public interface IActor
{
    string ID { get; }

    public static string GetPlayerID(int index)
    {
        return "Player_" + index;
    }

    public static string GetStudentNpcID(int index)
    {
        return "StudentNpc_" + index;
    }

    public static string GetTeacherID(int index)
    {
        return "Teacher_" + index;
    }
}
