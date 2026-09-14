using ContractManagement.Application.Identity.DTOs;
using FluentValidation;

namespace ContractManagement.Application.Identity.Validators;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(200).WithMessage("Department name must not exceed 200 characters.");
    }
}
