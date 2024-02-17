namespace CreateSqliteDatabaseGenerator;
[Generator] //this is important so it knows this class is a generator which will generate code for a class using it.
#pragma warning disable RS1036 // Specify analyzer banned API enforcement setting
public class MySourceGenerator : IIncrementalGenerator
#pragma warning restore RS1036 // Specify analyzer banned API enforcement setting
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c =>
        {
            c.CreateCustomSource().BuildSourceCode();
        });
        IncrementalValuesProvider<ClassDeclarationSyntax> declares = context.SyntaxProvider.CreateSyntaxProvider(
            (s, _) => IsSyntaxTarget(s),
            (t, _) => GetTarget(t))
            .Where(m => m != null)!;
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilation
            = context.CompilationProvider.Combine(declares.Collect());
        context.RegisterSourceOutput(compilation, (spc, source) =>
        {
            Execute(source.Item1, source.Item2, spc);
        });
    }
    private bool IsSyntaxTarget(SyntaxNode syntax)
    {
        bool rets = syntax is ClassDeclarationSyntax ctx &&
            ctx.BaseList is not null &&
            ctx.ToString().Contains(nameof(BaseCreateSqliteDatabaseContext<object>));
        return rets;
    }
    private ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var ourClass = context.GetClassNode(); //can use the sematic model at this stage
        return ourClass; //for this one, return the class always in this case.
    }
    private void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> list, SourceProductionContext context)
    {
        var others = list.Distinct();
        ParseClass parses = new(others, compilation);
        var results = parses.GetResults();
        EmitClass emits = new(results, context);
        emits.Emit();
    }
}