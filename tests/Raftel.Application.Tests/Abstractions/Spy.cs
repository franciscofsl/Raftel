namespace Raftel.Application.Tests.Abstractions;

public class Spy : ISpy
{
    private readonly List<string> _interceptedMessages = new();

    public void Intercept(string message)
    {
        _interceptedMessages.Add(message);
    }

    public string[] InterceptedMessages() => _interceptedMessages.ToArray();
}