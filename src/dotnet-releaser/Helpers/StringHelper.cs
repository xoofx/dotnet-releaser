namespace DotNetReleaser.Helpers;

internal static class StringHelper
{
    public static int CountNewLines(this string text)
    {
        int count = 0;
        int index = 0;
        while(true)
        {
            index = text.IndexOf('\n', index);
            if (index > 0)
            {
                count++;
                index++;
            }
            else
            {
                break;
            }
        }

        if (!text.EndsWith('\n'))
        {
            count++;
        }

        return count;
    }
}