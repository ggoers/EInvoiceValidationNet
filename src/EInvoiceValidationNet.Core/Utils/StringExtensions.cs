using System.IO;

namespace EInvoiceValidationNet.Core.Utils
{
    internal static class StringExtensions
    {
        public static string NormalizePath(this string path)
        {
            return path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        } 
    }
}