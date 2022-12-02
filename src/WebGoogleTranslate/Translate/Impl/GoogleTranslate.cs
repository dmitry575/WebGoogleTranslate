using System.Configuration;
using System.Text;
using GoogleTranslate.Common;
using GoogleTranslate.Translate;
using WebGoogleTranslate.Common;
using WebGoogleTranslate.Common.Models;
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

    /// <summary>
    /// How many times need to split text
    /// </summary>
    private const int SplitTextTimes = 10;

    /// <summary>
    /// If after translated html content get exception, try translate again but MaxLengthChunk divided by 2 
    /// </summary>
    private const int MaxLevel = 10;

    private readonly ILogger<GoogleTranslate> _logger;

    /// <summary>
    /// Configuration of translation
    /// </summary>
    private readonly Configuration _config;

    /// <summary>
    /// Helping for work with converting text, if it's html text or plan text
    /// </summary>
    private readonly IConvertFactory _convertorFactory;

    /// <summary>
    /// Sending request to google translate
    /// </summary>
    private readonly IGoogleTranslateRequest _translate;

    public GoogleTranslate(Configuration config, IConvertFactory convertFactory, IGoogleTranslateRequest translate,
        ILogger<GoogleTranslate> logger)
    {
        _config = config;
        _convertorFactory = convertFactory;
        _translate = translate;
        _logger = logger;
    }

    public async Task<TranslateResponse> Translate(string text, string fromLang, string toLang, bool isHtml,
        bool convert)
    {
        var convertService = convert ? _convertorFactory.Create(isHtml) : null;

        var translatedContent = await GetTranslateAsync(text, fromLang, toLang, convertService);

        return new TranslateResponse { Text = text, TextTranslated = translatedContent };
    }


    /// <summary>
    /// Translating text
    /// </summary>
    private async Task<string> GetTranslateAsync(string text, string fromLang, string toLang, IConvert convert,
        int maxChunkLength = MaxLengthChunk, int level = 1)
    {
        var sb = new StringBuilder();
        var contentTranslate = text;
        ConvertResult convertResult = null;
        if (convert != null)
        {
            convertResult = convert.Convert(text);
            contentTranslate = convertResult.Content;
        }

        foreach (var chunk in contentTranslate.GetChunks(maxChunkLength))
        {
            var translateText = await _translate.TranslateAsync(chunk, fromLang, toLang);
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(translateText);
        }

        var translatedContent = sb.ToString();
        // if need unconvert data
        if (convert != null)
        {
            try
            {
                translatedContent = convert.UnConvert(sb.ToString(), convertResult?.Groups, convertResult?.Tags);
            }
            catch (ConvertException e)
            {
                _logger.LogError(
                    $"get translated text failed, current max chunk: {maxChunkLength}, level:{level} : {e}");
                if (level > MaxLevel)
                {
                    // throw exception to another handler of exception
                    _logger.LogError($"get translated text failed, too many attempts");
                    throw;
                }

                return await GetTranslateAsync(contentTranslate, fromLang, toLang, convert,
                    maxChunkLength / SplitTextTimes,
                    level + 1);
            }
            catch (Exception)
            {
                _logger.LogError($"unconvert data failed: {contentTranslate}\r\n\r\ntranslate: {sb}\r\n\r\n");
                throw;
            }
        }
        _logger.LogError($"convertHtml: {contentTranslate}\r\n\r\ntranslate: {sb}\r\n\r\n");

        return translatedContent;
    }
}
