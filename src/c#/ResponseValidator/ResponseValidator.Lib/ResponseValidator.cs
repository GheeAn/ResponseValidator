using System.Text.Json.Nodes;

namespace ResponseValidator.Lib;

public static class ResponseValidator
{
    private static JsonNode? _root;
    private static JsonNode? _currentRoot;

    private static Dictionary<string, (bool?, string)> _validations = new();

    public static JsonNode FromJson(string json)
    {
        _root = JsonNode.Parse(json);
        _currentRoot = _root ?? throw new Exception("Invalid JSON, could not set root node");
        _validations = new Dictionary<string, (bool?, string)>();

        return _root;
    }
    
    public static JsonNode FromNode(JsonNode node)
    {
        _root = node;
        _currentRoot = _root;
        return _root;
    }
    
    public static JsonNode ForNode(this JsonNode node, string path)
    {
        var value = node.GetValue(path);

        _currentRoot = value ?? throw new Exception($"No node found at path: {path}");
        
        return value;
    }
    
    public static (string, T?, String, T?) Expect<T>(this JsonNode node, string path)
    {
        return ($"{node.GetPath()}.{path}", node.GetValue<T>(path), "", default);
    }
    
    public static (string, T?, String, T?) Equals<T>(this (string, T?, string, T?) validationResult, string path)
    {
        return (validationResult.Item1, validationResult.Item2, $"{_currentRoot!.GetPath()}.{path}", _currentRoot.GetValue<T>(path));
    }
    
    public static JsonNode ForEach(this JsonNode node, Action<JsonNode> action)
    {
        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is null) continue;
                _currentRoot = item;
                action(item);
            }
        }
        else if (node is JsonObject jsonObject)
        {
            foreach (var item in jsonObject)
            {
                if (item.Value is null) continue;
                _currentRoot = item.Value;
                action(item.Value);
            }
        }

        return node;
    }
    
    public static Dictionary<string, (bool?, string)> Validate<T>(this (string, T?, string, T?) validationResult)
    {
        if (validationResult.Item2 is null)
        {
            _validations.Add(validationResult.Item1, (null, "Expected value not found"));
            return _validations;
        }
        
        if (validationResult.Item4 is null)
        {
            _validations.Add(validationResult.Item3, (null, "Equal value not found"));
            return _validations;
        }
        
        var key = validationResult.Item1;
        var validation = validationResult.Item2!.Equals(validationResult.Item4);
        var message = validation ? GetPassMessage(validationResult)
             : 
            $"Expected {validationResult.Item1} ({validationResult.Item2}) is not equal to {validationResult.Item3} ({validationResult.Item4})";

        _validations[key] = (validation, message);

        return _validations;
    }
    
    public static Dictionary<string, (bool?, string)> Validate()
    {
        return _validations;
    }
    
    public static void Cleanup()
    {
        _root = null;
        _validations = new Dictionary<string, (bool?, string)>();
    }
    
    public static Dictionary<string, (bool?, string)> WithCleanup(this Dictionary<string, (bool?, string)> validations)
    {
        _validations = new Dictionary<string, (bool?, string)>();
        _root = null;

        return validations;
    }
    
    private static string GetPassMessage<T>((string, T?, string, T?) validationResult)
    {
        return $"Expected {GetOutputPath(validationResult.Item1)} ({validationResult.Item2}) is equal to {GetOutputPath(validationResult.Item3)} ({validationResult.Item4})";
    }
    
    private static string GetOutputPath(string path)
    {
        return string.Join(' ', path.Split('.')[^2..]).Replace("[", " ").Replace("]", "");
    }
}