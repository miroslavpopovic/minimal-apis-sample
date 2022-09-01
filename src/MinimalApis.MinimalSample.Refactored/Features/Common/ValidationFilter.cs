using FluentValidation;

namespace MinimalApis.MinimalSample.Refactored.Features.Common;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.SingleOrDefault(p => p!.GetType() == typeof(T)) is not T validatable)
        {
            return Results.BadRequest("Could not find validatable object.");
        }

        var validationResult = await _validator.ValidateAsync(validatable);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}
