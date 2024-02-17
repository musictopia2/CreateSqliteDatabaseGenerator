namespace CreateSqliteDatabaseGenerator;
internal static class SourceContextExtensions
{
    public static void RaiseContextException(this SourceProductionContext context, string information)
    {
        context.ReportDiagnostic(Diagnostic.Create(Context(information), Location.None));
    }
    public static void RaiseTableException(this SourceProductionContext context, string information)
    {
        context.ReportDiagnostic(Diagnostic.Create(NoTables(information), Location.None));
    }
    private static DiagnosticDescriptor NoTables(string information) => new("Second",
       "Could not create tables",
       information,
       "CreateTable",
       DiagnosticSeverity.Error,
       true
       );
    private static DiagnosticDescriptor Context(string information) => new("First",
       "Could not create database",
       information,
       "CreateDatabase",
       DiagnosticSeverity.Error,
       true
       );
}