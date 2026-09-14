using FluentValidation;

namespace ContractManagement.Application.Features.Attachments.Validators
{
    public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
    {
        public UploadAttachmentCommandValidator()
        {
            RuleFor(x => x.ContractId)
                .NotEmpty().WithMessage("ContractId là bắt buộc.");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("Tên file là bắt buộc.")
                .MaximumLength(500).WithMessage("Tên file không được vượt quá 500 ký tự.");

            RuleFor(x => x.FileStream)
                .NotNull().WithMessage("Dữ liệu tệp không được để trống.");
        }
    }
}
