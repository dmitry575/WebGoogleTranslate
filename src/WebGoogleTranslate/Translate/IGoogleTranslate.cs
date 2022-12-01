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
    TranslateResponse Translate(string text, bool isHtml);
}