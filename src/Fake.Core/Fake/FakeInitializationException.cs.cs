namespace Fake;

public class FakeInitializationException : FakeException
{
    public FakeInitializationException()
    {
    }

    public FakeInitializationException(string message)
        : base(message)
    {
    }

    public FakeInitializationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}