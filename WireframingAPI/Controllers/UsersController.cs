using Microsoft.AspNetCore.Mvc;
using WireframingAPI.Data;
using WireframingAPI.Models;
using System.Threading.Tasks;
using WireframingAPI.Data.EntityFrameworkProject.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace WireframingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private static ConcurrentDictionary<string, string> EmailVerificationCodes = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> SmsVerificationCodes = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, User> TempUserStorage = new ConcurrentDictionary<string, User>();


        public UsersController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;

        }

        [HttpGet("check/{icNumber}")]
        public async Task<ActionResult<bool>> CheckUserMigrationStatus(string icNumber)
        {
            var user = await _context.Users
                .Where(u => u.ICNumber == icNumber)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                if (!user.Migration)
                {
                    user.Migration = true;
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                return Ok(user.Migration);
            }
            else
            {
                var postUserResponse = await _httpClient.PostAsJsonAsync("https://localhost:7020/api/Users", new { ICNumber = icNumber });

                if (!postUserResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)postUserResponse.StatusCode, "Failed to create user.");
                }

                var createdUser = await postUserResponse.Content.ReadFromJsonAsync<User>();

                if (createdUser == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "User creation response is invalid.");
                }
                return Ok(createdUser.Migration);
            }
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userExists = await _context.Users.AnyAsync(u => u.ICNumber == user.ICNumber && u.Name == user.Name && u.PhoneNumber == user.PhoneNumber && u.Email == user.Email);
            if (userExists)
            {
                return Conflict(new { Message = "User with these details already exists. Please log in." });
            }
            var verificationCode = new Random().Next(1000, 9999).ToString();
            EmailVerificationCodes[user.ICNumber] = verificationCode;
            SmsVerificationCodes[user.ICNumber] = verificationCode;

            await SendEmailAsync(user.Email, "Your Verification Code", $"Your verification code is: {verificationCode}");
            SimulateSmsSending(user.PhoneNumber, $"Your verification code is: {verificationCode}");
            TempUserStorage[user.ICNumber] = user;

            return Ok(new { Message = "Verification code sent to email and phone. Please verify to complete registration." });
        }


        [HttpPut("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] VerificationRequest verificationRequest)
        {
            if (EmailVerificationCodes.TryGetValue(verificationRequest.ICNumber, out var emailCode) &&
                SmsVerificationCodes.TryGetValue(verificationRequest.ICNumber, out var smsCode))
            {
                if (emailCode == verificationRequest.VerificationCode && smsCode == verificationRequest.VerificationCode)
                {
                    EmailVerificationCodes.TryRemove(verificationRequest.ICNumber, out _);
                    SmsVerificationCodes.TryRemove(verificationRequest.ICNumber, out _);

                    if (TempUserStorage.TryRemove(verificationRequest.ICNumber, out var user))
                    {
                        user.Migration = true;
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();

                        return Ok(new { Message = "Verification successful. User created." });
                    }
                }
            }

            return BadRequest("Invalid verification code or ICNumber.");
        }
    

    private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var smtpClient = new SmtpClient(smtpSettings["Host"])
            {
                Port = int.Parse(smtpSettings["Port"]),
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["Username"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
        private void SimulateSmsSending(string phoneNumber, string message)
        {
            Console.WriteLine($"Sending SMS to {phoneNumber}: {message}");
        } 

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid login request.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.ICNumber == loginRequest.ICNumber &&
                    u.Email == loginRequest.Email &&
                    u.PhoneNumber == loginRequest.PhoneNumber &&
                    u.Pin == loginRequest.Pin &&
                    u.Name == loginRequest.Name);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new { Message = "Login successful." });
        }


        public class VerificationRequest
        {
            [Required]
            public string ICNumber { get; set; }

            [Required]
            public string VerificationCode { get; set; }
        }


    }







}
