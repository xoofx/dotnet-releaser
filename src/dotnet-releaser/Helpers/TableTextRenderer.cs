using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wcwidth;

namespace DotNetReleaser.Helpers;


public enum TextAlignKind {
    Left,
    Center,
    Right,
}

public class TableTextRenderer
{
    public TableTextRenderer()
    {
        ColumnHeaders = new List<(string, TextAlignKind)>();
        Rows = new List<List<string>>();
    }

    public List<(string, TextAlignKind)> ColumnHeaders { get; }

    public List<List<string>> Rows { get; }
    
    public void AddColumnHeader(string text, TextAlignKind textAlign = TextAlignKind.Left)
    {
        ColumnHeaders.Add((text, textAlign));
    }

    public void AddRow(IEnumerable<string> columns)
    {
        Rows.Add(new List<string>(columns));
    }

    public string Render()
    {
        var builder = new StringBuilder();
        var columnWidths = new List<int>();
        for (int i = 0; i < ColumnHeaders.Count; i++)
        {
            columnWidths.Add(WidthOfString(ColumnHeaders[i].Item1));
        }

        foreach (var row in Rows)
        {
            for (var i = 0; i < row.Count; i++)
            {
                var col = row[i];
                if (i == columnWidths.Count)
                {
                    columnWidths.Add(0);
                }
                columnWidths[i] = Math.Max(columnWidths[i], WidthOfString(col));
            }
        }

        // 01<-     width        ->2
        // | ---------------------- |
        AppendRow(builder, ColumnHeaders.Select(x => x.Item1).ToList(), columnWidths);
        AppendRow(builder, columnWidths.Select(x => new string('-', x)).ToList(), columnWidths, '-');
        foreach (var row in Rows)
        {
            AppendRow(builder, row, columnWidths);
        }

        return builder.ToString();
    }

    private void AppendRow(StringBuilder builder, List<string> row, List<int> columnWidths, char space = ' ')
    {
        builder.Append($"|{space}");
        for (int i = 0; i < row.Count; i++)
        {
            if (i > 0) builder.Append($"{space}|{space}");
            string? col = row[i];
            var text = AlignText(col, columnWidths[i],
                i < ColumnHeaders.Count ? ColumnHeaders[i].Item2 : TextAlignKind.Left);
            builder.Append(i + 1 == row.Count ? text.TrimEnd() : text);
        }

        builder.AppendLine();
    }

    private static string AlignText(string text, int width, TextAlignKind textAlign)
    {
        int widthOfText = WidthOfString(text);
        
        switch (textAlign)
        {
            case TextAlignKind.Left:
                return text.PadRight(width);
            case TextAlignKind.Right:
                return text.PadLeft(width);
            default:
                int leftAmount = (width - widthOfText)/2;
                return text.PadLeft(widthOfText  + leftAmount).PadRight(width);
        }
    }
    private static int WidthOfString(string text)
    {
        return text.EnumerateRunes().Select(x => UnicodeCalculator.GetWidth(x)).Sum();
    }
}