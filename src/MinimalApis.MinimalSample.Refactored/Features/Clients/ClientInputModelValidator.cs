using FluentValidation;

namespace MinimalApis.MinimalSample.Refactored.Features.Clients;

public class ClientInputModelValidator : AbstractValidator<ClientInputModel>
{
    public ClientInputModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 100);
    }
}
