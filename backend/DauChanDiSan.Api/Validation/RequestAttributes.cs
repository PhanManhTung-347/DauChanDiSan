using System.ComponentModel.DataAnnotations;

namespace DauChanDiSan.Api.Validation;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NonEmptyGuidAttribute : ValidationAttribute
{
    public NonEmptyGuidAttribute() : base("Mã yêu cầu phải là UUID khác rỗng.") { }

    public override bool IsValid(object? value) => value is Guid guid && guid != Guid.Empty;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class OneOfAttribute(params string[] allowed) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || value is string text && allowed.Contains(text, StringComparer.Ordinal);

    public override string FormatErrorMessage(string name) =>
        $"{name} phải là một trong các giá trị {string.Join(", ", allowed)}.";
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("URL phải có giao thức HTTP hoặc HTTPS.") { }

    public override bool IsValid(object? value) =>
        value is null || value is string text && Uri.TryCreate(text.Trim(), UriKind.Absolute, out var url)
            && url.Scheme is "http" or "https" && !string.IsNullOrEmpty(url.Host);
}
