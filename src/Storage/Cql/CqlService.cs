using Akka;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Util;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Snd.Sdk.Storage.Base;

namespace Snd.Sdk.Storage.Cql
{
    /// <summary>
    /// CQL-API compatible entity collection service.
    /// </summary>
    public class CqlService : ICqlEntityService
    {
        private readonly ILogger<CqlService> logger;
        private readonly ISession session;

        private Table<T> GetTableRef<T>(string entityName) =>
            new(this.session, MappingConfiguration.Global, entityName);

        private Table<T> GetTableRef<T>() => new(this.session, MappingConfiguration.Global);

        /// <summary>
        /// Entity collection storage implementation based on Apache Cassandra QL API-compatible engines.
        /// </summary>
        /// <param name="cqlSession">Active CQL session.</param>
        /// <param name="logger">Service logger.</param>
        public CqlService(ISession cqlSession, ILogger<CqlService> logger)
        {
            this.session = cqlSession;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task<bool> DeleteEntity<T>(Func<Table<T>, CqlDelete> deleteEntityDelegate)
        {
            var deleteQuery = deleteEntityDelegate(this.GetTableRef<T>());

            return deleteQuery
                .ExecuteAsync()
                .TryMap(maybeResult =>
                {
                    this.logger.LogDebug("Deletion query executed, trace: {queryTrace}", maybeResult.Info.QueryTrace);
                    return true;
                }, exception =>
                {
                    this.logger.LogError(exception, "Failed to execute a CQL query");
                    return false;
                });
        }

        /// <inheritdoc />
        public Source<T, NotUsed> GetEntities<T>(Func<Table<T>, CqlQuery<T>> selectEntitiesDelegate,
            int? itemsPerBatch = null)
        {
            var selectQuery = selectEntitiesDelegate(this.GetTableRef<T>());

            return Source.FromTask(selectQuery.SetPageSize(itemsPerBatch.GetValueOrDefault(1000)).ExecutePagedAsync())
                .ConcatMany(firstResult =>
                {
                    return Source.From(firstResult.ToList())
                        .Concat(Source.UnfoldAsync(firstResult.PagingState, nextBatchContinuator =>
                        {
                            if ((nextBatchContinuator ?? new byte[] { }).Length == 0)
                            {
                                return Task.FromResult(Option<(byte[], List<T>)>.None);
                            }

                            return selectQuery.SetPageSize(itemsPerBatch.GetValueOrDefault(1000))
                                .SetPagingState(nextBatchContinuator).ExecutePagedAsync()
                                .Map(result =>
                                    Option<(byte[], List<T>)>.Create((result.PagingState, result.ToList())));
                        }).SelectMany(v => v));
                });
        }

        private Source<T, NotUsed> GetEntitiesSource<T>(Mapper mapper, string cqlStatement, int? itemsPerBatch = null)
        {
            return Source
                .FromTask(mapper.FetchPageAsync<T>(itemsPerBatch.GetValueOrDefault(1000), null, cqlStatement, null))
                .ConcatMany(firstResult =>
                {
                    return Source.From(firstResult.ToList())
                        .Concat(Source.UnfoldAsync(firstResult.PagingState, nextBatchContinuator =>
                        {
                            if ((nextBatchContinuator ?? new byte[] { }).Length == 0)
                            {
                                return Task.FromResult(Option<(byte[], List<T>)>.None);
                            }

                            return mapper.FetchPageAsync<T>(itemsPerBatch.GetValueOrDefault(1000), nextBatchContinuator,
                                    cqlStatement, null)
                                .Map(result =>
                                    Option<(byte[], List<T>)>.Create((result.PagingState, result.ToList())));
                        }).SelectMany(v => v));
                });
        }

        /// <inheritdoc />
        public Source<T, NotUsed> GetEntities<T>(string cqlStatement, int? itemsPerBatch = null)
        {
            var mapper = new Mapper(this.session);
            return this.GetEntitiesSource<T>(mapper, cqlStatement, itemsPerBatch);
        }

        /// <inheritdoc />
        public Source<T, NotUsed> GetEntities<T>(Func<Table<T>, string> cqlStatementDelegate, int? itemsPerBatch = null)
        {
            var mapper = new Mapper(this.session);
            var cqlStatement = cqlStatementDelegate(this.GetTableRef<T>());
            return this.GetEntitiesSource<T>(mapper, cqlStatement, itemsPerBatch);
        }

        /// <inheritdoc />
        public Task<IEnumerable<T>> GetObjectResult<T>(string cqlStatement)
        {
            var mapper = new Mapper(this.session);
            return mapper.FetchAsync<T>(cqlStatement);
        }

        /// <inheritdoc />
        public string GetSelectAllExpression<T>()
        {
            return this.GetTableRef<T>().Expression.ToString();
        }

        private CqlCommand GetInsertCommand<T>(T entity, int? ttlSeconds = null, bool insertNulls = false)
        {
            return ttlSeconds switch
            {
                { } x => this.GetTableRef<T>()
                    .Insert(entity, insertNulls)
                    .SetTTL(x),
                _ => this.GetTableRef<T>()
                    .Insert(entity, insertNulls)
            };
        }

        /// <inheritdoc />
        public Task<bool> UpsertEntity<T>(T entity, int? ttlSeconds = null, bool insertNulls = false)
        {
            return this.GetInsertCommand(entity, ttlSeconds, insertNulls)
                .ExecuteAsync().TryMap(maybeResult =>
                {
                    this.logger.LogDebug("Entity created, trace: {queryTrace}", maybeResult.Info.QueryTrace);
                    return true;
                }, exception =>
                {
                    this.logger.LogError(exception, "Failed to create the entity");
                    return false;
                });
        }

        /// <inheritdoc />
        public Task<bool> UpsertAtomicPair<TFirst, TSecond>(TFirst first, TSecond second, int? ttlSeconds = null, bool insertNulls = false)
        {
            var loggedBatch = this.session.CreateBatch(BatchType.Logged);

            loggedBatch.Append(this.GetInsertCommand(first, ttlSeconds, insertNulls));
            loggedBatch.Append(this.GetInsertCommand(second, ttlSeconds, insertNulls));

            return loggedBatch.ExecuteAsync().TryMap(() => true, exception =>
            {
                this.logger.LogError(exception, "Failed to insert atomic batch");
                return false;
            });
        }

        /// <inheritdoc />
        public Task<bool> UpdateEntity<T>(Func<Table<T>, CqlUpdate> updateEntityDelegate)
        {
            return updateEntityDelegate(this.GetTableRef<T>()).ExecuteAsync().TryMap(maybeResult =>
            {
                this.logger.LogDebug("Entity updated, trace: {queryTrace}", maybeResult.Info.QueryTrace);
                return true;
            }, exception =>
            {
                this.logger.LogError(exception, "Failed to update the entity");
                return false;
            });
        }

        /// <inheritdoc />
        public Task<T> GetEntity<T>(Func<Table<T>, CqlQuery<T>> selectEntityDelegate)
        {
            var selectQuery = selectEntityDelegate(this.GetTableRef<T>());
            return selectQuery
                .ExecuteAsync()
                .TryMap(
                    result => result.FirstOrDefault(),
                    ex =>
                    {
                        switch (ex)
                        {
                            case ArgumentNullException nrex:
                                this.logger.LogError(nrex, "Failure when reading entity {query}",
                                    selectQuery.Expression.ToString());
                                break;
                            default:
                                this.logger.LogError(ex, "Unhandled exception when reading entity {query}",
                                    selectQuery.Expression.ToString());
                                break;
                        }

                        return default;
                    }
                );
        }

        /// <inheritdoc />
        public Task<IPage<T>> GetEntityPage<T>(
            Func<Table<T>, CqlQuery<T>> selectEntityDelegate,
            int? pageSize = null,
            byte[] pagingState = null)
        {
            var selectQuery = selectEntityDelegate(this.GetTableRef<T>());
            return (pagingState switch
            {
                null => selectQuery
                    .SetPageSize(pageSize.GetValueOrDefault(1000))
                    .ExecutePagedAsync(),
                _ => selectQuery
                    .SetPageSize(pageSize.GetValueOrDefault(1000))
                    .SetPagingState(pagingState)
                    .ExecutePagedAsync()
            }).TryMap(maybePage => maybePage, exception =>
            {
                this.logger.LogError(exception, "Failed to run query {query}",
                    selectQuery.Expression.ToString());
                return default;
            });
        }

        private Task<IPage<T>> GetEntityPageUsingTextQuery<T>(Mapper mapper, string cqlStatement, int? pageSize = null,
            byte[] pagingState = null)
        {
            return mapper.FetchPageAsync<T>(
                pageSize.GetValueOrDefault(1000),
                pagingState,
                cqlStatement,
                null).TryMap(maybePage => maybePage, exception =>
            {
                this.logger.LogError(exception, "Failed to run query {query}", cqlStatement);
                return default;
            });
        }

        /// <inheritdoc />
        public Task<IPage<T>> GetEntityPage<T>(
            string cqlStatement,
            int? pageSize = null,
            byte[] pagingState = null)
        {
            var mapper = new Mapper(this.session);
            return this.GetEntityPageUsingTextQuery<T>(mapper, cqlStatement, pageSize, pagingState);
        }

        /// <inheritdoc />
        public Task<IPage<T>> GetEntityPage<T>(
            Func<Table<T>, string> cqlStatementDelegate,
            int? pageSize = null,
            byte[] pagingState = null)
        {
            var mapper = new Mapper(this.session);
            var cqlStatement = cqlStatementDelegate(this.GetTableRef<T>());

            return this.GetEntityPageUsingTextQuery<T>(mapper, cqlStatement, pageSize, pagingState);
        }

        /// <inheritdoc />
        public string GetTableName<T>()
        {
            return this.GetTableRef<T>().Name;
        }

        /// <inheritdoc />
        public string GetKeyspace()
        {
            return this.session.Keyspace;
        }
    }
}
