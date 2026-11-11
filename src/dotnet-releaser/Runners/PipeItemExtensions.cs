using System;
using XenoAtom.MsBuildPipeLogger;

namespace DotNetReleaser.Runners;

/// <summary>
/// Extension methods for <see cref="PipeItem"/>.
/// </summary>
public static class PipeItemExtensions
{
    /// <summary>
    /// Gets the value of a metadata of an item, with the semantics of MSBuild's <c>ITaskItem.GetMetadata</c>:
    /// the name is case-insensitive and a missing metadata gives an empty string.
    /// </summary>
    /// <param name="item">The item to read the metadata from.</param>
    /// <param name="name">The name of the metadata.</param>
    /// <returns>The value of the metadata, or an empty string if the item has no such metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    public static string GetMetadata(this PipeItem item, string name)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(name);

        foreach (var metadata in item.Metadata)
        {
            if (string.Equals(metadata.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return metadata.Value ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
