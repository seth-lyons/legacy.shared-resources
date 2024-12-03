using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SharedResources
{
    public static partial class Extentions
    {
        #region String

        public static bool Includes(this string value, string compareTo) => (value?.IndexOf(compareTo, StringComparison.InvariantCultureIgnoreCase) >= 0) == true;
        public static bool Is(this string value, string compareTo)
            => string.IsNullOrEmpty(value)
                ? string.IsNullOrEmpty(compareTo)
                : value.Equals(compareTo, StringComparison.InvariantCultureIgnoreCase);
        public static bool Is(this string value, object compareTo) => value?.Equals(compareTo?.ToString(), StringComparison.InvariantCultureIgnoreCase) ?? compareTo == null;
        public static bool IsEmpty(this string value) => string.IsNullOrWhiteSpace(value);
        public static string ToBase64(this string value, Encoding encoding = null) => Convert.ToBase64String((encoding ?? Encoding.UTF8).GetBytes(value));
        public static string FromBase64(this string value, Encoding encoding = null) => (encoding ?? Encoding.UTF8).GetString(Convert.FromBase64String(value));
        public static T FromJson<T>(this string value) => JsonConvert.DeserializeObject<T>(value);
        public static string Capitalize(this string value) => value?.Length > 1 && !char.IsUpper(value[0]) ? char.ToUpper(value[0]) + value.Substring(1) : value;
        public static bool IsValidEmail(this string value)
        {
            try { return new System.Net.Mail.MailAddress(value).Address == value; } catch { return false; }
        }

        public static TSelf TrimStrings<TSelf>(this TSelf input)
        {
            var stringProperties = input.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

            foreach (var stringProperty in stringProperties)
            {
                string currentValue = (string)stringProperty.GetValue(input, null);
                if (currentValue != null)
                    stringProperty.SetValue(input, currentValue.Trim(), null);
            }
            return input;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
        }

        public static T As<T>(this string value, T result) => value == null ? default : result;
        #endregion

        #region Enumerables

        public static bool IsEmpty<T>(this IEnumerable<T> values) => values?.Any() != true;

        public static IEnumerable<TSource[]> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size", "Must be greater than zero.");

            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new TSource[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count).ToArray();
        }

        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            if (values?.Any() == true)
            {
                foreach (T item in values)
                    action(item);
            }
        }

        public static void AddIfValues<T>(this List<T> list, List<T> values)
        {
            if (values.IsEmpty())
                return;
            list.AddRange(values);
        }

        public static void AddIfValues<T>(this List<T> list, IEnumerable<T> values)
        {
            if (values.IsEmpty())
                return;
            list.AddRange(values);
        }

        #endregion

        #region Objects

        public static JToken ToJson(this XmlNode value, bool omitRoot = true)
            => value == null ? null : JToken.Parse(JsonConvert.SerializeXmlNode(value, Newtonsoft.Json.Formatting.None, omitRoot));

        public static JToken ToJson(this object value, bool includeNulls = false) =>
           value == null ? null
            : JToken.Parse(value.GetType() == typeof(string)
            ? (string)value : JsonConvert.SerializeObject(value,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = includeNulls ? NullValueHandling.Include : NullValueHandling.Ignore
                }
            ));

        public static byte[] ToArray(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        #endregion
        public static bool IsInt(this double num) => Math.Abs(num % 1) <= (Double.Epsilon * 100);

        public static bool IsMatch(this string value, string pattern, out string firstCapture)
        {
            firstCapture = null;
            if (!value.IsEmpty())
            {
                var match = Regex.Match(value, pattern);
                if (match.Success)
                {
                    firstCapture = match.Groups.Count >= 2 ? match?.Groups[1]?.Value?.Trim() : null;
                    return true;
                }
            }
            return false;
        }

        public static void AddValue(this JObject jObj, string key, JToken value)
        {
            var hasValue = jObj.ContainsKey(key);

            if (!hasValue)
                jObj[key] = value;
            else if (jObj[key].Type == JTokenType.Array)
                ((JArray)jObj[key]).Add(value);
            else
            {
                var jArr = new JArray { jObj[key].DeepClone(), value };
                jObj[key] = jArr;
            }
        }

        public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp) =>
             new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);

        public static string FormattedMessage(this Exception ex, string identifier = null, string additionalInfo = null)
        {
            StackTrace trace = new StackTrace(ex, true);
            StackFrame frame = trace.GetFrames()?.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a?.GetFileName()));
            StackFrame innerException = null;
            if (ex?.InnerException != null)
                innerException = (new StackTrace(ex.InnerException, true))?.GetFrames()?.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a?.GetFileName()));

            return $"[Exception] {frame?.GetFileName()} in {frame?.GetMethod()?.Name}, line {frame?.GetFileLineNumber()}, col {frame?.GetFileColumnNumber()}.\n" +
                $"{(identifier.IsEmpty() ? "" : $"\t[Identity] {identifier}\n")}" +
                $"{(additionalInfo.IsEmpty() ? "" : $"\t[Info] {additionalInfo}\n")}" +
                $"\t[Message] {ex?.Message}" +
                $"{(ex?.InnerException != null ? $"\n\t[InnerException] {innerException?.GetFileName()} in {innerException?.GetMethod()?.Name}, line {innerException?.GetFileLineNumber()}, col {innerException?.GetFileColumnNumber()}\n\t[InnerMessage] {ex?.InnerException?.Message}" : "")}";
        }

        public static void PrintFormattedMessage(this Exception ex, string identifier = null, string additionalInfo = null)
            => Console.WriteLine(ex.FormattedMessage(identifier, additionalInfo));

        public static void EnsureSuccess(this WebRequestResponse response)
        {
            if (response.IsError)
                throw new WebException($"Response did not indicate success. {response.StatusCode}, {response.Reason} - {response.ResponseBody}");
        }

        public static XElement NestedElement(this XElement element, string elementPath, XNamespace nameSpace = null)
        {
            if (nameSpace == null)
                nameSpace = element.GetDefaultNamespace();
            var elementTrail = elementPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentElement = element;
            foreach (string elementName in elementTrail)
            {
                currentElement = currentElement?.Element($"{(nameSpace == null ? "" : $"{{{nameSpace}}}")}" + elementName);
                if (currentElement == null)
                    break;
            }
            return currentElement;
        }

        public static string[] CustomOrder(this string[] array, Dictionary<int, string> ordering)
        {
            if (array == null || ordering == null || array.Length <= 0 || ordering.Count <= 0)
                return array;

            var sorted = new string[array.Length];
            var finalIndex = array.Length - 1;

            //Set assigned values
            foreach (var spot in ordering?.Where(o => o.Key <= finalIndex)?.OrderBy(o => o.Key))
            {
                var index = Array.FindIndex(array, v => v.Is(spot.Value));
                if (index < 0) // value doesnt exist
                    continue;

                var assignedSeat = spot.Key;
                if (assignedSeat < 0)
                    throw new IndexOutOfRangeException("Location cannot be less than 0");

                if (sorted[assignedSeat] != null)
                    throw new Exception("Desired Location is not empty");

                sorted[assignedSeat] = array[index]; // keeps case
                array[index] = null;
            }

            var lastAssignment = finalIndex;
            foreach (var spot in ordering?.Where(o => o.Key > finalIndex)?.OrderByDescending(o => o.Key))
            {
                var index = Array.FindIndex(array, v => v.Is(spot.Value));
                if (index < 0) // value doesnt exist
                    continue;

                var assignedSeat = lastAssignment--;
                if (assignedSeat < 0)
                    throw new Exception("No determinable location available");

                if (sorted[assignedSeat] != null)
                    throw new Exception("Desired Location is not empty");

                sorted[assignedSeat] = array[index]; // keeps case
                array[index] = null;
            }

            //backfill with remaining values
            var seatAssignment = 0;
            foreach (var header in array.Where(h => h != null))
            {
                while (sorted[seatAssignment] != null)
                    seatAssignment++;
                sorted[seatAssignment] = header;
            }

            return sorted;
        }

        public static void SetFromPropertyPath(this JObject obj, string propertyPath, object value)
        {
            if (obj == null) obj = new JObject();
            JToken workingObject = obj;

            var propertyParts = propertyPath.Split(new[] { "--", ":" }, StringSplitOptions.RemoveEmptyEntries);
            var finalIndex = propertyParts.Length - 1;

            for (int i = 0; i <= finalIndex; i++)
            {
                if (i == finalIndex)//value
                    workingObject[propertyParts[i]] = new JValue(value);
                else //object                
                {
                    var childObject = workingObject?[propertyParts[i]];
                    if (childObject == null || childObject.Type == JTokenType.Null)
                    {
                        childObject = new JObject();
                        workingObject[propertyParts[i]] = childObject;
                    }
                    workingObject = childObject;
                }
            }
        }
    }
}
