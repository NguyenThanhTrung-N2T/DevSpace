using FluentValidation;

namespace Auth.Application.EmailVerification.ResendEmail;

public record ResendEmailRequest(string Email);

public class ResendEmailRequestValidator : AbstractValidator<ResendEmailRequest>
{
    public ResendEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}
