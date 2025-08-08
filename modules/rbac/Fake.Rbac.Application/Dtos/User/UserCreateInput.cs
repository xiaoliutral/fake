namespace Fake.Rbac.Application.Dtos.User;

public class UserCreateInput
{
    /// <summary>
    /// The username for the new user.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The password for the new user.
    /// </summary>
    public required string Password { get; set; }
}