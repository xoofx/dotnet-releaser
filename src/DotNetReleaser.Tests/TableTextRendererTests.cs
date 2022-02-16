using DotNetReleaser.Helpers;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class TableTextRendererTests
{
    [Test]
    public void TestLeftAlign()
    {
        var text = GetTableAsText(TextAlignKind.Left);
        AssertHelper.Equals(@"| Property                     | Type         | Description
|------------------------------|--------------|----------------------------
| This_is_a_long_property_name | string       | This is a long description.
| abc                          | int          | short description.
| abc_def                      | double_float | shorter.
", text);
    }

    [Test]
    public void TestRightAlign()
    {
        var text = GetTableAsText(TextAlignKind.Right);
        AssertHelper.Equals(@"|                     Property |         Type |                 Description
|------------------------------|--------------|----------------------------
| This_is_a_long_property_name |       string | This is a long description.
|                          abc |          int |          short description.
|                      abc_def | double_float |                    shorter.
", text);

    }

    [Test]
    public void TestCenterAlign()
    {
        var text = GetTableAsText(TextAlignKind.Center);
        AssertHelper.Equals(@"|           Property           |     Type     |         Description
|------------------------------|--------------|----------------------------
| This_is_a_long_property_name |    string    | This is a long description.
|             abc              |     int      |     short description.
|           abc_def            | double_float |          shorter.
", text);

    }

    private string GetTableAsText(TextAlignKind align)
    {
        var renderer = new TableTextRenderer();
        renderer.AddColumnHeader("Property", align);
        renderer.AddColumnHeader("Type", align);
        renderer.AddColumnHeader("Description", align);

        renderer.AddRow(new[] { "This_is_a_long_property_name", "string", "This is a long description." });
        renderer.AddRow(new[] { "abc", "int", "short description." });
        renderer.AddRow(new[] { "abc_def", "double_float", "shorter." });
        return renderer.Render();
    }
}