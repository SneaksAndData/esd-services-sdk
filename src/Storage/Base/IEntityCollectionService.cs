using System;
using System.Linq.Expressions;
using Akka;
using Akka.Streams.Dsl;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base
{
    /// <summary>
    /// Schema-aware object storage abstraction.
    /// </summary>
    /// <typeparam name="T">Object type in this entity collection.</typeparam>
    public interface IEntityCollectionService<T>
    {
        /// <summary>
        /// Reads a single entity from cloud table applying optional filters.
        /// </summary>
        /// <param name="entityName">Name of the entity collection.</param>
        /// <param name="rowId">Unique row id in the collection.</param>
        /// <param name="partitionId">Optional partition name, if partitioning is used.</param>
        /// <returns>Single entity of type T.</returns>
        Task<T> GetEntity(string entityName, string rowId, string partitionId = null);

        /// <summary>
        /// Reads all entities matching supplied filters. If no filters are supplied, streams whole table.
        /// </summary>
        /// <param name="entityName">Name of the entity collection.</param>
        /// <param name="filterCondition">Optional column expression.</param>
        /// <param name="itemsPerBatch">Number of rows to extract per batch.</param>
        /// <returns>Stream of entities of type T.</returns>
        Source<T, NotUsed> GetEntities(string entityName, string filterCondition = "", int? itemsPerBatch = null);

        /// <summary>
        /// Inserts a new entity in the table or merges it with an existing one.
        /// </summary>
        /// <param name="entityName">Name of the entity collection.</param>
        /// <param name="entity">Object to insert or merge.</param>
        /// <returns></returns>
        Task<MergeEntityResult> MergeEntity(string entityName, T entity);


        /// <summary>
        /// Removes a specified entity from an entity collection.
        /// </summary>
        /// <param name="entityName">Name of the entity collection.</param>
        /// <param name="entity">Object to remove.</param>
        /// <returns></returns>
        Task<bool> DeleteEntity(string entityName, T entity);
    }
}
