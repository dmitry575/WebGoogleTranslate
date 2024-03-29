﻿
using GoogleTranslate.Common;

namespace WebGoogleTranslate.Common
{
    /// <summary>
    /// Factory of converting files
    /// </summary>
    public interface IConvertFactory
    {
        IConvert Create(bool isHtml);
    }
}
