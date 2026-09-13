using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Features.Email.Services;

namespace SkillHive.Features.Email
{
    [ApiController]
    [Route("api/email/test")]
    public class EmailTestController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public EmailTestController(EmailService emailService, IConfiguration config)
        {
            _emailService = emailService;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] TestEmailRequest req)
        {
            await _emailService.SendAsync(
                req.To,
                "Welcome to SkillHive",
                "welcome",
                new
                {
                    FullName = req.Name,
                    PlatformName = _config["App:Name"] ?? "SkillHive",
                    DashboardUrl = _config["App:BaseUrl"] ?? "http://localhost:5075"
                });

            return ApiResponse.Success(new { sentTo = req.To }, "Test email sent");
        }
    }

    public class TestEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}