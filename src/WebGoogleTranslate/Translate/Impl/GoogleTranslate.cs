using System.Text;
using GoogleTranslate.Translate;
using WebGoogleTranslate.Common;
using WebGoogleTranslate.Config;
using WebGoogleTranslate.Extensions;
using WebGoogleTranslate.Translate.Models;

namespace WebGoogleTranslate.Translate.Impl;

/// <summary>
/// Translate files via google translate
/// </summary>
public class GoogleTranslate : IGoogleTranslate
{
    /// <summary>
    /// Max length of text witch can to send to google translate
    /// </summary>
    private const int MaxLengthChunk = 2000;

    private readonly ILogger<GoogleTranslate> _logger;

    /// <summary>
    /// Helping for work with converting text, if it's html text or plan text
    /// </summary>
    private readonly IConvertFactory _convertFactory;

    /// <summary>
    /// Sending request to google translate
    /// </summary>
    private readonly IGoogleTranslateRequest _translate;
    public GoogleTranslate(IConvertFactory convertFactory, IGoogleTranslateRequest translate, ILogger<GoogleTranslate> logger)
    {
        _convertFactory = convertFactory;
        _translate = translate;
        _logger = logger;
    }


    public async Task<TranslateResponse> Translate(string text, string fromLang, string toLang, bool isHtml, bool convert)
    {
        var convertService = _convertFactory.Create(isHtml);
        var convertResult = convertService.Convert(text);

        var contentTranslate = convertResult.Content;

        var translatedContent = await GetTranslateAsync(contentTranslate, fromLang, toLang);
        try
        {
            translatedContent = convertService.UnConvert(translatedContent, convertResult.Groups, convertResult.Tags);
        }

        catch (Exception)
        {
            _logger.LogError($"unconverting {contentTranslate}\r\n\r\ntranslate: {translatedContent}\r\n\r\n");
            throw;
        }

        return new TranslateResponse
        {
            Text = text,
            TextTranslated = translatedContent
        };

    }


    /// <summary>
    /// Translating text
    /// </summary>
    private async Task<string> GetTranslateAsync(string contentTranslate, string fromLang, string toLang)
    {
        var sb = new StringBuilder();

        foreach (var chunk in contentTranslate.GetChunks(MaxLengthChunk))
        {
            var translateText = await _translate.TranslateAsync(chunk, fromLang, toLang);
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(translateText);
        }

        return sb.ToString();
    }

}