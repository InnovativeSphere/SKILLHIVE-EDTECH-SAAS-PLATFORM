using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RazorLight;
using SkillHive.Common;

namespace SkillHive.Features.Email.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly Logger _logger;
        private readonly RazorLightEngine _razor;

        public EmailService(IConfiguration config, Logger logger)
        {
            _config = config;
            _logger = logger;

            var templatePath = Path.Combine(AppContext.BaseDirectory, "Features", "Email", "Templates");

            _razor = new RazorLightEngineBuilder()
                .UseFileSystemProject(templatePath)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task SendAsync<T>(string to, string subject, string templateName, T model)
        {
            var html = await RenderTemplateAsync(templateName, model);
            await SendRawAsync(to, subject, html, attachments: null);
        }

        public async Task SendWithAttachmentAsync<T>(
            string to,
            string subject,
            string templateName,
            T model,
            IEnumerable<string> attachmentPaths)
        {
            var html = await RenderTemplateAsync(templateName, model);
            await SendRawAsync(to, subject, html, attachmentPaths);
        }

        private async Task<string> RenderTemplateAsync<T>(string templateName, T model)
        {
            var templateFile = $"{templateName}.cshtml";
            return await _razor.CompileRenderAsync(templateFile, model);
        }

        private async Task SendRawAsync(string to, string subject, string html, IEnumerable<string>? attachments)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_config["Smtp:From"]!));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = html };

                if (attachments != null)
                {
                    foreach (var path in attachments)
                    {
                        if (File.Exists(path))
                            await bodyBuilder.Attachments.AddAsync(path);
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
               await client.ConnectAsync(
    _config["Smtp:Host"],
    int.Parse(_config["Smtp:Port"] ?? "465"),
    SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.Info($"Email sent to {Utils.MaskEmail(to)} — {subject}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Email send failed to {Utils.MaskEmail(to)} — {subject}", ex);
                // Do not rethrow: email failures must never crash the caller
            }
        }
    }
}