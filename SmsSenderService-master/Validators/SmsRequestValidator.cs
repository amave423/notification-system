using FluentValidation;
using SmsSenderService.Models;

namespace SmsSenderService.Validators;

public class SmsRequestValidator : AbstractValidator<SmsRequest>
{
    public SmsRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{1,14}$");

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(1600);

        RuleFor(x => x.Sender)
            .MaximumLength(11)
            .When(x => !string.IsNullOrEmpty(x.Sender));
    }
}