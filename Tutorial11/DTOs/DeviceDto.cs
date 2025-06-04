namespace Tutorial11.DTOs;

public record DeviceDto(string Name, string TypeName, bool IsEnabled, object AdditionalProperties, EmployeeDto? CurrentEmployee);