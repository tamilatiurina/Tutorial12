using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tutorial11.Helper.Middleware;

public class Rule
{
    public string ParamName { get; set; }
    
    [JsonPropertyName("regex")]
    public JsonElement Regex { get; set; }
}