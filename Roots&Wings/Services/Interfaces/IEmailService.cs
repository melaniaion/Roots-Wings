using Microsoft.AspNetCore.Identity.UI.Services;

namespace Roots_Wings.Services.Interfaces
{
    public interface IEmailService : IEmailSender
    {
        Task SendConfirmationEmailAsync(string email, string subject, string confirmationLink, string imageUrl);
    }
}
