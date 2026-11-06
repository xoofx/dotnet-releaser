using System;
using System.Threading.Tasks;

namespace DotNetReleaser;

/// <summary>
/// Provides the NuGet API key used to push packages.
/// A static key (passed with <c>--nuget-token</c>) is returned as is, while a key obtained from NuGet trusted publishing
/// is short-lived (about one hour) and is therefore acquired lazily, when first needed, and renewed once it gets old.
/// </summary>
internal sealed class NuGetApiKeyProvider
{
    /// <summary>
    /// Age after which a key acquired through trusted publishing is renewed. NuGet keys expire after one hour,
    /// this leaves a comfortable margin for the longest single package push.
    /// </summary>
    internal static readonly TimeSpan DefaultMaxKeyAge = TimeSpan.FromMinutes(20);

    private readonly Func<Task<string?>>? _acquire;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _maxKeyAge;
    private string? _apiKey;
    private DateTimeOffset _acquiredAt;

    private NuGetApiKeyProvider(string? staticApiKey, Func<Task<string?>>? acquire, TimeSpan maxKeyAge, Func<DateTimeOffset> clock)
    {
        _apiKey = staticApiKey;
        _acquire = acquire;
        _maxKeyAge = maxKeyAge;
        _clock = clock;
    }

    /// <summary>
    /// Creates a provider returning a fixed API key that never expires.
    /// </summary>
    public static NuGetApiKeyProvider FromStaticKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        return new NuGetApiKeyProvider(apiKey, null, TimeSpan.Zero, static () => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a provider acquiring a short-lived API key on demand with <paramref name="acquire"/> (returning <c>null</c> on failure)
    /// and renewing it when it is older than <paramref name="maxKeyAge"/>.
    /// </summary>
    public static NuGetApiKeyProvider FromAcquire(Func<Task<string?>> acquire, TimeSpan maxKeyAge, Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(acquire);
        return new NuGetApiKeyProvider(null, acquire, maxKeyAge, clock ?? (static () => DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Gets a valid API key, acquiring or renewing it if needed. Returns <c>null</c> if it could not be acquired
    /// (the failure is reported by the acquire function itself).
    /// </summary>
    public async Task<string?> GetApiKeyAsync()
    {
        if (_acquire is null)
        {
            return _apiKey;
        }

        if (_apiKey is null || _clock() - _acquiredAt >= _maxKeyAge)
        {
            var apiKey = await _acquire().ConfigureAwait(false);
            if (string.IsNullOrEmpty(apiKey))
            {
                _apiKey = null;
                return null;
            }

            _apiKey = apiKey;
            _acquiredAt = _clock();
        }

        return _apiKey;
    }
}
