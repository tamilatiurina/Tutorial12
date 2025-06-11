using Tutorial11.Models;

namespace Tutorial11.Helper.Middleware;

public class ValidationRule
{
    public string Type { get; set; }
    public string PreRequestName { get; set; }
    public string PreRequestValue { get; set; }
    public List<Rule> Rules { get; set; }
}