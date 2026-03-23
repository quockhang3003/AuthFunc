using DataAccess.Interfaces;
using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service.Implements;

public class TokenCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(PermissionConstants.TOKEN_CLEANUP_INTERVAL_MINUTES);

        public TokenCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<TokenCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token cleanup background service started with interval: {Interval} minutes", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    
                    var blacklistedTokenRepository = scope.ServiceProvider.GetRequiredService<IBlacklistedTokenRepository>();
                    var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                    var userSessionRepository = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
                    
                    // Cleanup expired blacklisted tokens
                    var expiredTokensCount = await blacklistedTokenRepository.CleanupExpiredTokensAsync();
                    if (expiredTokensCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokensCount);
                    }
                    
                    // Cleanup expired refresh tokens
                    var expiredRefreshTokensCount = await refreshTokenRepository.CleanupExpiredTokensAsync();
                    if (expiredRefreshTokensCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredRefreshTokensCount);
                    }
                    
                    // Cleanup inactive user sessions (inactive for more than 7 days)
                    var inactiveSessionsCount = await userSessionRepository.CleanupInactiveSessionsAsync(TimeSpan.FromDays(7));
                    if (inactiveSessionsCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} inactive user sessions", inactiveSessionsCount);
                    }
                    
                    if (expiredTokensCount > 0 || expiredRefreshTokensCount > 0 || inactiveSessionsCount > 0)
                    {
                        _logger.LogInformation("Token cleanup completed successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during token cleanup");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }

            _logger.LogInformation("Token cleanup background service stopped");
        }
    }