using System;
using System.Threading;
using System.Threading.Tasks;
using sistema_gestao_recursos_humanos.backend.models;
using System.Runtime.CompilerServices;

namespace sistema_gestao_recursos_humanos.backend.services
{
    public interface IAppLogService
    {
        Task LogAsync(
            string message,
            LogType type = LogType.Information,
            Exception? ex = null,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        // Convenience methods
        Task TraceAsync(
            string message,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        Task DebugAsync(
            string message,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        Task InfoAsync(
            string message,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        Task WarnAsync(
            string message,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        Task ErrorAsync(
            string message,
            Exception? ex = null,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");

        Task CriticalAsync(
            string message,
            Exception? ex = null,
            string? action = null,
            CancellationToken ct = default,
            [CallerMemberName] string caller = "");
    }
}
