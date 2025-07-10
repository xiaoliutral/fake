namespace Fake.Authorization;

[Serializable]
public class FakeAuthorizationException : FakeException
{
    public FakeAuthorizationException()
    {
    }

    public FakeAuthorizationException(string message)
        : base(message)
    {
    }


    public FakeAuthorizationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}