using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

using NS = SharedResources;
namespace SharedResources
{
    public class PipelineRequest
    {
        public PipelineRequest() { }
        public PipelineRequest(Filter filter, IEnumerable<string> fields, Sort sort) => Initialize(filter, fields, new[] { sort });
        public PipelineRequest(Filter filter, IEnumerable<string> fields, IEnumerable<Sort> sort = null) => Initialize(filter, fields, sort);

        public void Initialize(Filter filter, IEnumerable<string> fields, IEnumerable<Sort> sort = null)
        {
            Filter = filter;
            Fields = fields;
            SortBy = sort;
        }

        public override string ToString() => RenderJson();

        public string RenderJson() =>
            JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            });

        [JsonProperty("filter")]
        public Filter Filter { get; set; }

        [JsonProperty("fields")]
        public IEnumerable<string> Fields { get; set; }

        [JsonProperty("sortOrder")]
        public IEnumerable<Sort> SortBy { get; set; }
    }

    public class Filter
    {
        public Filter() { }

        public Filter(Operator @operator = NS::Operator.And, params Filter[] terms) => Initialize(terms: terms, @operator: @operator);
        public Filter(IEnumerable<Filter> terms, Operator @operator = NS::Operator.And) => Initialize(terms: terms, @operator: @operator);
        public Filter(Operator @operator, IEnumerable<Filter> terms) => Initialize(terms: terms, @operator: @operator);

        public Filter(string name, string value, MatchType matchType = NS::MatchType.Exact)
            => Initialize(name: name, value: value, matchType: matchType);
        public Filter(string name, string value, Precision precision, MatchType matchType = NS::MatchType.Exact)
           => Initialize(name: name, value: value, precision: precision, matchType: matchType);
        public Filter(string name, string value, bool include, MatchType matchType = NS::MatchType.Exact)
            => Initialize(name: name, value: value, include: include, matchType: matchType);
        public Filter(string name, string value, Precision precision, bool include, MatchType matchType = NS::MatchType.Exact)
            => Initialize(name: name, value: value, precision: precision, include: include, matchType: matchType);

        public void Initialize(
            IEnumerable<Filter> terms = null,
            Operator? @operator = null,
            string name = null,
            string value = null,
            MatchType? matchType = null,
            Precision? precision = null,
            bool? include = null)
        {
            Terms = terms;
            Operator = @operator;
            Name = name;
            Value = value;
            MatchType = matchType;
            Precision = precision;
            Include = include;
        }

        [JsonProperty("operator"), JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public Operator? Operator { get; set; }

        [JsonProperty("terms")]
        public IEnumerable<Filter> Terms { get; set; }


        [JsonProperty("canonicalName")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("matchType"), JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public MatchType? MatchType { get; set; }

        [JsonProperty("precision"), JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public Precision? Precision { get; set; }

        [JsonProperty("include")]
        public bool? Include { get; set; }
    }

    public class Sort
    {
        public Sort(string name, bool descending = false)
        {
            Name = name;
            Direction = descending ? "desc" : "asc";
        }

        [JsonProperty("canonicalName")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public string Direction { get; set; }
    }

    public enum Operator
    {
        And,
        Or
    }

    public enum MatchType
    {
        Contains,
        Equals, // EQUALS FOR DATE FIELDs, USE EXACT FOR STRING FIELDS: MIGHT NEED EXACT WITH PERCISION
        Exact,
        GreaterThan,
        GreaterThanOrEquals,
        IsEmpty,
        IsNotEmpty,
        LessThan,
        LessThanOrEquals,
        NotEquals,
        StartsWith,
    }

    public enum Precision
    {
        Exact,
        Day,
        Month,
        Year,
        Recurring
    }
}
