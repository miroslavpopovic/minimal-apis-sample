using FluentValidation;

namespace MinimalApis.MinimalSample.Refactored.Features.Projects;

public class ProjectInputModelValidator : AbstractValidator<ProjectInputModel>
{
    public ProjectInputModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 100);

        RuleFor(x => x.ClientId)
            .NotEmpty();
    }
}