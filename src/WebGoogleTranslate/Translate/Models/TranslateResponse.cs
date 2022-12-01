namespace WebGoogleTranslate.Translate.Models;

/// <summary>
/// Result of request
/// </summary>
public class TranslateResponse
{
    /// <summary>
    /// Source text
    /// </summary>
    public string Text { get; set; }
    
    /// <summary>
    /// Ready translated text
    /// </summary>
    public string TextTranslated { get; set; }
}
