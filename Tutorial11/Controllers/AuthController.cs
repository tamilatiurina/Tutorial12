using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutorial11.DAL;
using Tutorial11.DTOs;
using Tutorial11.Models;
using Tutorial11.Services.Token;

namespace Tutorial11.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        
        private readonly DeviceContext _context;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public AuthController(DeviceContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost]
        public async Task<IActionResult> Auth(LoginAccountDto loginAccount, CancellationToken cancellationToken)
        {
            var foundUser = await _context.Account.Include(u => u.Role).FirstOrDefaultAsync(u => string.Equals(u.Username, loginAccount.Username), cancellationToken);

            if (foundUser == null)
            {
                return Unauthorized();
            }
            var verificationResult = _passwordHasher.VerifyHashedPassword(foundUser, foundUser.Password, loginAccount.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var token = new
            {
                AccessToken = _tokenService.GenerateToken(foundUser.Username, foundUser.Role.Name),
            };

            return Ok(token);
        }

    }
}
