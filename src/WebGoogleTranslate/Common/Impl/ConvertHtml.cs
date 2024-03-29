﻿using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GoogleTranslate.Common;
using HtmlAgilityPack;
using WebGoogleTranslate.Common.Models;

namespace WebGoogleTranslate.Common.Impl;

public class ConvertHtml : IConvert
{
    private readonly List<string> _tagsNotTranslate = new List<string> { "pre", "code", "blockquote" };
    private readonly char[] _listNumbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    private readonly char[] _listSpaces = { ' ', '\r', '\n' };
    private const string PrefixTag = "11";
    private const string GroupPrefixTag = "12";
    private readonly Regex _regexSpace = new Regex("[ ]{2,}", RegexOptions.None);

    private readonly Regex _regexUrls =
        new Regex(
            @"(((http|ftp|https):\/\/)+[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:\/~\+#]*[\w\-\@?^=%&amp;\/~\+#])?)");

    private readonly Regex _regexHooks = new Regex(@"(\(|\)|#|~|\*|:|-|_|`)+");

    public ConvertResult Convert(string content)
    {
        var result = new ConvertResult();
        content = content.Replace("&#39;", "'");
        (var clean, result.Tags) = GetClean(content);

        (clean, result.Groups) = GetGroup(clean);

        clean = WebUtility.HtmlDecode(clean);
        result.Content = clean;

        return result;
    }

    /// <summary>
    /// Grouping new data
    /// </summary>
    private (string, Dictionary<int, string>) GetGroup(string clean)
    {
        var groupTags = new Dictionary<int, string>();
        clean = clean.Trim();

        // empty or space symbol
        const int EMPTY = 0;

        // current position int any tags
        const int INSERT_TAG = 1;

        // current position after close tag
        const int AFTER_TAG_EMPTY = 2;

        int status = 0;
        int cur = 0;
        int count = 0;
        int index = 0;

        for (int i = 0; i < clean.Length; i++)
        {
            switch (status)
            {
                case EMPTY:
                    if (clean[i] == '[')
                    {
                        if (clean.Substring(i + 1, 2) == PrefixTag)
                        {
                            cur = i;
                            status = INSERT_TAG;
                            count++;
                        }
                    }

                    break;

                case INSERT_TAG:
                    if (clean[i] == ']')
                    {
                        status = AFTER_TAG_EMPTY;
                        break;
                    }

                    if (!_listNumbers.Contains(clean[i]))
                    {
                        count = 0;
                        status = EMPTY;
                    }


                    break;

                case AFTER_TAG_EMPTY:
                    if (clean[i] == '[')
                    {
                        status = INSERT_TAG;
                        count++;
                        break;
                    }

                    if (!_listSpaces.Contains(clean[i]))
                    {
                        if (count > 1)
                        {
                            groupTags.Add(index++, clean.Substring(cur, i - cur));
                        }

                        count = 0;
                        status = EMPTY;
                    }

                    break;
            }
        }

        if (count > 0 && status == AFTER_TAG_EMPTY && cur > 0)
        {
            groupTags.Add(index, clean.Substring(cur, clean.Length - cur));
        }

        foreach (var key in groupTags.Keys.ToArray())
        {
            clean = clean.Replace(groupTags[key], $" [{GroupPrefixTag}{key}] \r\n");
            groupTags[key] = _regexSpace.Replace(groupTags[key], " ");
        }

        clean = clean.Replace($".[{PrefixTag}", $". [{PrefixTag}");
        clean = clean.Replace($"![{PrefixTag}", $"! [{PrefixTag}");
        clean = clean.Replace(".[12", ". [12");
        clean = clean.Replace("![12", "! [12");

        clean = _regexSpace.Replace(clean, " ");

        // need for google
        clean = clean.Replace(" ,", ",");

        return (clean, groupTags);
    }

    /// <summary>
    /// Clean html remove html tags
    /// </summary>
    /// <param name="html">Dirty html</param>
    private (string, Dictionary<int, string>) GetClean(string html)
    {
        var hap = new HtmlDocument
        {
            OptionWriteEmptyNodes = true
        };

        hap.LoadHtml(html);
        return CleanHtml(hap);
    }

