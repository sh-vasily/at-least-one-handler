namespace AtLeastOneHandler;

public class Logger<T>: ILogger<T>
{
    public void Log(string message)
    {
        Console.Out.WriteLine($"{DateTime.Now}:{typeof(T)}:{message}");
    }
}