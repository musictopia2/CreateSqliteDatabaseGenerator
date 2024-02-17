namespace CreateSqliteDatabaseGenerator;
internal class TableModel
{
    public string NamespaceName { get; set; } = "";
    public string TableName { get; set; } = ""; //this is what the table will be called.
    public string ClassName { get; set; } = "";


    //public string GetGlobalName => $"global::{NamespaceName}.{ClassName}";
    public string ErrorMessage { get; set; } = "";

    //public bool ImplementedSimpleIdentityInterface { get; set; } //this will help to see what errors to show up. //if a table did not implement proper interface, then must show error.
    public BasicList<ColumnModel> Columns { get; set; } = [];
}