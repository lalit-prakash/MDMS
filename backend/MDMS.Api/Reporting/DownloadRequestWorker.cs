using System.Net.Http.Headers;
using MDMS.Application.Common;
using MDMS.Application.Security;
using MDMS.Domain.Enums;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

namespace MDMS.Api.Reporting;

/// <summary>
/// Background worker for Download Requests: picks up Pending requests one at a time, replays the
/// stored list-endpoint path with export=csv against this same API as the requesting user (a
/// short-lived token minted for that user, so every existing filter, permission and column rule
/// applies exactly as on screen), and stores the resulting CSV on the request. A request left in
/// Processing by a crash or restart is marked Failed on startup rather than silently hanging.
/// </summary>
public class DownloadRequestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IServer _server;
    private readonly JwtTokenService _tokens;
    private readonly ILogger<DownloadRequestWorker> _log;

    public DownloadRequestWorker(IServiceScopeFactory scopes, IServer server, JwtTokenService tokens, ILogger<DownloadRequestWorker> log)
    {
        _scopes = scopes;
        _server = server;
        _tokens = tokens;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); // let Kestrel bind first
            await MarkInterruptedAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await ProcessNextAsync(stoppingToken))
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Download request worker loop error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private async Task MarkInterruptedAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMdmsDbContext>();
        var stuck = await db.DownloadRequests.Where(r => r.Status == DownloadRequestStatus.Processing).ToListAsync(ct);
        foreach (var r in stuck) r.Fail("Interrupted by a server restart. Please request it again.");
        if (stuck.Count > 0) await db.SaveChangesAsync(ct);
    }

    private string? BaseUrl()
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.FirstOrDefault(a => a.StartsWith("http://")) ?? addresses?.FirstOrDefault();
        return address?.Replace("[::]", "localhost").Replace("0.0.0.0", "localhost").Replace("+", "localhost").Replace("*", "localhost").TrimEnd('/');
    }

    /// <returns>true if a request was processed, so the caller polls again immediately.</returns>
    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMdmsDbContext>();

        var request = await db.DownloadRequests.Where(r => r.Status == DownloadRequestStatus.Pending)
            .OrderBy(r => r.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (request is null) return false;

        request.MarkProcessing();
        await db.SaveChangesAsync(ct);

        try
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
                ?? throw new InvalidOperationException("Requesting user no longer exists.");
            var baseUrl = BaseUrl() ?? throw new InvalidOperationException("Cannot determine this server's address.");

            using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.CreateAccessToken(user).Value);

            var path = request.RequestPath;
            var url = $"{path}{(path.Contains('?') ? "&" : "?")}export=csv";
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Export failed ({(int)response.StatusCode}).");

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var rows = Math.Max(0, System.Text.Encoding.UTF8.GetString(bytes).Count(c => c == '\n') - 1);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"MDMS_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            request.Complete(fileName, bytes, rows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "Download request {Id} failed", request.Id);
            request.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
