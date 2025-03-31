using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ResponseValidator.Lib;

public static class JsonPathGetter
{
    private static readonly Regex PathSegmentRegex = new(@"([^[.\]]+)(\[(.+?)\])?", RegexOptions.Compiled);
    private static readonly Regex PredicateRegex = new(@"@\.([^\s=!]+)\s*([=!]=)\s*([^)]+)", RegexOptions.Compiled);

    public static JsonNode? GetValue(this JsonNode? node, string path)
    {
        if (node is null || string.IsNullOrWhiteSpace(path))
            return null;

        var current = node;

        foreach (Match match in PathSegmentRegex.Matches(path))
        {
            var property = match.Groups[1].Value;
            var indexer = match.Groups[3].Value;

            current = current?[property];

            if (string.IsNullOrEmpty(indexer) || current is null)
                continue;

            if (PredicateRegex.IsMatch(indexer))
            {
                if (current is not JsonArray array) return null;

                var predicateMatch = PredicateRegex.Match(indexer);
                var prop = predicateMatch.Groups[1].Value;
                var op = predicateMatch.Groups[2].Value;
                var value = predicateMatch.Groups[3].Value.Trim().Trim('"');

                foreach (var item in array)
                {
                    var val = item?[prop];
                    if (val is null) continue;

                    var valStr = val.ToString();

                    // Support == and !=
                    if ((op == "==" && valStr == value) ||
                        (op == "!=" && valStr != value))
                    {
                        current = item;
                        break;
                    }
                }
            }
            else if (int.TryParse(indexer, out int i))
            {
                if (current is JsonArray array && i >= 0 && i < array.Count)
                {
                    current = array[i];
                }
                else return null;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    public static T? GetValue<T>(this JsonNode node, string path)
    {
        var valueNode = GetValue(node, path);
        return valueNode is not null ? valueNode.GetValue<T>() : default;
    }
}