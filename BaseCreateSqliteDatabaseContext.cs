global using CreateSqliteDatabaseGenerator;
namespace CreateSqliteDatabaseGenerator;
[IncludeCode]
internal interface ISqlitePropertyConfig<T>
{
    ISqlitePropertyConfig<T> SetToNullable<P>(Func<T, P> propertySelector);
    ISqlitePropertyConfig<T> SetCustomDefault<P>(string value, Func<T, P> propertySelector);
    ISqlitePropertyConfig<T> SetDateStampDefault<P>(Func<T, P> propertySelector);
}
internal interface ISqliteConfig
{
    ISqliteConfig CreateTableWithDefaults<T>();
    ISqliteConfig CreateTableWithPropertiesOptions<T>(Action<ISqlitePropertyConfig<T>> action);
}
internal abstract class BaseCreateSqliteDatabaseContext<D>
{
    public const string ConfigureName = nameof(Configure);
    protected abstract void Configure(ISqliteConfig config);
}