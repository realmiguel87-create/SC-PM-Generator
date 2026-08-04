namespace SCPM.Application.SBCC.Dtos;

public class VariationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal ValueImpact { get; set; }
    public string Status { get; set; } = default!;
}

public class ExtensionOfTimeDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public int DaysClaimed { get; set; }
    public int? DaysAwarded { get; set; }
    public string Status { get; set; } = default!;
}

public class LossAndExpenseClaimDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal ClaimedAmount { get; set; }
    public decimal? AwardedAmount { get; set; }
    public string Status { get; set; } = default!;
}

public class ArchitectsInstructionDto
{
    public Guid Id { get; set; }
    public int InstructionNumber { get; set; }
    public string Description { get; set; } = default!;
    public DateOnly IssuedDate { get; set; }
    public string Status { get; set; } = default!;
}

public class InterimValuationDto
{
    public Guid Id { get; set; }
    public int ValuationNumber { get; set; }
    public DateOnly ValuationDate { get; set; }
    public decimal GrossValuation { get; set; }
    public decimal NetPayment { get; set; }
    public string Status { get; set; } = default!;
}
