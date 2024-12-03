using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security;
using System.IO.Compression;

namespace SharedResources
{
    public static partial class Operations
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) =>
           new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);


        //returns the first non-empty value in the params
        public static T Coalesce<T>(params T[] options)
        {
            foreach (T option in options)
            {
                if (option != null && (!(option is string) || !string.IsNullOrWhiteSpace(option as string)))
                    return option;
            }
            return default;
        }

        public static string GetProgress(int number, int total, bool addLeadingZeros = true)
            => addLeadingZeros
            ? $"{number.ToString($"D{total.ToString().Length}")} of {total}) "
            : $"{number} of {total}) ";

        public static void PrintProgress(int number, int total, string message = "", bool addLeadingZeros = true)
            => Console.WriteLine(GetProgress(number, total, addLeadingZeros) + message);

        public static string FormatPhone(string value, string separator = "-", bool useCountryCode = false, bool wrapAreaCode = false)
        {
            if (value.IsEmpty())
                return value;

            var digits = Regex.Replace(value, @"[^0-9]", "");
            if (digits.Length > 10 && digits[0] == '1')
                digits = digits.Substring(1);

            var areaCode = digits.Substring(0, 3);
            var prefix = digits.Substring(3, 3);
            var suffix = digits.Substring(6, 4);
            var ext = digits.Length > 10 ? digits.Substring(10) : null;

            return (useCountryCode ? "+1 " : "") +
                (wrapAreaCode ? $"({areaCode}) " : $"{areaCode}{separator}") +
                $"{prefix}{separator}{suffix}" +
                (string.IsNullOrWhiteSpace(ext) ? "" : $" x{ext}");
        }

        public static string First(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrWhiteSpace(values[i]))
                    return values[i];
            return null;
        }

        public static JArray CsvToJson(byte[] file, bool removeHeaderSpacing = false, string[] stringOverrides = null)
        {
            using (var stream = new MemoryStream(file))
                return CsvToJson(stream, removeHeaderSpacing, stringOverrides);
        }

        public static JArray CsvToJson(Stream file, bool removeHeaderSpacing = false, string[] stringOverrides = null)
        {
            var records = new JArray();
            using (var parser = new TextFieldParser(file))
            {
                string[] headers = parser.ReadFields();
                if (removeHeaderSpacing)
                    headers = headers.Select(x => x.Replace(" ", "")).ToArray();

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    JObject jObj = new JObject();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var fieldValue = fields[i];
                        jObj[headers[i]] = new JValue(stringOverrides?.Contains(headers[i], StringComparer.InvariantCultureIgnoreCase) == true ? fieldValue
                            : long.TryParse(fieldValue, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out long lValue) ? lValue
                            : double.TryParse(fieldValue, out double dValue) ? dValue
                            : (object)fieldValue);
                    }
                    records.Add(jObj);
                }
                parser.Close();
            }
            return records;
        }

        #region Get CSV
        public static byte[] JsonToCSV(JArray jArray, string[] headerLocations, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false) =>
            JsonToCSV(jArray?.Cast<JObject>(), headerLocations, ignoreHeaders, encoding, excludeHeaders);

        public static byte[] JsonToCSV(IEnumerable<JObject> jObjects, string[] headerLocations, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false)
        {
            if (jObjects?.Any() != true)
                return null;

            var headers = (headerLocations ?? Array.Empty<string>())
                ?.Concat(jObjects?.SelectMany((JObject rows) => rows.Properties().Select(prop => prop.Name)) ?? Array.Empty<string>())
                ?.Distinct()
                ?.Where(h => ignoreHeaders?.Contains(h) != true)
                ?.ToArray();

            var csv = new StringBuilder();

            if (!excludeHeaders)
                csv.AppendLine(string.Join(",", headers.Select(h => FormattedValue(h))));

            foreach (var row in jObjects)
            {
                var currentRow = string.Empty;
                for (int i = 0; i < headers.Length; i++)
                    currentRow += ((i == 0 ? "" : ",") + FormattedValue((string)row?[headers[i]]));
                csv.AppendLine(currentRow);
            }
            return (encoding ?? Encoding.UTF8).GetBytes(csv.ToString());
        }


        public static byte[] ToCSV(this JArray jArray, Dictionary<int, string> headerLocations = null, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false) =>
            JsonToCSV(jArray?.Cast<JObject>(), headerLocations, ignoreHeaders, encoding, excludeHeaders);

        public static byte[] ToCSV(this IEnumerable<JObject> jArray, Dictionary<int, string> headerLocations = null, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false) =>
            JsonToCSV(jArray, headerLocations, ignoreHeaders, encoding, excludeHeaders);

        public static byte[] JsonToCSV(JArray jArray, Dictionary<int, string> headerLocations = null, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false) =>
            JsonToCSV(jArray?.Cast<JObject>(), headerLocations, ignoreHeaders, encoding, excludeHeaders);

        public static byte[] JsonToCSV(IEnumerable<JObject> jObjects, Dictionary<int, string> headerLocations = null, string[] ignoreHeaders = null, Encoding encoding = null, bool excludeHeaders = false)
        {
            if (jObjects?.Any() != true)
                return null;

            var headers = (jObjects?.SelectMany((JObject rows) => rows.Properties().Select(prop => prop.Name)) ?? Array.Empty<string>())
                ?.Distinct()
                ?.Where(h => ignoreHeaders?.Contains(h) != true)
                ?.ToArray()
                ?.CustomOrder(headerLocations);

            var csv = new StringBuilder();

            if (!excludeHeaders)
                csv.AppendLine(string.Join(",", headers.Select(h => FormattedValue(h))));

            foreach (var row in jObjects)
            {
                var currentRow = string.Empty;
                for (int i = 0; i < headers.Length; i++)
                    currentRow += ((i == 0 ? "" : ",") + FormattedValue((string)row?[headers[i]]));
                csv.AppendLine(currentRow);
            }
            return (encoding ?? Encoding.UTF8).GetBytes(csv.ToString());
        }

        private static string FormattedValue(string value)
        {
            return string.IsNullOrEmpty(value) ? ""
                : (value.Contains("\"") || value.Contains(",") || value.Contains("\n") || value.Contains("\r") || (value != value?.Trim())) ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
        #endregion

        public static double GetDistance(double lat1Rad, double lng1Rad, double lat2Rad, double lng2Rad)
        {
            double a = Math.Pow(Math.Sin((lat2Rad - lat1Rad) / 2), 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Pow(Math.Sin((lng2Rad - lng1Rad) / 2), 2);

            return ((2 * Math.Asin(Math.Sqrt(a))) * 3956); //3956 is the average radius of the earth in miles
        }
        public static double ToRadians(double deg) => deg * Math.PI / 180;

        public static string RenderFromTemplate<T>(
            string template,
            T values,
            bool removeCommentLines = false,
            bool removeMissingValueLines = false,
            bool useExecutingAssembly = false,
            bool escapeValues = false
        ) where T : class
            => Render(GetEmbeddedTemplate(template, removeCommentLines, useExecutingAssembly), values, removeMissingValueLines, escapeValues);

        public static string RenderFromTemplate<T>(
           Assembly assembly,
           string template,
           T values,
           bool removeCommentLines = false,
           bool removeMissingValueLines = false,
           bool escapeValues = false
       ) where T : class
           => Render(GetEmbeddedTemplate(assembly, template, removeCommentLines), values, removeMissingValueLines, escapeValues);

        public static string Render<T>(string html, T values, bool removeMissingValueLines = false, bool escapeValues = false) where T : class
        {
            html = ProcessHtml(html, values, escapeValues: escapeValues);
            if (removeMissingValueLines)
            {
                var missingRegex = new Regex(@"\s^.*{{.*?}}.*$", RegexOptions.Multiline);
                html = missingRegex.Replace(html, "");
            }

            return Regex.Replace(html, "{{[A-Za-z0-9._]+?}}", "");
        }

        public static string GetEmbeddedTemplate(string name, bool removeCommentLines = false, bool useExecutingAssembly = false)
            => useExecutingAssembly
            ? GetEmbeddedTemplate(Assembly.GetExecutingAssembly(), $"SharedResources.Resources.{name}", removeCommentLines)
            : GetEmbeddedTemplate(Assembly.GetEntryAssembly(), name, removeCommentLines);

        public static string GetEmbeddedTemplate(Assembly assembly, string name, bool removeCommentLines = false)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(resourceStream))
            {
                if (!removeCommentLines)
                    return reader.ReadToEnd();

                var commentRegex = new Regex(@"^\s*<!--.*?-->\s*$");
                var strBuilder = new StringBuilder();
                var line = reader.ReadLine();
                while (line != null)
                {
                    if (!string.IsNullOrWhiteSpace(line?.Trim()) && !commentRegex.IsMatch(line))
                        strBuilder.AppendLine(line);
                    line = reader.ReadLine();
                }
                return strBuilder.ToString();
            }
        }

        public static byte[] GetEmbeddedAsset(string name, Assembly assembly = null)
        {
            using (Stream resourceStream = (assembly ?? Assembly.GetCallingAssembly() ?? Assembly.GetEntryAssembly()).GetManifestResourceStream(name))
                return resourceStream.ToArray();
        }

        public static string ProcessHtml<T>(string html, T values, string propertyPrefix = null, bool escapeValues = false) where T : class
        {
            if (!html.IsEmpty() && values != null)
            {
                foreach (var property in values.GetType().GetProperties())
                {
                    var type = property.PropertyType;
                    if (type.IsClass && type != typeof(string))
                        html = ProcessHtml(html, property.GetValue(values), $"{propertyPrefix}{property.Name}.", escapeValues);
                    else
                    {
                        var propertyValue = property.GetValue(values, null)?.ToString();
                        if (propertyValue != null)
                            html = html.Replace("{{" + $"{propertyPrefix}{property.Name}" + "}}", escapeValues ? SecurityElement.Escape(propertyValue) : propertyValue);
                    }
                }
            }
            return html ?? "";
        }

        public static T ConditionalValue<T>(bool condition, T value)
            => condition ? value : default;

        public static X509Certificate2 GetCertificate(string identifier, X509FindType identifierType = X509FindType.FindByThumbprint, StoreLocation storeLocation = StoreLocation.LocalMachine)
        {
            X509Certificate2 certificate = null;
            using (var store = new X509Store(storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                certificate = store.Certificates
                    .Find(identifierType, identifier, false)
                    ?.OfType<X509Certificate2>()
                    ?.OrderByDescending(c => c.NotAfter)
                    ?.FirstOrDefault();

                store.Close();
            }
            return certificate;
        }
        public static bool TryParse<T>(string jsonStr, out T value) where T : JToken
        {
            try
            {
                value = JContainer.Parse(jsonStr) as T;
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public static JObject ParseArguments(string[] args, bool setKeysLower = true, bool setValuesLower = false)
        {
            JObject job = new JObject();

            if (args.Length <= 0)
                return job;

            string currentParam = null;

            bool lastWasKey = false;
            foreach (var arg in args)
            {
                if (arg.StartsWith("-")) //param
                {
                    if (lastWasKey)
                        job[currentParam] = true;

                    currentParam = (setKeysLower ? arg?.ToLower() : arg).TrimStart('-')?.Trim();

                    lastWasKey = true;
                }
                else //value
                {
                    lastWasKey = false;

                    if (!currentParam.IsEmpty())
                    {
                        if (long.TryParse(arg, out long longResult))
                            job.AddValue(currentParam, longResult);
                        else if (double.TryParse(arg, out double doubleResult))
                            job.AddValue(currentParam, doubleResult);
                        else if (bool.TryParse(arg, out bool boolResult))
                            job.AddValue(currentParam, boolResult);
                        else if (DateTime.TryParse(arg, out DateTime dtResult))
                            job.AddValue(currentParam, dtResult);
                        else
                            job.AddValue(currentParam, setValuesLower ? arg?.ToLower() : arg);
                    }
                }
            }

            if (lastWasKey) //last arg was a flag, set to true
                job[currentParam] = true;

            return job;
        }

        public static string RenameExistingFile(string fileName, string targetDirectory)
        {
            string incrementedFilename = fileName;
            int i = 1;
            while (File.Exists(Path.Combine(targetDirectory, incrementedFilename)))
                incrementedFilename = fileName.Insert(fileName.LastIndexOf('.'), $" ({i++})");
            return Path.Combine(targetDirectory, incrementedFilename);
        }

        public static byte[] CreateArchive(IEnumerable<(string FileName, byte[] FileData)> archiveItems)
        {
            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in archiveItems)
                    {
                        using (var fileStream = new MemoryStream(file.FileData))
                        {
                            var archiveEntry = archive.CreateEntry(file.FileName);
                            using (var entryStream = archiveEntry.Open())
                            {
                                fileStream.CopyTo(entryStream, file.FileData.Length);
                            }
                        }
                    }
                } //Must dispose zip stream before exporting because it defies all logic
                return zipStream.ToArray();
            }
        }

        public static Exception Try(this Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        public static async Task<Exception> Try(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }
    }
}
