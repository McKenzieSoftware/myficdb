using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFicDB.Web.DatabaseHealth;

namespace MyFicDB.Web.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/v1/db")]
    public sealed class ApiDatabaseHealthCheckController : ControllerBase
    {
        private readonly IDatabaseHealthSnapshotStore _databaseHealthSnapshotStore;

        public ApiDatabaseHealthCheckController(IDatabaseHealthSnapshotStore databaseHealthSnapshotStore)
        {
            _databaseHealthSnapshotStore = databaseHealthSnapshotStore;
        }

        [HttpGet("health")]
        [ProducesResponseType(typeof(DatabaseHealthSnapshotRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetDatabaseHealth()
        {
            var snapshot = _databaseHealthSnapshotStore.Current;
            
            if(snapshot is null)
            {
                return NoContent();
            }

            return Ok(snapshot);
        }
    }
}
