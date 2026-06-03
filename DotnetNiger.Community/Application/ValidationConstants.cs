namespace DotnetNiger.Community.Application;

public static class ValidationConstants
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    public const int MaxTitleLength = 200;
    public const int MinPasswordLength = 8;
    public const int MaxContentLength = 10000;
    public const int LoginAttemptsLimit = 5;
    public const int RegisterAttemptsLimit = 3;
    public const int RateLimitWindowSeconds = 300;
}

public static class ValidationMessages
{
    public const string TitleRequired = "Le titre est requis";
    public const string TitleTooLong = "Le titre est trop long";
    public const string ContentRequired = "Le contenu est requis";
}
