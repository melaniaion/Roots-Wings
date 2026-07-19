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
            _env = env;  //in order to access data from wwwroot
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
                    IsBodyHtml = true  //to render the code
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // TO DO: Log or handle exception, retry using PoLY retry policy later
                throw;  // Re-throwing the exception
            }
        }

        //FOR CHANGED EMAIL
        public async Task SendConfirmationEmailAsync(string email, string subject, string confirmationLink)
        {
            // Constructing the full path to the email template
            var templatePath = Path.Combine(_env.WebRootPath, "emailTemplates", "confirmEmail.html");
            var template = await File.ReadAllTextAsync(templatePath);
            var emailBody = template.Replace("{ConfirmationLink}", confirmationLink);

            await SendEmailAsync(email, subject, emailBody);
        }

        public async Task SendEmailChangeAsync(string email, string subject, string confirmationLink)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "emailTemplates", "confirmEmailChange.html");
            var template = await File.ReadAllTextAsync(templatePath);
            var emailBody = template.Replace("{ConfirmationLink}", confirmationLink);

            await SendEmailAsync(email, subject, emailBody);
        }

        public async Task SendPasswordResetAsync(string email, string subject, string confirmationLink)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "emailTemplates", "resetPassword.html");
            var template = await File.ReadAllTextAsync(templatePath);
            var emailBody = template.Replace("{ConfirmationLink}", confirmationLink);

            await SendEmailAsync(email, subject, emailBody);
        }

    }
}