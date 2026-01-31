namespace MyFicDB.Web.DatabaseHealth
{
    public interface IDatabaseHealthSnapshotStore
    {
        DatabaseHealthSnapshotRecord? Current { get; }
        void Set(DatabaseHealthSnapshotRecord databaseHealthSnapshot);
    }
}
