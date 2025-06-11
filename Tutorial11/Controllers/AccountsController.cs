using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorial11.DAL;
using Tutorial11.DTOs;
using Tutorial11.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Tutorial11.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly PasswordHasher<Account> _passwordHasher = new();
        private readonly DeviceContext _context;

        public AccountsController(DeviceContext context)
        {
            _context = context;
        }

        // GET: api/Accounts
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccount()
        {
            var accounts = await _context.Account
                .Select(a => new 
                {
                    a.Id,
                    a.Username
                })
                .ToListAsync();

            return Ok(accounts);
        }
        
        
        // GET: api/Accounts/5
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(int id)
        {
            var account = await _context.Account
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Username,
                    role = a.Role.Name
                })
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return NotFound();
            }

            return Ok(account);
        }

        // PUT: api/Accounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccount(int id, LoginAccountDto updateAccount, CancellationToken token)
        {
            try
            {
                var account = await _context.Account.FindAsync(id);
                if (account == null)
                {
                    return NotFound();
                }

                account.Username = updateAccount.Login;
                account.Password = updateAccount.Password;

                _context.Account.Update(account);
                await _context.SaveChangesAsync(token);

                return NoContent();

            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Cannot update account", instance: $"api/Accounts/{id}");
            }
        }

        // POST: api/Accounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Account>> PostAccount(CreateAccountDto newAccount)
        {
            try
            {
                var employeeExists = await _context.Employee
                    .AnyAsync(e => e.Id == newAccount.EmployeeId);

                if (!employeeExists)
                {
                    return BadRequest($"Employee with ID {newAccount.EmployeeId} does not exist.");
                }

                var roleExists = await _context.Role
                    .AnyAsync(e => e.Id == newAccount.RoleId);

                if (!roleExists)
                {
                    return BadRequest($"Role does not exist.");
                }

                //Role role = await _context.Role
                  //  .FirstOrDefaultAsync(r => r.Id == newAccount.RoleId);

                var account = new Account
                {
                    Username = newAccount.Username,
                    Password = newAccount.Password,
                    EmployeeId = newAccount.EmployeeId,
                    RoleId = newAccount.RoleId
                };

                account.Password = _passwordHasher.HashPassword(account, newAccount.Password);
                _context.Account.Add(account);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetAccount", new { id = account.Id }, account);
            }catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Cannot create account", instance: $"api/Accounts");
            }
        }

        // DELETE: api/Accounts/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Account.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            _context.Account.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AccountExists(int id)
        {
            return _context.Account.Any(e => e.Id == id);
        }
        
        [Authorize(Roles = "User")]
        [HttpGet("me")]
        public async Task<ActionResult<object>> GetMyAccount()
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (username == null)
                return Unauthorized();

            var account = await _context.Account
                .Include(a => a.Employee)
                .ThenInclude(e => e.Person)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
                return NotFound();

            return Ok(new
            {
                Username = account.Username,
                Password = account.Password,
                Salary = account.Employee.Salary,
                HireDate = account.Employee.HireDate,
                FirstName = account.Employee.Person.FirstName,
                MiddleName = account.Employee.Person.MiddleName,
                LastName = account.Employee.Person.LastName,
                PhoneNumber = account.Employee.Person.PhoneNumber,
                Email = account.Employee.Person.Email,
                PassportNumber = account.Employee.Person.PassportNumber,
            });
        }
        
        [Authorize(Roles = "User")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyAccount(UpdatePersonalInfoDto update, CancellationToken token)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (username == null)
                    return Unauthorized();


                var account = await _context.Account
                    .Include(a => a.Employee)
                    .ThenInclude(e => e.Person)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    Console.Write("Null");
                    return NotFound();
                }

                account.Username = update.Username;
                account.Password = _passwordHasher.HashPassword(account, update.Password);
                account.Employee.Person.FirstName = update.FirstName;
                account.Employee.Person.MiddleName = update.MiddleName;
                account.Employee.Person.LastName = update.LastName;
                account.Employee.Person.PhoneNumber = update.PhoneNumber;
                account.Employee.Person.Email = update.Email;
                account.Employee.Person.PassportNumber = update.PassportNumber;
                _context.Account.Update(account);
                await _context.SaveChangesAsync(token);

                return NoContent();
            }
            
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, title: "Cannot update account", instance: $"api/Accounts/me");
            }
        }
    }
}
