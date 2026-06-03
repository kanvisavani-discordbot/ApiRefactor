namespace ApiRefactor.Infrastructure.Auth;

public static class Roles
{
    /// <summary>Can call read endpoints (GET).</summary>
    public const string Reader = "waves.reader";

    /// <summary>Can call write endpoints (POST/PUT/PATCH). Implies reader access.</summary>
    public const string Writer = "waves.writer";
}
