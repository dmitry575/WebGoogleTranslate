﻿namespace WebGoogleTranslate.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Split in sentences
    /// </summary>
    private const string Delimitary = ".!?()-:;,";

    /// <summary>
    /// Get split text
    /// </summary>
    public static List<string> GetChunks(this string content, int maxLength)
    {
        List<string> result = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }
        if (content.Length <= maxLength)
        {
            return new List<string> { content };
        }

        int pos = 0;
        while (pos < content.Length)
        {
            // check is edge of text
            if ((content.Length - pos) < maxLength)
            {
                result.Add(content.Substring(pos));
                break;
            }

            // send end of text
            int end = pos + maxLength;
            if (end >= content.Length)
            {
                end = content.Length-1;
            }
            for (; end > pos; end--)
            {
                if (Delimitary.IndexOf(content[end]) > -1)
                {
                    // if before space
                    if (end < content.Length-1 && end != (pos + maxLength) && content[end + 1] == ' ')
                    {
                        break;
                    }
                }
            }

            // if do not find any split, set spit as space
            if (pos == end)
            {
                end = pos + maxLength;
                if (end >= content.Length)
                {
                    end = content.Length - 1;
                }
                for (; end > pos; end--)
                {
                    if (content[end] == ' ')
                        break;
                }
            }

            if (pos == end)
            {
                end = pos + maxLength;
            }
            var endIndex = end - pos + 1;
            // chec kend of string 
            if(pos + endIndex >= content.Length) 
            {
                endIndex = content.Length - pos;
            }
            result.Add(content.Substring(pos, endIndex));
            pos = end + 1;
        }

        return result;
    }
}