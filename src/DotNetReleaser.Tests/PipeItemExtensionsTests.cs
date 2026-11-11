using System;
using DotNetReleaser.Runners;
using XenoAtom.MsBuildPipeLogger;

namespace DotNetReleaser.Tests;

public class PipeItemExtensionsTests
{
    [Test]
    public void GetMetadataIsCaseInsensitive()
    {
        var item = new PipeItem(string.Empty, "MyPackage", [new PipeProperty("Kind", "PackageId")]);

        Assert.AreEqual("PackageId", item.GetMetadata("Kind"));
        Assert.AreEqual("PackageId", item.GetMetadata("kind"));
    }

    [Test]
    public void GetMetadataReturnsEmptyStringWhenMissingOrNull()
    {
        var item = new PipeItem(string.Empty, "MyProject.csproj", [new PipeProperty("TargetFrameworks", null)]);

        Assert.AreEqual(string.Empty, item.GetMetadata("TargetFramework"));
        Assert.AreEqual(string.Empty, item.GetMetadata("TargetFrameworks"));
    }

    [Test]
    public void GetMetadataThrowsOnNullArguments()
    {
        var item = new PipeItem(string.Empty, "MyProject.csproj", []);

        Assert.Throws<ArgumentNullException>(() => PipeItemExtensions.GetMetadata(null!, "Kind"));
        Assert.Throws<ArgumentNullException>(() => item.GetMetadata(null!));
    }
}
