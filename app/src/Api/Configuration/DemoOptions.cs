namespace RefaccionariaCuate.Api.Configuration;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";
    public bool AllowReset { get; set; }
    public string DefaultAdminPassword { get; set; } = "Demo123!";
}
