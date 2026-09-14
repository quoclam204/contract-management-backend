using System;
using CLM.Domain;

namespace CLM.Domain.Entities.Workflow
{
    public class ApprovalStep : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public Guid WorkflowStepId { get; set; }
        public Guid ApproverId { get; set; }
        public StepStatus Status { get; set; }
        public string? Comment { get; set; }
        public DateTime ProcessedAt { get; set; }

        public virtual WorkflowStep WorkflowStep { get; set; } = default!;
    }
}