using FluentValidation;

namespace MinimalApis.MinimalSample.Refactored.Features.Users;

public class UserInputModelValidator : AbstractValidator<UserInputModel>
{
    public UserInputModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 100);

        RuleFor(x => x.HourRate)
            .GreaterThan(0)
            .LessThan(1000);
    }
}