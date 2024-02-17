namespace CreateSqliteDatabaseGenerator;
internal class ColumnModel
{
    public IPropertySymbol? PropertySymbol { get; set; }
    public string Name { get; set; } = ""; //cannot be just the property symbols name because there are exceptions for this.
    public string? DefaultValue { get; set; } //needs to allow nullable this time.
    public bool Nullable { get; set; } //usually not nullable.
    public string SqliteType { get; set; } = ""; //needs to figure out what type this is.
    //tim says that money will have integer with 2 00s
    public bool NeedsAutoIncrement { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool UseFunction { get; set; }
}