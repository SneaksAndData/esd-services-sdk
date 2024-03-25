using Akka;
using Akka.Streams.Dsl;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Azure
{
    /// <summary>
    /// https://github.com/Azure/azure-sdk-for-net/blob/Azure.Data.Tables_12.4.0/sdk/tables/Azure.Data.Tables/MigrationGuide.md
    /// </summary>
    public class AzureTableService<T> : IEntityCollectionService<T> where T : class, ITableEntity, new()
    {
        private readonly TableServiceClient tableServiceClient;
        private readonly ILogger<AzureTableService<T>> logger;

        /// <summary>
        /// Entity collection storage implementation based on Azure Tables.
        /// </summary>
        /// <param name="tableServiceClient">Azure Table or Cosmos service client.</param>
        /// <param name="logger">Service logger.</param>
        public AzureTableService(TableServiceClient tableServiceClient, ILogger<AzureTableService<T>> logger)
        {
            this.tableServiceClient = tableServiceClient;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task<bool> DeleteEntity(string entityName, T entity)
        {
            var tableClient = tableServiceClient.GetTableClient(entityName);

            return tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey).Map(result => !result.IsError);
        }

        /// <inheritdoc />
        public Source<T, NotUsed> GetEntities(string entityName, string filterCondition = "", int? itemsPerBatch = null)
        {
            var tableClient = tableServiceClient.GetTableClient(entityName);

            return Source
                .From(tableClient.Query<T>(filter: filterCondition, maxPerPage: itemsPerBatch.GetValueOrDefault(1000))
                    .AsPages())
                .SelectMany(pg => pg.Values);
        }

        /// <inheritdoc />
        public Task<T> GetEntity(string entityName, string rowId, string partitionId)
        {
            return tableServiceClient.GetTableClient(entityName).GetEntityAsync<T>(partitionId, rowId)
                .TryMap(
                result => result.Value,
                ex =>
                {
                    switch (ex)
                    {
                        case RequestFailedException rfex:
                            this.logger.LogError(rfex,
                                "Failure when reading entity {entityName}, row: {rowId}, partition: {partitionId}",
                                entityName, rowId, partitionId);
                            break;
                        case ArgumentNullException nrex:
                            this.logger.LogError(nrex, "Failure when reading entity {entityName}", entityName);
                            break;
                        default:
                            this.logger.LogError(ex, "Unhandled exception when reading entity {entityName}",
                                entityName);
                            break;
                    }
                    return default(T);
                }
            );
        }

        /// <inheritdoc />
        public Task<MergeEntityResult> MergeEntity(string entityName, T entity)
        {
            var tableClient = tableServiceClient.GetTableClient(entityName);
            return tableClient.UpsertEntityAsync(entity, mode: TableUpdateMode.Merge).Map(result =>
                new MergeEntityResult
                {
                    IsSuccessful = !result.IsError,
                    Trace = result.ContentStream == null ? null :
                        result.Content.ToStream().Length > 0 ? result.Content.ToString() : null
                });
        }
    }
}
