using Roots_Wings.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Roots_Wings.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string _gmailAddress = _configuration["Roots&WingsMailAddress"];
            string _gmailPassword = _configuration["Roots&WingsMailPassword"];
            try
            {
                var fromAddress = new MailAddress(_gmailAddress, "Roots&Wings");
                var toAddress = new MailAddress(email);

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, _gmailPassword)
                };

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // TO DO: Log or handle exception, retry using PoLY retry policy later
                throw;  // Re-throwing the exception
            }
        }

        public async Task SendConfirmationEmailAsync(string email, string subject, string confirmationLink, string imageUrl)
        {
            // Constructing the full path to the email template
            var templatePath = Path.Combine(_env.WebRootPath, "emailTemplates", "confirmEmail.html");
            var template = await File.ReadAllTextAsync(templatePath);
            var emailBody = template.Replace("{ConfirmationLink}", confirmationLink);

            await SendEmailAsync(email, subject, emailBody);
        }
    }
}