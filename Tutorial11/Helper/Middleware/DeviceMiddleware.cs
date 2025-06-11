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
    private readonly ILogger<DeviceMiddleware> _logger;

    public DeviceMiddleware(RequestDelegate next, ValidationConfig config, ILogger<DeviceMiddleware> logger)
    {
        _next = next;
        _validationConfig = config;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation("➡ Starting DeviceMiddleware for {Path}", context.Request.Path);

        try
        {
            if (!context.Request.Path.StartsWithSegments("/api/Devices") ||
                context.Request.Method == HttpMethods.Options ||
                context.Request.Method == HttpMethods.Get)
            {
                _logger.LogInformation("Skipping DeviceMiddleware for path {Path} and method {Method}", context.Request.Path, context.Request.Method);
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
                _logger.LogWarning("Device is null or validation config is missing.");
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
                        _logger.LogWarning("Missing required property '{Property}' for device type '{DeviceType}'", rule.ParamName, device.Type);
                        context.Response.StatusCode = 400;
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
                        _logger.LogWarning("Validation failed for '{Property}' with value '{Value}' on device type '{DeviceType}'", rule.ParamName, stringValue, device.Type);
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(
                            $"Validation failed for '{rule.ParamName}' with value '{stringValue}'");
                        return;
                    }
                }
            }

            await _next(context);
            _logger.LogInformation("DeviceMiddleware completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeviceMiddleware");
            throw;
        }
    }

    private object GetPropertyValue(object obj, string propName)
    {
        return obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
    }
    
}