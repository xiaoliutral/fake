namespace Fake;

public class FakeException : Exception
{
    public FakeException()
    {
    }

    public FakeException(string? message)
        : base(message)
    {
    }

    public FakeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}