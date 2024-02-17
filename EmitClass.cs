namespace CreateSqliteDatabaseGenerator;
internal class EmitClass(BasicList<ResultsModel> results, SourceProductionContext context)
{
    private BasicList<TableModel> GetTableErrors()
    {
        BasicList<TableModel> output = [];
        foreach (var item in results)
        {
            foreach (var p in item.Tables)
            {
                if (p.ErrorMessage != "")
                {
                    output.Add(p);
                }
            }
        }
        return output;
    }
    private string GetErrorMessage(ResultsModel result)
    {
        if (result.ImplementedDatabaseInterface == false && result.Tables.Count > 0)
        {
            return $"The database context class of {result.DatabaseContextName} requires the ISqlDatabaseConfiguration interface to be implemented so it can look up the key";
        }
        if (result.ImplementedDatabaseInterface == false && result.Tables.Count == 0)
        {
            return $"The database context class of {result.DatabaseContextName} requires the ISqlDatabaseConfiguration interface to be implemented so it can look up the key.  Also had no tables";
        }
        if (result.Tables.Count == 0)
        {
            return $"The database context class of {result.DatabaseContextName} cannot be created because there are no tables";
        }
        return "";
    }
    private BasicList<string> GetContextErrors()
    {
        BasicList<string> output = [];
        foreach (var item in results)
        {
            string fins = GetErrorMessage(item);
            if (string.IsNullOrWhiteSpace(fins) == false)
            {
                output.Add(fins);
            }
        }
        return output;
    }
    public void Emit()
    {
        var firsts = GetContextErrors();
        bool hadErrors = false;
        if (firsts.Count != 0)
        {
            foreach (var item in firsts)
            {
                context.RaiseContextException(item);
            }
            hadErrors = true;
        }
        if (results.Count == 0)
        {
            return;
        }
        var seconds = GetTableErrors();
        if (seconds.Count != 0)
        {
            foreach (var item in seconds)
            {
                context.RaiseTableException(item.ErrorMessage);
            }
            hadErrors = true;
        }
        if (hadErrors)
        {
            return; //because you cannot create the source code because there was errors that had to be fixed first.
        }
        SourceCodeStringBuilder builder = new();
        builder.WriteCreateDatabaseStub(w =>
        {
            foreach (var result in results)
            {
                WriteBasicResults(w, result);
                w.WriteLine("if (File.Exists(path) == false)")
                 .WriteCodeBlock(w =>
                 {
                     w.WriteLine("connectionString = $\"Data Source={path};Version=3;\";")
                     .WriteLine("m_dbConnection = new(connectionString);")
                     .WriteLine("m_dbConnection.Open();");
                     WriteTableResults(w, result);
                     w.WriteLine("command.Dispose();")
                     .WriteLine("m_dbConnection.Close();")
                     .WriteLine("m_dbConnection.Dispose();");
                 });
            }
        }, results.Single().MainNamespace);
        context.AddSource("CreateDatabase.g.cs", builder.ToString());
    }
    private void WriteTableResults(ICodeBlock w, ResultsModel result)
    {
        result.Tables.ForEach(t =>
        {
            w.WriteLine(w =>
            {
                w.Write("sql = ")
                .AppendDoubleQuote(w =>
                {
                    w.Write("create table ");
                    WriteFinishTableStatement(w, t);
                }).Write(";");
            }).WriteLine("command = new(sql, m_dbConnection);")
        .WriteLine("command.ExecuteNonQuery();");
        });
    }
    private void WriteFinishTableStatement(IWriter w, TableModel table)
    {
        //this is where i write the sql statement.
        w.Write(table.TableName)
            .Write("(");
        var fins = table.Columns.Last();
        foreach (var item in table.Columns)
        {
            bool last = fins.Equals(item);
            WriteColumns(w, item, last);
        }
        w.Write(")");
    }
    private void WriteColumns(IWriter w, ColumnModel column, bool last)
    {
        w.Write(column.Name)
            .Write(" ")
            .Write(column.SqliteType);
        if (column.Nullable == false)
        {
            w.Write(" NOT NULL");
        }
        if (column.IsPrimaryKey && column.NeedsAutoIncrement)
        {
            w.Write(" PRIMARY KEY AutoIncrement");
        }
        else if (column.IsPrimaryKey)
        {
            w.Write(" PRIMARY KEY");
        }
        else if (column.DefaultValue is not null)
        {
            w.Write(" DEFAULT ");
            WriteStartDefault(w, column);
            w.Write(column.DefaultValue!);
            WriteStartDefault(w, column);
        }
        if (last == false)
        {
            w.Write(", ");
        }
    }
    private void WriteStartDefault(IWriter w, ColumnModel column)
    {
        if (column.UseFunction || column.SqliteType.ToLower() == "text")
        {
            w.Write("'");
        }
    }
    private void WriteBasicResults(ICodeBlock w, ResultsModel result)
    {
        w.WriteLine(w =>
        {
            w.Write("key = ").GlobalWrite()
            .Write(result.DatabaseContextNamespace)
            .Write(".")
            .Write(result.DatabaseContextName)
            .Write(".DatabaseName;")
            .Write(";");
        })
        .WriteLine("possible = config.GetValue<string>($\"{key}Path\");");
        if (result.ImplementedConfiguration)
        {
            w.WriteLine("if (possible is not null)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine("path = possible;");
            })
            .WriteLine("else")
            .WriteCodeBlock(w =>
            {
                w.WriteLine(w =>
                {
                    w.Write("path = ")
                    .GlobalWrite()
                    .Write(result.DatabaseContextNamespace)
                    .Write(".")
                    .Write(result.DatabaseContextName)
                    .Write(".Path;");
                });
            });
        }
        else
        {
            w.WriteLine("if (possible is null)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine("throw new global::CommonBasicLibraries.BasicDataSettingsAndProcesses.CustomBasicException($\"No value was given for key {key} and did not implement interface to have default path\");");
            })
            .WriteLine("path = possible;");
        }
        w.WriteLine("if (File.Exists(path) && recreateIfExists)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine("File.Delete(path);");
            });
    }
}