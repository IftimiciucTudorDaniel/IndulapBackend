using System.Text.RegularExpressions;

public static class XmlUtils
{
    public static string FixInvalidAmpersands(string xmlContent)
    {
        return Regex.Replace(xmlContent, @"&(?!amp;|lt;|gt;|apos;|quot;|#[0-9]+;|#x[0-9a-fA-F]+;)", "&amp;");
    }
}
