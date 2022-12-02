using WebGoogleTranslate.Translate.Models;

namespace WebGoogleTranslate.Translate;

/// <summary>
/// Main interface for translating through Google Translate 
/// </summary>
public interface IGoogleTranslate
{
    /// <summary>
    /// Translate
    /// </summary>
    Task<TranslateResponse> Translate(string text, string fromLang, string toLang, bool isHtml, bool convert);
}