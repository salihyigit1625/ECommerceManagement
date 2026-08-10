using FluentValidation;
using ECommerceManagement.Application.DTOs.Auth;

namespace ECommerceManagement.Application.Validations.Auth;

public class SellerRegisterDtoValidator : AbstractValidator<SellerRegisterDto>
{
    public SellerRegisterDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı boş bırakılamaz.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş bırakılamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş bırakılamaz.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Şirket adı alanı zorunludur.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası alanı zorunludur.");
    }
}