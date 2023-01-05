
namespace WebGoogleTranslate.Translate.Models
{
    /// <summary>
    /// Model for request to translating
    /// </summary>
    public class TranslateRequest
    {

        /// <summary>
        /// Text for translating
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// From language translate
        /// </summary>
        public string FromLang { get; set; }

        /// <summary>
        /// To language translate
        /// </summary>
        public string ToLang { get; set; }

        /// <summary>
        /// Text is HTML or only text
        /// </summary>
        public bool IsHtml { get; set; }

        /// <summary>
        /// Need convert data before sending to Google, need for html and sometime for plan text
        /// </summary>
        public bool Convert { get; set; }
    }
}
