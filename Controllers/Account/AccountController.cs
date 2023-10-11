using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotePad.Data;
using NotePad.Models;
using NotePad.ViewModels.Dto;
using System.Net;
using System.Net.Mail;

namespace NotePad.Controllers.Account
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly SmtpSettings _smtpSettings;
        private readonly Datacontext _datacontext;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager,
            IOptions<SmtpSettings> smtpSettings, Datacontext datacontext, IConfiguration configuration,
            IServiceProvider serviceProvider, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _smtpSettings = smtpSettings.Value;
            _datacontext = datacontext;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        [HttpGet("Register")]
        public IActionResult Register()
        {
            var response = new UserRegistrationDto();
            return View(response);
        }

        [HttpGet("Verify")]
        public IActionResult Verify()
        {
            var response = new VerificationDto();
            return View(response);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register (UserRegistrationDto userDto)
        {
            try
            {
                string otp = GenerateOtp();

                if (userDto.Password != userDto.ConfirmPassword)
                {
                    ModelState.AddModelError("Confirm Password", "Password is not equal to confirm password");

                    return View();
                }


                var user = new User
                {
                    Email = userDto.Email.ToLowerInvariant(),
                    UserName = userDto.UserName,
                    PasswordHash = userDto.Password,
                    Verification = otp,
                    VerificationCodeExpiration = DateTime.UtcNow.AddMinutes(20)
                };


                var existinguser = await _userManager.FindByEmailAsync(user.Email);
                
                if (existinguser != null)
                {
                    ModelState.AddModelError("Email", "Email alraedy exists");
                    return View(userDto);
                }

                var result = await _userManager.CreateAsync(user, userDto.Password);

                if (result.Succeeded)
                {
                    bool otpSent = false;

                    otpSent = SendOtp(user.Email, otp);

                    if (!otpSent)
                    {
                        return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to send OTP email. Please try again later.");
                    }
                    // Redirect to the "Index" page after successful registration
                    return RedirectToAction("Verify", "Account");
                }

                else
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Please check the provided information.");
                    return View(userDto);
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("Verify")]
        public async Task<IActionResult> Verify(VerificationDto verificationDto)
        {
            // Retrieve the user based on some unique identifier (e.g., email)
            var user = _datacontext.Users.FirstOrDefault(x => x.Verification == verificationDto.Otp);

            if (user != null)
            {
                // Check if the OTP is correct
                string storedOTP = user.Verification;
                if (verificationDto.Otp == storedOTP)
                {
                    // Check if the verification code is still valid (within the time limit)
                    if (user.VerificationCodeExpiration.HasValue && user.VerificationCodeExpiration > DateTime.UtcNow)
                    {
                        // OTP is valid and within the time limit. Redirect to the login view.
                        user.isVerified = true;
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Verification code has expired. Please request a new one.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid OTP. Please try again.");
                }
            }
            else
            {
                ModelState.AddModelError("", "User not found. Please check your email and OTP.");
            }

            return View(verificationDto);
        }


        public IActionResult Login()
        {
            var response = new LoginDto();
            return View(response);
        }



        private bool SendOtp(string email, string otp)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port);
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                smtpClient.EnableSsl = true;

                mail.From = new MailAddress("NoReply@TextBID.com");
                mail.To.Add(email);
                mail.Subject = "OTP Verification";
                mail.Body = $"Your OTP is: {otp}";

                smtpClient.Send(mail);

                // Set the verification code expiration time to 10 minutes from now
                TimeSpan codeExpirationTime = TimeSpan.FromMinutes(10);

                var user = _datacontext.Users.SingleOrDefault(u => u.Email == email);

                // Return true if the email was sent successfully
                return true;
            }
            catch (Exception ex)
            {
                // Handle the exception (log or report it)
                Console.WriteLine($"Error sending OTP email: {ex.Message}");
                return false;
            }
        }


        private string GenerateOtp()
        {
            Random random = new Random();
            int otpValue = random.Next(100000, 999999);
            return otpValue.ToString();
        }
    }
}
