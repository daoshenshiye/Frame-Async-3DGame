namespace ClientSocket.Tools;

public static class TimesCounter
{
    public static int count = 0;

    public static void Increment()
    {
        count++;
    }

    public static void Reset()
    {
        count = 0;
    }

    public static void GetCounterAndPrint()
    {
        Console.WriteLine(count);   
    }
}