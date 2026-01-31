using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyFicDB.Web.Options;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// <para>
    /// Start-up only maintenance service that allows the user to reset the login password.
    /// </para>
    /// <para>
    /// This service runs once at application startup, resets the user's password to the one they define, then exits out.
    /// </para>
    /// <para>
    /// For this service to work, it checks if <c>MYFICDB_RESET_PASSWORD</c> is set to <c>true</c> in ENV and then checks if the password entered in
    /// <c>MYFICDB_PASSWORD_VALUE</c> is valid.  If true and valid, the password will be reset and the service shuts down (the app doesn't).
    /// </para>
    /// </summary>
    public sealed class ResetPasswordHostedService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly IOptions<ResetPasswordOptions> _options;
        private readonly ILogger<ResetPasswordHostedService> _logger;

        public ResetPasswordHostedService(IServiceProvider services, IOptions<ResetPasswordOptions> options, ILogger<ResetPasswordHostedService> logger)
        {
            _services = services;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var enabled = _options.Value.Enabled;
            var newPassword = _options.Value.NewPassword;

            if (!enabled)
            {
                return;
            }

            _logger.LogInformation($"Password reset is enabled, beginning reset process.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                _logger.LogError("Password reset requested but MYFICDB_RESET_PASSWORD_VALUE is missing/invalid.");
                return;
            }

            try
            {
                using var scope = _services.CreateScope();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();

                // single-user system so we can safely just get the first user, as no more than one user
                // should ever exist
                var user = await userManager.Users.OrderBy(u => u.Id).FirstOrDefaultAsync(cancellationToken);

                if (user is null)
                {
                    _logger.LogError("Password reset requested but no user exists.");
                    return;
                }

                // Identity-approved reset, this is based on not knowing the existing password
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password was reset for user {UserId}. IMPORTANT: set MYFICDB_RESET_PASSWORD=false after this run.", user.Id);
                }
                else
                {
                    _logger.LogError("Password reset failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            } catch (SqliteException sqlEx)
            {
                _logger.LogCritical(sqlEx, "Unknown error with Sqlite has occurred. If this happens more than once, please open a bug report.");
                return;
            } catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unknown error has occurred.  If this happens more than once, please open a bug report.");
                return;
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
