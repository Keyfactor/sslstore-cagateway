using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore
{
    /// <summary>
    ///     Masks credentials and tokens in JSON before it is written to the log.
    /// </summary>
    internal static class LogRedactor
    {
        private const string Mask = "***REDACTED***";

        private static readonly Regex SensitiveValues = new Regex(
            "(\"(?:AuthToken|PartnerCode|InvokingPartnerCode|ReplayToken|Token|TokenCode|TokenID|CustomerPassword|" +
            "KeyfactorApiPassword|Password)\"\\s*:\\s*)\"(?:[^\"\\\\]|\\\\.)*\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Serializes an object to JSON with sensitive values masked.</summary>
        public static string Json(object value)
        {
            return Redact(JsonConvert.SerializeObject(value));
        }

        /// <summary>Masks sensitive values in a JSON string.</summary>
        public static string Redact(string json)
        {
            return string.IsNullOrEmpty(json) ? json : SensitiveValues.Replace(json, "$1\"" + Mask + "\"");
        }
    }
}
