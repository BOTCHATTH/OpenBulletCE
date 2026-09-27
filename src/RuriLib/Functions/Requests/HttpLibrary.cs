namespace RuriLib.Functions.Requests
{
    /// <summary>
    /// Which HTTP engine the REQUEST block uses (OB2 parity).
    /// </summary>
    public enum HttpLibrary
    {
        /// <summary>The built-in .NET <c>HttpClient</c> stack.</summary>
        SystemNet,

        /// <summary>The custom Extreme.Net stack (default, original OB behaviour).</summary>
        RuriLibHttp,

        /// <summary>libcurl-impersonate — real browser TLS/JA3 + HTTP/2 fingerprints.</summary>
        CurlImpersonate
    }
}
