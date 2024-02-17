namespace CreateSqliteDatabaseGenerator;
internal static class SourceBuilderExtensions
{
    public static void WriteCreateDatabaseStub(this SourceCodeStringBuilder builder, Action<ICodeBlock> action, string mainNameSpace)
    {
        //global using cc1 = ManuallyCreateSqliteDatabase.CreateClass;
        builder.WriteLine(w =>
        {
            w.Write("global using cc1 = ")
            .Write(mainNameSpace)
            .Write(".CreateDatabaseClass;");
        })
        .WriteLine("using Microsoft.Extensions.Configuration;")
        .WriteLine(w =>
        {
            w.Write("namespace ")
            .Write(mainNameSpace)
            .Write(";");

        }).WriteLine("internal static class CreateDatabaseClass")
        .WriteCodeBlock(w =>
        {
            w.WriteLine("public static void CreateSqliteDatabase(bool recreateIfExists = false)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine("string path;")
                .WriteLine("string? possible;")
                .WriteLine("string connectionString;")
                .WriteLine("string sql;")
                .WriteLine("string key;")
                .WriteLine("global::System.Data.SQLite.SQLiteCommand command;")
                .WriteLine("global::System.Data.SQLite.SQLiteConnection m_dbConnection;")
                .WriteLine("var config = global::CommonBasicLibraries.BasicDataSettingsAndProcesses.BasicDataFunctions.Configuration ?? throw new global::CommonBasicLibraries.BasicDataSettingsAndProcesses.CustomBasicException(\"No IConfiguration Registered\");");
                action.Invoke(w);
            });
        });


        //.WriteCodeBlock(action.Invoke)
        //;




        //builder.WriteLine("#nullable enable");
        //builder.WriteLine("namespace CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.BasicExtensions;")
        //.WriteLine("public static partial class ModelExtensions")
        //.WriteCodeBlock(action.Invoke);
    }
    public static void WriteCloneExtension(this SourceCodeStringBuilder builder, Action<ICodeBlock> action)
    {
        builder.WriteLine("#nullable enable")
            .WriteLine("namespace CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.BasicExtensions;")
            .WriteLine("public static partial class ModelExtensions")
            .WriteCodeBlock(action.Invoke);
    }
}