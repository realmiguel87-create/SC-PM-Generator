using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

public class Variation : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Reference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal ValueImpact { get; set; }
    public VariationStatus Status { get; set; } = VariationStatus.Instructed;
}

public class ExtensionOfTime : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Reference { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public int DaysClaimed { get; set; }
    public int? DaysAwarded { get; set; }
    public ExtensionOfTimeStatus Status { get; set; } = ExtensionOfTimeStatus.Claimed;
}

public class LossAndExpenseClaim : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Reference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal ClaimedAmount { get; set; }
    public decimal? AwardedAmount { get; set; }
    public LossAndExpenseStatus Status { get; set; } = LossAndExpenseStatus.Claimed;
}

public class ArchitectsInstruction : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public int InstructionNumber { get; set; }
    public string Description { get; set; } = default!;
    public DateOnly IssuedDate { get; set; }
    public ArchitectsInstructionStatus Status { get; set; } = ArchitectsInstructionStatus.Issued;
}

public class InterimValuation : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public int ValuationNumber { get; set; }
    public DateOnly ValuationDate { get; set; }
    public decimal GrossValuation { get; set; }
    public decimal NetPayment { get; set; }
    public InterimValuationStatus Status { get; set; } = InterimValuationStatus.Draft;
}
