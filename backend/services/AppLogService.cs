using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;

namespace sistema_gestao_recursos_humanos.backend.services
{
    // One simple, scoped service
    public sealed class AppLogService : IAppLogService
    {
        //private readonly AdventureWorksContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AppLogService> _logger;
        private readonly IDbContextFactory<AdventureWorksContext> _factory;

        public AppLogService(
            //AdventureWorksContext db,
            IHttpContextAccessor http,
            ILogger<AppLogService> logger,
            IDbContextFactory<AdventureWorksContext> factory)
        {
            //_db = db;
            _http = http;
            _logger = logger;
            _factory = factory;
        }

        public async Task LogAsync(
            string message,
            LogType type = LogType.Information,
            Exception? ex = null,
            string? action = null,
            CancellationToken ct = default,
            string? caller = "")
        {
            int? beid = null;
            var claim = _http.HttpContext?.User?.FindFirst("BusinessEntityID")?.Value;
            if (int.TryParse(claim, out var id))
                beid = id;

            var actionName = string.IsNullOrWhiteSpace(action) ? caller : action;

            var db = _factory.CreateDbContext();

            var log = new Log
            {
                Message = message,
                BusinessEntityID = beid,
                Type = type,
                Action = actionName ?? string.Empty,
                // ex.ToString() contains type, message, stack, and inners—more useful than StackTrace alone
                StackTrace = ex?.ToString(),
                Date = DateTime.UtcNow
            };

            try
            {
                db.Logs.Add(log);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception persistEx)
            {
                // Never break the main flow because logging failed
                _logger.LogError(persistEx,
                    "Failed to persist application log. Original message: {Message}",
                    message);
            }
        }

        public Task TraceAsync(string message, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Trace, null, action, ct, caller);

        public Task DebugAsync(string message, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Debug, null, action, ct, caller);


        public Task InfoAsync(string message, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Information, null, action, ct, caller);

        public Task WarnAsync(string message, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Warning, null, action, ct, caller);

        public Task ErrorAsync(string message, Exception? ex = null, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Error, ex, action, ct, caller);

        public Task CriticalAsync(string message, Exception? ex = null, string? action = null, CancellationToken ct = default, [CallerMemberName] string caller = "")
            => LogAsync(message, LogType.Critical, ex, action, ct, caller);
    }
}
