using System;
using CLM.Domain;

namespace CLM.Domain.Entities.Workflow
{
    public class WorkflowStep : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid WorkflowDefinitionId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; } = default!;
        public Guid ApproverRoleId { get; set; }
        public decimal MinimumAmount { get; set; }

        public virtual WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    }
}