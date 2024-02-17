namespace CreateSqliteDatabaseGenerator;
internal class ResultsModel
{
    public string MainNamespace { get; set; } = "";
    //if none was found, then won't be here.
    public string DatabaseContextNamespace { get; set; } = "";
    public string DatabaseContextName { get; set; } = "";
    public bool ImplementedConfiguration { get; set; }
    public bool ImplementedDatabaseInterface { get; set; }
    //the new format should ensure only one database.


    //public int HowManyDatabases { get; set; } //if more than one, then raise error.
    //public bool HadDatabaseContext { get; set; }
    public BasicList<TableModel> Tables { get; set; } = [];
    //public string SqlitePath { get; set; } = ""; //this is where it needs to capture the sqlite path.
}