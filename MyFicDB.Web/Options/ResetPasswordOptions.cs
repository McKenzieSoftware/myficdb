namespace MyFicDB.Web.Options
{
    /// <summary>
    /// Used for Reseting the users password.  Defined in docker compose with MYFICDB_RESET_PASSWORD and MYFICDB_RESET_PASSWORD_VALUE
    /// </summary>
    public sealed class ResetPasswordOptions
    {
        public bool Enabled { get; set; }
        public string? NewPassword { get; set; }
    }
}
