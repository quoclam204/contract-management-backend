using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Domain.Workflow.Entities;

/// <summary>
/// Bảng WORKFLOW_STEPS: Cấu hình các bước duyệt trong một luồng duyệt
/// </summary>
[Table("WORKFLOW_STEPS")]
public class WorkflowStep
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid WorkflowDefinitionId { get; set; }

    [Required]
    public int StepOrder { get; set; }

    [Required]
    public ApproverRole ApproverRole { get; set; }

    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Minimum contract amount required for this step to be applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumAmount { get; set; } = 0;

    // Navigation property
    [ForeignKey(nameof(WorkflowDefinitionId))]
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}