namespace Raftel.Application.UnitTests.Abstractions;

public interface ISpy
{
    void Intercept(string message);
    string[] InterceptedMessages();
}