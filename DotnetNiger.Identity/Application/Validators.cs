using FluentValidation;
using DotnetNiger.Identity.Application.DTOs;

namespace DotnetNiger.Identity.Application;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Matches(@"^[a-z]+\.[a-z]+$");
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().Matches(@"^[a-z0-9-]+$");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class CreateTenantClientRequestValidator : AbstractValidator<CreateTenantClientRequest>
{
    public CreateTenantClientRequestValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RedirectUris)
            .Must(BeValidJsonUriList)
            .When(x => x.RedirectUris != null)
            .WithMessage("Les redirect URIs doivent être un tableau JSON valide d'URLs");
        RuleFor(x => x.PostLogoutRedirectUris)
            .Must(BeValidJsonUriList)
            .When(x => x.PostLogoutRedirectUris != null)
            .WithMessage("Les post-logout redirect URIs doivent être un tableau JSON valide d'URLs");
    }

    private static bool BeValidJsonUriList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            var uris = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            return uris != null && uris.All(u => Uri.TryCreate(u, UriKind.Absolute, out _));
        }
        catch { return false; }
    }
}

public class CreateTenantApiKeyRequestValidator : AbstractValidator<CreateTenantApiKeyRequest>
{
    public CreateTenantApiKeyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