    /// <summary>
    /// Clearing Html
    /// </summary>
    /// <param name="hap"></param>
    private (string, Dictionary<int, string>) CleanHtml(HtmlDocument hap, int index = 0)
    {
        var htmlTags = new Dictionary<int, string>();
        foreach (var tagName in _tagsNotTranslate)
        {
            var collections = hap.DocumentNode.SelectNodes($"//{tagName}|./text()");
            if (collections != null)
            {
                var nodesQueue = new Queue<HtmlNode>(collections);
                CleanNotTranslateTags(nodesQueue, htmlTags, ref index);
            }
        }

        var nodeCollections = hap.DocumentNode.SelectNodes("./*|./text()");
        if (nodeCollections != null)
        {
            var nodes = new Queue<HtmlNode>(nodeCollections);
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (node.Name != "#text")
                {
                    htmlTags.Add(index, GetTagAttributes(node));
                    var newNode = HtmlNode.CreateNode($" [{PrefixTag}{index}] ");
                    index++;

                    parentNode.InsertBefore(newNode, node);

                    // closing tag
                    if (!HtmlNode.IsEmptyElement(node.Name))
                    {
                        htmlTags.Add(index, GetTagClose(node));
                        var newNodeClose = HtmlNode.CreateNode($" [{PrefixTag}{index}] ");
                        index++;
                        parentNode.InsertAfter(newNodeClose, node);
                    }

                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);
                }
            }
        }

        var content = hap.DocumentNode.InnerHtml;
        content = Replaces(content, htmlTags, index);
        return (content, htmlTags);
    }

    private void CleanNotTranslateTags(Queue<HtmlNode> nodes, Dictionary<int, string> htmlTags, ref int index)
    {
        while (nodes.Count > 0)
        {
            var node = nodes.Dequeue();

            var parentNode = node.ParentNode;
            if (_tagsNotTranslate.Contains(node.Name) && node.Name != "#text")
            {
                htmlTags.Add(index, node.OuterHtml);
                var child = HtmlNode.CreateNode($" [{PrefixTag}{index}] ");
                index++;

                parentNode.InsertBefore(child, node);
                parentNode.RemoveChild(node);
            }
        }
    }

    /// <summary>
    /// Different replaces
    /// </summary>
    /// <param name="content">Content</param>
    /// <param name="htmlTags">Tags</param>
    /// <param name="index">Current index fro tags</param>
    private string Replaces(string content, Dictionary<int, string> htmlTags, int index)
    {
        content = _regexUrls.Replace(content, delegate(Match m)
        {
            htmlTags.Add(++index, m.Value);
            return $" [{PrefixTag}{index}] ";
        });

        content = _regexHooks.Replace(content, delegate(Match m)
        {
            htmlTags.Add(++index, m.Value);
            return $" [{PrefixTag}{index}] ";
        });
        return content;
    }


    private string GetTagAttributes(HtmlNode node)
    {
        var sb = new StringBuilder();
        sb.Append("<");
        sb.Append(node.Name);
        foreach (var attribute in node.Attributes)
        {
            string str1 = attribute.QuoteType == AttributeValueQuote.DoubleQuote ? "\"" : "'";
            sb.Append(" " + attribute.OriginalName + "=" + str1 + HtmlDocument.HtmlEncode(attribute.Value) + str1);
        }

        if (HtmlNode.IsEmptyElement(node.Name))
        {
            sb.Append(" /");
        }

        sb.Append(">");

        return sb.ToString();
    }

    /// <summary>
    /// Get close tag
    /// </summary>
    private string GetTagClose(HtmlNode node)
    {
        return string.Format("</" + node.OriginalName + ">");
    }

    public string UnConvert(string dirtyContent, Dictionary<int, string> groups, Dictionary<int, string> tags)
    {
        var html = GetUnClean(
            GetUnGroup(
                GetAfterTranslate(dirtyContent),
                groups),
            tags);
        html = html.Replace("<p> ", "<p>");
        html = html.Replace("<\\p> ", "<\\p>");
        html = html.Replace(" ,", ",");
        html = html.Replace(" .", ".");
        return html;
    }

    /// <summary>
    /// Collecting tags
    /// </summary>
    /// <param name="translate">Text</param>
    private string GetAfterTranslate(string translate)
    {
        string clean = translate;
        Regex regex = new Regex(@"\[\s*(" + PrefixTag + @"[0-9]+)\s*\]");
        if (regex.IsMatch(clean))
        {
            clean = regex.Replace(clean, "[$1]");
        }

        Regex regexGroup = new Regex(@"\[\s*(" + GroupPrefixTag + @"[0-9]+)\s*\]");
        if (regexGroup.IsMatch(clean))
        {
            clean = regexGroup.Replace(clean, "[$1]");
        }

        return clean;
    }

    private string GetUnGroup(string translate, Dictionary<int, string> tagsGroups)
    {
        if (tagsGroups == null) return translate;

        var result = translate;
        foreach (var tagsGroup in tagsGroups)
        {
            var key = $"{GroupPrefixTag}{tagsGroup.Key}";

            Regex regexTag = new Regex(@"\[\s*(" + key + @")\s*\]");
            if (regexTag.IsMatch(result))
            {
                result = regexTag.Replace(result, "[$1]");
            }

            if (!result.Contains("[" + key + "]"))
            {
                throw new ConvertException($"not found key [{key}] for {tagsGroup.Value}");
            }

            if (tagsGroup.Value.StartsWith("</"))
            {
                result = result.Replace(" [" + key + "]", tagsGroup.Value);
            }
            else
            {
                result = result.Replace("[" + key + "] ", tagsGroup.Value);
            }

            result = result.Replace("[" + key + "]", tagsGroup.Value);
        }

        return result;
    }

    private string GetUnClean(string translate, Dictionary<int, string> tags)
    {
        var result = translate;
        foreach (var tag in tags)
        {
            var key = $"{PrefixTag}{tag.Key}";

            Regex regexTag = new Regex(@"\[\s*(" + key + @")\s*\]");
            if (regexTag.IsMatch(result))
            {
                result = regexTag.Replace(result, "[$1]");
            }


            if (!result.Contains("[" + key + "]"))
            {
                throw new ConvertException($"not found key {key} for {tag.Value}");
            }

            result = result.Replace("[" + key + "] ", tag.Value);
            result = result.Replace("[" + key + "]", tag.Value);
        }

        return result;
    }
}
