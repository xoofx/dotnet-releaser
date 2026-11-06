using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class NuGetApiKeyProviderTests
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(20);

    [Test]
    public async Task StaticKeyIsNeverRenewed()
    {
        var provider = NuGetApiKeyProvider.FromStaticKey("static-key");

        Assert.AreEqual("static-key", await provider.GetApiKeyAsync());
        Assert.AreEqual("static-key", await provider.GetApiKeyAsync());
    }

    [Test]
    public async Task TrustedPublishingKeyIsAcquiredLazilyAndReusedWhileFresh()
    {
        var now = DateTimeOffset.UtcNow;
        var count = 0;
        var provider = NuGetApiKeyProvider.FromAcquire(() => Task.FromResult<string?>($"key-{++count}"), MaxAge, () => now);

        Assert.AreEqual(0, count, "The key must not be acquired before it is needed");
        Assert.AreEqual("key-1", await provider.GetApiKeyAsync());

        now += MaxAge - TimeSpan.FromSeconds(1);
        Assert.AreEqual("key-1", await provider.GetApiKeyAsync());
        Assert.AreEqual(1, count);
    }

    [Test]
    public async Task TrustedPublishingKeyIsRenewedWhenOld()
    {
        var now = DateTimeOffset.UtcNow;
        var count = 0;
        var provider = NuGetApiKeyProvider.FromAcquire(() => Task.FromResult<string?>($"key-{++count}"), MaxAge, () => now);

        Assert.AreEqual("key-1", await provider.GetApiKeyAsync());

        // A publication taking more than one hour must never push with the key acquired at the beginning
        now += TimeSpan.FromHours(1);
        Assert.AreEqual("key-2", await provider.GetApiKeyAsync());
        Assert.AreEqual("key-2", await provider.GetApiKeyAsync());
        Assert.AreEqual(2, count);

        now += MaxAge;
        Assert.AreEqual("key-3", await provider.GetApiKeyAsync());
    }

    [Test]
    public async Task FailedAcquisitionReturnsNullAndIsRetried()
    {
        var count = 0;
        var provider = NuGetApiKeyProvider.FromAcquire(() => Task.FromResult<string?>(++count == 1 ? null : "key"), MaxAge);

        Assert.IsNull(await provider.GetApiKeyAsync());
        Assert.AreEqual("key", await provider.GetApiKeyAsync());
    }

    [Test]
    public async Task FailedRenewalDoesNotReturnExpiredKey()
    {
        var now = DateTimeOffset.UtcNow;
        var count = 0;
        var provider = NuGetApiKeyProvider.FromAcquire(() => Task.FromResult<string?>(++count == 1 ? "key-1" : null), MaxAge, () => now);

        Assert.AreEqual("key-1", await provider.GetApiKeyAsync());
        now += TimeSpan.FromHours(1);
        Assert.IsNull(await provider.GetApiKeyAsync());
    }
}
