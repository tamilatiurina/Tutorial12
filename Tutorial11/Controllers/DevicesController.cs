using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorial11.DAL;
using Tutorial11.DTOs;
using Tutorial11.Models;

namespace Tutorial11.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceContext _context;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(DeviceContext context, ILogger<DevicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Devices
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevice()
        {
            try
            {
                var devices = await _context.Device
                    .Select(d => new { d.Id, d.Name })
                    .ToListAsync();

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: "api/devices");
            }
        }
        
        // GET: api/Devices/types
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDeviceTypes()
        {
            try
            {
                var devices = await _context.DeviceType
                    .Select(d => new { d.Id, d.Name })
                    .ToListAsync();

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: "api/devices/types");
            }
        }

        // GET: api/Devices/5 (Admin or assigned user)
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetDevice(long id, CancellationToken token)
        {
            try
            {
                var device = await _context.Device
                    .Include(d => d.DeviceType)
                    .Include(d => d.DeviceEmployees)
                        .ThenInclude(de => de.Employee)
                            .ThenInclude(e => e.Person)
                    .FirstOrDefaultAsync(d => d.Id == id, token);

                if (device == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // If not admin, check assignment
                if (userRole != "Admin")
                {
                    var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (username == null)
                        return Unauthorized("Invalid account");

                    var accountUser = await _context.Account
                        .Include(a => a.Employee)
                        .ThenInclude(e => e.Person)
                        .FirstOrDefaultAsync(a => a.Username == username);

                    if (accountUser == null)
                        return Unauthorized("Invalid account");
                    

                    bool isAssigned = device.DeviceEmployees.Any(de =>
                        de.EmployeeId == accountUser.EmployeeId && de.ReturnDate == null);

                    if (!isAssigned)
                        return Unauthorized("You are not authorized to access this device.");
                }

                var activeAssignment = device.DeviceEmployees
                    .Where(de => de.ReturnDate == null)
                    .OrderByDescending(de => de.IssueDate)
                    .FirstOrDefault();

                EmployeeDto? currentEmployee = null;

                if (activeAssignment != null)
                {
                    var emp = activeAssignment.Employee;
                    currentEmployee = new EmployeeDto(
                        emp.Id,
                        $"{emp.Person.FirstName} {emp.Person.LastName}"
                    );
                }

                var result = new DeviceDto(
                    device.Name,
                    device.IsEnabled,
                    JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(device.AdditionalProperties)),
                    device.DeviceType?.Name ?? "Unknown"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: $"api/devices/{id}");
            }
        }

        // PUT: api/Devices/5 (Admin or assigned user)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutDevice(long id, [FromBody] CreateDeviceDto deviceDto, CancellationToken token)
        {
            _logger.LogInformation("PUT /api/devices/{DeviceId} started", id);
            try
            {
                var device = await _context.Device
                    .Include(d => d.DeviceEmployees)
                    .ThenInclude(de => de.Employee)
                    .ThenInclude(e => e.Account)
                    .FirstOrDefaultAsync(d => d.Id == id, token);

                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found", id);
                    return NotFound($"Device with ID {id} not found.");
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userRole != "Admin")
                {
                    var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (username == null)
                    {
                        _logger.LogWarning("Unauthorized attempt: missing username");
                        return Unauthorized("Invalid account");
                    }

                    var accountUser = await _context.Account
                        .Include(a => a.Employee)
                        .ThenInclude(e => e.Person)
                        .FirstOrDefaultAsync(a => a.Username == username);

                    if (accountUser == null)
                    {
                        _logger.LogWarning("Unauthorized attempt: account not found for user {Username}", username);
                        return Unauthorized("Invalid account");
                    }


                    bool isAssigned = device.DeviceEmployees.Any(de =>
                        de.EmployeeId == accountUser.EmployeeId && de.ReturnDate == null);

                    if (!isAssigned)
                    {
                        _logger.LogWarning("User {Username} not authorized to access device {DeviceId}", username, id);
                        return Unauthorized("You are not authorized to access this device.");
                    }
                }

                var deviceType = await _context.DeviceType
                    .FirstOrDefaultAsync(dt => dt.Id == deviceDto.TypeId, token);

                if (deviceType == null)
                {
                    _logger.LogWarning("DeviceType '{DeviceType}' not found", deviceDto.TypeId);
                    return NotFound("DeviceType not found.");
                }

                device.Name = deviceDto.Name;
                device.IsEnabled = deviceDto.IsEnabled;
                device.AdditionalProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(deviceDto.AdditionalProperties)
                ) ?? new Dictionary<string, object>();
                device.DeviceTypeId = deviceType.Id;
                
                _context.Device.Update(device);
                await _context.SaveChangesAsync(token);
                
                _logger.LogInformation("Device {DeviceId} updated successfully", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating device {DeviceId}", id);
                return Problem(detail: ex.Message, title: "Cannot update device", instance: $"api/devices/{id}");
            }
        }

        // POST: api/Devices
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostDevice([FromBody] CreateDeviceDto deviceDto, CancellationToken token)
        {
            _logger.LogInformation("POST /api/devices started");
            try
            {
                var deviceType = await _context.DeviceType
                    .FirstOrDefaultAsync(dt => dt.Id == deviceDto.TypeId, token);

                if (deviceType == null)
                {
                    _logger.LogWarning("DeviceType '{DeviceType}' not found", deviceDto.TypeId);
                    return NotFound("DeviceType not found.");
                }

                var newDevice = new Device
                {
                    Name = deviceDto.Name,
                    IsEnabled = deviceDto.IsEnabled,
                    AdditionalProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(deviceDto.AdditionalProperties)
                    ) ?? new Dictionary<string, object>(),
                    DeviceTypeId = deviceType.Id
                };

                await _context.Device.AddAsync(newDevice, token);
                await _context.SaveChangesAsync(token);
                
                _logger.LogInformation("New device created with ID {DeviceId}", newDevice.Id);
                return CreatedAtAction(nameof(GetDevice), new { id = newDevice.Id }, newDevice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating a new device");
                return Problem(detail: ex.Message, title: "Cannot create new device", instance: "api/devices");
            }
        }


        // DELETE: api/Devices/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDevice(int id, CancellationToken token)
        {
            try
            {
                var device = await _context.Device
                    .FirstOrDefaultAsync(d => d.Id == id, token);

                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found.");
                }

                _context.Device.Remove(device);
                await _context.SaveChangesAsync(token);

                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Cannot delete device", instance: $"api/devices/{id}");
            }
        }
    }
}
