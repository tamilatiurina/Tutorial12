using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tutorial11.Models;

namespace Tutorial11.Helper.Middleware;

public class DeviceMiddleware
{
   private readonly RequestDelegate _next;
    private readonly ValidationConfig _validationConfig;

    public DeviceMiddleware(RequestDelegate next, ValidationConfig config)
    {
        _next = next;
        _validationConfig = config;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/Devices") ||
            context.Request.Method == HttpMethods.Options ||
            context.Request.Method == HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrEmpty(json))
        {
            await _next(context);
            return;
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var device = JsonSerializer.Deserialize<Device>(json, options);

        if (device == null || _validationConfig?.Validations == null)
        {
            await _next(context);
            return;
        }

        var matchingValidations = _validationConfig.Validations
            .Where(v => v.Type == device.Type)
            .Where(v => GetPropertyValue(device, v.PreRequestName)?.ToString().ToLower() ==
                        v.PreRequestValue.ToLower());

        foreach (var validation in matchingValidations)
        {
            foreach (var rule in validation.Rules)
            {
                if (!device.AdditionalProperties.TryGetValue(rule.ParamName, out var propValue))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Missing required property '{rule.ParamName}'");
                    return;
                }

                var stringValue = propValue?.ToString() ?? "";

                bool isValid = false;

                if (rule.Regex.ValueKind == JsonValueKind.String)
                {
                    var pattern = rule.Regex.GetString()?.Trim('/');
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        isValid = Regex.IsMatch(stringValue, pattern);
                    }
                }
                else if (rule.Regex.ValueKind == JsonValueKind.Array)
                {
                    var allowedValues = rule.Regex.EnumerateArray().Select(x => x.GetString()).ToList();
                    isValid = allowedValues.Contains(stringValue);
                }

                if (!isValid)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(
                        $"Validation failed for '{rule.ParamName}' with value '{stringValue}'");
                    return;
                }
            }
        }
        await _next(context);
    }

    private object GetPropertyValue(object obj, string propName)
    {
        return obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
    }
    
}