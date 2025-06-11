using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tutorial11.DAL;
using Tutorial11.Helper.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tutorial11.Helper.Middleware;
using Tutorial11.Services.Token;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<ITokenService, TokenService>();

var jwtConfigData = builder.Configuration.GetSection("Jwt");

var connectionString = builder.Configuration.GetConnectionString("DeviceDB")
                       ?? throw new Exception("DeviceDB not found");
builder.Services.AddDbContext<DeviceContext>(o => o.UseSqlServer(connectionString));

builder.Services.Configure<JwtOptions>(jwtConfigData);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = jwtConfigData["Issuer"],
                ValidAudience = jwtConfigData["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigData["Key"])),
                ClockSkew = TimeSpan.FromMinutes(10)
            };
        }
    );

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var jsonText = File.ReadAllText("validationRules.json");
Console.WriteLine("ValidationRules.json content:");
Console.WriteLine(jsonText);

var config = JsonSerializer.Deserialize<ValidationConfig>(jsonText, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

if (config == null || config.Validations == null)
{
    Console.WriteLine("Validation config or its Validations property is null");
}
else
{
    Console.WriteLine($"Loaded {config.Validations.Count} rules");
}

app.UseMiddleware<DeviceMiddleware>(config);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
