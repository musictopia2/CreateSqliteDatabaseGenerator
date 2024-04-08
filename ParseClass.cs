namespace CreateSqliteDatabaseGenerator;
internal class ParseClass(IEnumerable<ClassDeclarationSyntax> list, Compilation compilation)
{
    public BasicList<ResultsModel> GetResults()
    {
        BasicList<ResultsModel> output = [];
        foreach (var item in list)
        {
            ResultsModel? p = GetFluentResults(item);
            if (p != null)
            {
                output.Add(p);
            }
        }
        return output;
    }
    private ResultsModel? GetFluentResults(ClassDeclarationSyntax node)
    {
        ParseContext context = new(compilation, node);
        var members = node.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var m in members)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(m) as IMethodSymbol;
            if (symbol is not null && symbol.Name == BaseCreateSqliteDatabaseContext<object>.ConfigureName)
            {
                ResultsModel output = new();
                ParseDatabaseContext(output, context, node);
                ParseContext(output, context, m);
                //cannot put as none because needs it for errors.
                if (output.Tables.Count > 0)
                {
                    output.MainNamespace = output.Tables.First().NamespaceName; //i think the first one is fine.
                }
                return output;
            }
        }
        return null;
    }
    private void PopulateSqliteType(ColumnModel p, bool dateException)
    {
        if (dateException)
        {
            p.SqliteType = "REAL";
            return;
        }
        if (p.PropertySymbol is null)
        {
            return;
        }
        var symbol = p.PropertySymbol.Type;
        if (symbol.Name == "Nullable")
        {
            ITypeSymbol others;
            others = p.PropertySymbol.Type.GetSingleGenericTypeUsed()!;
            p.SqliteType = GetSqliteFromSymbol(others);
            return;
        }
        p.SqliteType = GetSqliteFromSymbol(symbol);
    }
    private string GetSqliteFromSymbol(ITypeSymbol symbol)
    {
        if (symbol.Name == "Nullable")
        {
            return "Error.  No Nullable";
        }
        if (symbol.Name == "Byte")
        {
            return "Error.  No Byte";
        }
        if (symbol.Name == "Int16" || symbol.Name == "Int32"
            || symbol.Name == "Int64" || symbol.Name == "Decimal"
            || symbol.Name == "UInt16" || symbol.Name == "UInt32"
            || symbol.Name == "UInt64" || symbol.Name == "Boolean"
            )
        {
            return "INTEGER";
        }
        if (symbol.TypeKind == TypeKind.Enum)
        {
            return "INTEGER";
        }
        if (symbol.Name.StartsWith("Enum"))
        {
            return "INTEGER";
        }
        if (symbol.Name == "Double" || symbol.Name == "Single")
        {
            return "REAL";
        }
        return "TEXT";
    }
    private bool CanIncludeProperty(IPropertySymbol p)
    {
        if (p.IsKnownType() == false)
        {
            return false;
        }
        if (p.Type.Name == "Object")
        {
            return false; //can't include objects.
        }
        if (p.Type.Name == "Byte")
        {
            return false;
        }
        if (p.Type.Name == "SByte")
        {
            return false;
        }
        if (p.HasAttribute("NotMapped"))
        {
            return false;
        }
        return true;
    }
    private void ParseDatabaseContext(ResultsModel results, ParseContext context, ClassDeclarationSyntax node)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        var temp = symbol!.BaseType;
        var item = temp!.TypeArguments[0];
        results.DatabaseContextName = item.Name;
        results.DatabaseContextNamespace = item.ContainingNamespace.ToDisplayString();
        results.ImplementedDatabaseInterface = item.Implements("ISqlDatabaseConfiguration");
        results.ImplementedConfiguration = item.Implements("ISqlitePathConfiguration");
    }
    private void ParseContext(ResultsModel results, ParseContext context, MethodDeclarationSyntax syntax)
    {
        static CallInfo? GetPropertyCall(IReadOnlyList<CallInfo> calls, IPropertySymbol p, ITypeSymbol classSymbol)
        {
            foreach (var call in calls)
            {
                var ignoreIdentifier = call.Invocation.DescendantNodes()
                       .OfType<IdentifierNameSyntax>()
                       .Last();
                var cloneProp = classSymbol.GetMembers(ignoreIdentifier.Identifier.ValueText)
                .OfType<IPropertySymbol>()
                .SingleOrDefault();
                if (cloneProp.Name == p.Name && cloneProp.OriginalDefinition.ToDisplayString() == p.OriginalDefinition.ToDisplayString())
                {
                    return call;
                }
            }
            return null;
        }
        bool rets;
        var makeCalls = ParseUtils.FindCallsOfMethodWithName(context, syntax, nameof(ISqliteConfig.CreateTableWithPropertiesOptions));
        BasicList<TableModel> output = [];
        foreach (var make in makeCalls)
        {
            INamedTypeSymbol makeType = (INamedTypeSymbol)make.MethodSymbol.TypeArguments[0]!;
            TableModel cc = new();
            cc.ClassName = makeType.Name;
            rets = makeType.TryGetAttribute("Table", out IEnumerable<AttributeData> temp);
            if (rets)
            {
                var data = temp.Single();
                cc.TableName = data.ConstructorArguments.Single().Value!.ToString();
            }
            else
            {
                cc.TableName = makeType.Name;
            }
            cc.NamespaceName = makeType.ContainingNamespace.ToDisplayString();
            PopulatePossibleError(cc, makeType);
            if (string.IsNullOrWhiteSpace(cc.ErrorMessage))
            {
                var pList = makeType.GetAllPublicProperties();
                var seconds = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(ISqlitePropertyConfig<object>.SetCustomDefault), optional: true, argumentIndex: 1);
                var thirds = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(ISqlitePropertyConfig<object>.SetDateStampDefault), optional: true, argumentIndex: 1);
                var fourths = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(ISqlitePropertyConfig<object>.SetToNullable), optional: true, argumentIndex: 1);
                foreach (var p in pList)
                {
                    if (CanIncludeProperty(p))
                    {
                        ColumnModel other = new();
                        other.PropertySymbol = p;
                        string columnAttribute = "Column";
                        rets = p.TryGetAttribute(columnAttribute, out temp);
                        if (rets == false)
                        {
                            other.Name = p.Name;
                        }
                        else
                        {
                            var data = temp.Single();
                            other.Name = data.ConstructorArguments.Single().Value!.ToString();
                        }
                        if (other.Name.ToLower() == "id")
                        {
                            other.IsPrimaryKey = true;
                            if (p.HasAttribute("NoIncrement") == false)
                            {
                                other.NeedsAutoIncrement = true;
                            }
                        }
                        var call = GetPropertyCall(fourths, p, makeType);
                        other.Nullable = call is not null;
                        //needs another condition.  because the model should determine whether this is nullable or not.
                        if (p.Type.Name == "Nullable")
                        {
                            other.Nullable = true; //try this as well (?)
                        }
                        call = GetPropertyCall(thirds, p, makeType);
                        bool dateException = call is not null;
                        if (dateException)
                        {
                            other.DefaultValue = "(datetime(''now'', ''localtime''))";
                            other.UseFunction = true;
                        }
                        else
                        {
                            other.UseFunction = false; //for now.
                            call = GetPropertyCall(seconds, p, makeType);
                            if (call is not null)
                            {
                                other.DefaultValue = ParseUtils.GetStringContent((CallInfo)call);
                            }
                        }
                        PopulateSqliteType(other, dateException);
                        cc.Columns.Add(other);
                    }
                }
            }
            output.Add(cc);
        }
        makeCalls = ParseUtils.FindCallsOfMethodWithName(context, syntax, nameof(ISqliteConfig.CreateTableWithDefaults));
        foreach (var make in makeCalls)
        {
            INamedTypeSymbol makeType = (INamedTypeSymbol)make.MethodSymbol.TypeArguments[0]!;
            TableModel cc = new();
            cc.ClassName = makeType.Name;
            rets = makeType.TryGetAttribute("Table", out IEnumerable<AttributeData> temp);
            if (rets)
            {
                var data = temp.Single();
                cc.TableName = data.ConstructorArguments.Single().Value!.ToString();
            }
            else
            {
                cc.TableName = makeType.Name;
            }
            cc.NamespaceName = makeType.ContainingNamespace.ToDisplayString();
            PopulatePossibleError(cc, makeType);
            if (string.IsNullOrWhiteSpace(cc.ErrorMessage))
            {
                var pList = makeType.GetAllPublicProperties();
                foreach (var p in pList)
                {
                    if (CanIncludeProperty(p))
                    {
                        ColumnModel other = new();
                        other.PropertySymbol = p;
                        string columnAttribute = "Column";
                        rets = p.TryGetAttribute(columnAttribute, out temp);
                        if (rets == false)
                        {
                            other.Name = p.Name;
                        }
                        else
                        {
                            var data = temp.Single();
                            other.Name = data.ConstructorArguments.Single().Value!.ToString();
                        }
                        if (other.Name == "ID")
                        {
                            other.IsPrimaryKey = true;
                            if (p.HasAttribute("NoIncrement") == false)
                            {
                                other.NeedsAutoIncrement = true;
                            }
                        }
                        if (p.Type.Name == "Nullable")
                        {
                            other.Nullable = true; //try this as well (?)
                        }
                        PopulateSqliteType(other, false);
                        cc.Columns.Add(other);
                    }
                }
            }
            output.Add(cc);
        }
        results.Tables = output;
    }
    private void PopulatePossibleError(TableModel cc, INamedTypeSymbol makeType)
    {
        if (makeType.Implements("ISimpleDatabaseEntity") == false)
        {
            cc.ErrorMessage = $"{cc.ClassName} class needs to implement ISimpleDatabaseEntity interface for ids";
        }
        else
        {
            cc.ErrorMessage = "";
        }
    }
}