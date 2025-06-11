using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class EmployeesController : ControllerBase
    {
        private readonly DeviceContext _context;

        public EmployeesController(DeviceContext context)
        {
            _context = context;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShortEmployeeDto>>> GetEmployees(CancellationToken token)
        {
            try
            {
                var shortInfoEmployees = _context.Employee
                    .Select(e => new ShortEmployeeDto(
                        e.Id, 
                        $"{e.Person.FirstName} {e.Person.MiddleName} {e.Person.LastName}"
                    ));

                var result = await shortInfoEmployees.ToListAsync(token);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: "api/employees");
            }
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDetailDto>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employee
                    .Include(e => e.Person)
                    .Include(e => e.Position)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    return NotFound();
                }

                var result = new EmployeeDetailDto2
                {
                    Person = new PersonDto
                    {
                        PassportNumber = employee.Person.PassportNumber,
                        FirstName = employee.Person.FirstName,
                        MiddleName = employee.Person.MiddleName,
                        LastName = employee.Person.LastName,
                        PhoneNumber = employee.Person.PhoneNumber,
                        Email = employee.Person.Email
                    },
                    Salary = employee.Salary,
                    HireDate = employee.HireDate,
                    Position = employee.Position.Name
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Server error", instance: $"api/employees/{id}");
            }
        }
        // PUT: api/Employees/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest();
            }

            _context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Employees
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee(CreateEmployeeDto dto)
        {
            var person = new Person
            {
                PassportNumber = dto.Person.PassportNumber,
                FirstName = dto.Person.FirstName,
                MiddleName = dto.Person.MiddleName,
                LastName = dto.Person.LastName,
                PhoneNumber = dto.Person.PhoneNumber,
                Email = dto.Person.Email
            };

            var employee = new Employee
            {
                Person = person,
                Salary = dto.Salary,
                HireDate = DateTime.UtcNow,
                PositionId = dto.PositionId
            };

            _context.Employee.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmployee", new { id = employee.Id }, employee);
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employee.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee.Any(e => e.Id == id);
        }
    }
}
