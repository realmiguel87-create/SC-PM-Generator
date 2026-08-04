using FluentValidation;

namespace SCPM.Application.Cost.Commands.RecordForecast;

public class RecordForecastCommandValidator : AbstractValidator<RecordForecastCommand>
{
    public RecordForecastCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ForecastCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CommentaryNotes).MaximumLength(2000);
    }
}
