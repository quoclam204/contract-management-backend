using System;
using System.Collections.Generic;
using CLM.Domain;

namespace CLM.Domain.Entities.Workflow
{
    public class WorkflowDefinition : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    }
}