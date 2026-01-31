using MyFicDB.Web.DatabaseHealth;

namespace MyFicDB.Core.DatabaseHealth
{
    public sealed class DatabaseHealthSnapshotStore : IDatabaseHealthSnapshotStore
    {
        private DatabaseHealthSnapshotRecord? _current;

        public DatabaseHealthSnapshotRecord? Current => Volatile.Read(ref _current);

        public void Set(DatabaseHealthSnapshotRecord snapshot)
        {
            Volatile.Write(ref _current, snapshot);
        }
    }
}
