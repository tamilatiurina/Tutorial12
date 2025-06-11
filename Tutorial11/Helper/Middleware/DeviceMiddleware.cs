using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tutorial11.DTOs;
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
            var dto = JsonSerializer.Deserialize<CreateDeviceDto>(json, options);

            if (dto == null)
            {
                 _logger.LogWarning("Deserialization of CreateDeviceDto failed.");
                await _next(context);
                return;
            }

            _logger.LogInformation("Deserialized DTO TypeId: {TypeId}", dto.TypeId);
            
            string deviceTypeName = dto.TypeId switch
            {
                1 => "PC",
                2 => "Smartwatch",
                3 => "Embedded",
                4 => "Monitor",
                5 => "Printer",
                _ => null
            };

            if (deviceTypeName == null)
            {
                _logger.LogWarning("Unknown Device TypeId: {TypeId}", dto.TypeId);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Unknown device type");
                return;
            }


            var additionalProps = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.AdditionalProperties.ToString()!);


            var matchingValidations = _validationConfig.Validations
                .Where(v => v.Type == deviceTypeName)
                .Where(v => v.PreRequestName.Equals("isEnabled", StringComparison.OrdinalIgnoreCase) &&
                    v.PreRequestValue.Equals(dto.IsEnabled.ToString(), StringComparison.OrdinalIgnoreCase));

            foreach (var validation in matchingValidations) {
                foreach (var rule in validation.Rules)
                {
                    if (!additionalProps.TryGetValue(rule.ParamName, out var propValue))
                    {
                        _logger.LogWarning("Missing required property '{Property}' for device type '{DeviceType}'",
                            rule.ParamName, deviceTypeName);
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
                        _logger.LogWarning(
                            "Validation failed for '{Property}' with value '{Value}' on device type '{DeviceType}'",
                            rule.ParamName, stringValue, deviceTypeName);
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