using System;
using System.Collections.Generic;
using System.Threading;
using Akka;
using Akka.Streams.Dsl;
using System.Threading.Tasks;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace Snd.Sdk.Storage.Base
{
    /// <summary>
    /// Service with CQL API (Cassandra, Scylla, Astra).
    /// </summary>
    public interface ICqlEntityService
    {
        /// <summary>
        /// Generates select ... from ... text statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string GetSelectAllExpression<T>();

        /// <summary>
        /// Creates a new entity with optional TTL.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        /// <param name="ttlSeconds">Time to live for this entity.</param>
        /// <param name="insertNulls">Whether to merge non-supplied fields.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<bool> UpsertEntity<T>(T entity, int? ttlSeconds = null, bool insertNulls = false);

        /// <summary>
        /// Inserts or updates a batch of entities in the table with optional TTL and null field handling.
        /// </summary>
        /// <typeparam name="T">The type of the entities to be upserted.</typeparam>
        /// <param name="entities">A list of entities to be upserted.</param>
        /// <param name="batchSize">The number of entities to be processed in each batch. Default is 1000.</param>
        /// <param name="ttlSeconds">Optional time to live for the entities in seconds. Default is null.</param>
        /// <param name="insertNulls">Specifies whether to merge non-supplied fields. Default is false.</param>
        /// <returns></returns>
        Task<bool> UpsertBatch<T>(List<T> entities, int batchSize = 1000, int? ttlSeconds = null,
            bool insertNulls = false, string rateLimit = "1000 per second", CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates a pair of entities atomically with optional TTL.
        /// </summary>
        /// <param name="first">First entity.</param>
        /// <param name="second">Second entity.</param>
        /// <param name="ttlSeconds">Time to live for this entity.</param>
        /// <param name="insertNulls">Whether to merge non-supplied fields.</param>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <returns></returns>
        Task<bool> UpsertAtomicPair<TFirst, TSecond>(TFirst first, TSecond second, int? ttlSeconds = null,
            bool insertNulls = false);

        /// <summary>
        /// Inserts a new entity in the table or merges it with an existing one. Supports setting TTL and updating counters.
        /// </summary>
        /// <param name="updateEntityDelegate">CQL statement to update this entity.</param>
        /// <returns></returns>
        Task<bool> UpdateEntity<T>(Func<Table<T>, CqlUpdate> updateEntityDelegate);

        /// <summary>
        /// Reads a single entity from cloud table applying optional filters.
        /// </summary>
        /// <param name="selectEntityDelegate">Select statement for this entity.</param>
        /// <returns>Single entity of type T.</returns>
        Task<T> GetEntity<T>(Func<Table<T>, CqlQuery<T>> selectEntityDelegate);

        /// <summary>
        /// Reads all entities matching supplied filters. If no filters are supplied, streams whole table.
        /// </summary>
        /// <param name="selectEntitiesDelegate">Select query for these entities.</param>
        /// <param name="itemsPerBatch">Number of rows to extract per batch.</param>
        /// <returns>Stream of entities of type T.</returns>
        Source<T, NotUsed> GetEntities<T>(Func<Table<T>, CqlQuery<T>> selectEntitiesDelegate,
            int? itemsPerBatch = null);

        /// <summary>
        /// Runs a provided statement and maps its result to T. If no filters are supplied in the query, this will stream the whole table.
        /// Cql statement is derived from delegate using implicit table reference based on entity type.
        /// </summary>
        /// <param name="cqlStatementDelegate">Select query provider.</param>
        /// <param name="itemsPerBatch">Number of rows to extract per batch.</param>
        /// <returns>Stream of entities of type T.</returns>
        Source<T, NotUsed> GetEntities<T>(Func<Table<T>, string> cqlStatementDelegate, int? itemsPerBatch = null);

        /// <summary>
        /// Runs a provided statement and maps its result to T. If no filters are supplied in the query, this will stream the whole table.
        /// </summary>
        /// <param name="cqlStatement">Select query for these entities.</param>
        /// <param name="itemsPerBatch">Number of rows to extract per batch.</param>
        /// <returns>Stream of entities of type T.</returns>
        Source<T, NotUsed> GetEntities<T>(string cqlStatement, int? itemsPerBatch = null);

        /// <summary>
        /// Runs a Cql query and translates into a provided type.
        /// Careful with queries for this method as it does not page results.
        /// </summary>
        /// <param name="cqlStatement"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IEnumerable<T>> GetObjectResult<T>(string cqlStatement);

        /// <summary>
        /// Removes a specified entity from an entity collection.
        /// </summary>
        /// <param name="deleteEntityDelegate">CQL statement to delete this entity.</param>
        /// <returns></returns>
        Task<bool> DeleteEntity<T>(Func<Table<T>, CqlDelete> deleteEntityDelegate);

        /// <summary>
        /// Reads a subset of a paged query using paging state blob. If not provided, will always return the first page.
        /// </summary>
        /// <param name="selectEntityDelegate">SELECT Cql query.</param>
        /// <param name="pageSize">Page size to return.</param>
        /// <param name="pagingState">Page identifier to return.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IPage<T>> GetEntityPage<T>(Func<Table<T>, CqlQuery<T>> selectEntityDelegate, int? pageSize = null, byte[] pagingState = null);

        /// <summary>
        /// Reads a subset of a paged query using paging state blob. If not provided, will always return the first page.
        /// </summary>
        /// <param name="cqlStatement">SELECT Cql query.</param>
        /// <param name="pageSize">Page size to return.</param>
        /// <param name="pagingState">Page identifier to return.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IPage<T>> GetEntityPage<T>(string cqlStatement, int? pageSize = null, byte[] pagingState = null);

        /// <summary>
        /// Reads a subset of a paged query using paging state blob. If not provided, will always return the first page.
        /// </summary>
        /// <param name="cqlStatementDelegate">SELECT Cql query delegate.</param>
        /// <param name="pageSize">Page size to return.</param>
        /// <param name="pagingState">Page identifier to return.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<IPage<T>> GetEntityPage<T>(Func<Table<T>, string> cqlStatementDelegate, int? pageSize = null,
            byte[] pagingState = null);

        /// <summary>
        /// Returns the implicit table name for the model type provided.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string GetTableName<T>();

        /// <summary>
        /// Returns the keyspace name for the service.
        /// </summary>
        /// <returns></returns>
        string GetKeyspace();
    }
}
