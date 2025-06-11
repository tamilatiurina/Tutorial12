1. Create appsettings.json as follows:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
2. Create appsettings.Development.json as follows:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DeviceDB": "Server=YOURSERVER;Database=YOURDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "http://localhost:5000",
    "Audience": "http://localhost:5000",
    "Key": "YOURKEYOF256BITS/32CHARS",
    "ValidInMinutes": 10
  }
}
```
