﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Raven.Abstractions.Data;
using Raven.Server.Documents.Indexes;
using Raven.Server.ServerWide.Context;

namespace Raven.Server.Documents.Queries.Dynamic
{
    public class DynamicQueryRunner
    {
        private const string DynamicIndexPrefix = "dynamic/";

        private readonly IndexStore _indexStore;
        private readonly DocumentsOperationContext _context;
        private readonly DocumentsStorage _documents;

        public DynamicQueryRunner(IndexStore indexStore, DocumentsStorage documents, DocumentsOperationContext context)
        {
            _indexStore = indexStore;
            _context = context;
            _documents = documents;
        }

        public DocumentQueryResult Execute(string dynamicIndexName, IndexQuery query)
        {
            var collection = dynamicIndexName.Substring(DynamicIndexPrefix.Length);

            var map = DynamicQueryMapping.Create(collection, query);

            if (map.MapFields.Length == 0 && map.SortDescriptors.Length == 0)
            {
                // we optimize for empty queries without sorting options
                var result = new DocumentQueryResult
                {
                    IndexName = collection,
                    IsStale = false
                };

                _context.OpenReadTransaction();

                var collectionStats = _documents.GetCollection(collection, _context);

                result.TotalResults = (int) collectionStats.Count;

                foreach (var document in _documents.GetDocumentsAfter(_context, collection, 0, query.Start, query.PageSize))
                {
                    result.Results.Add(document);
                }

                return result;
            }
            
            bool newAutoIndex = false;

            Index index;
            if (TryMatchExistingIndexToQuery(map, out index) == false)
            {
                var autoIndexDef = map.CreateAutoIndexDefinition();

                var id = _indexStore.CreateIndex(autoIndexDef);
                index = _indexStore.GetIndex(id);

                newAutoIndex = true;
            }

            query = EnsureValidQuery(query, map);

            return ExecuteActualQuery(index, query, newAutoIndex);
        }

        private DocumentQueryResult ExecuteActualQuery(Index index, IndexQuery query, bool newAutoIndex)
        {
            // Perform the query until we have some results at least
            var sp = Stopwatch.StartNew();

            while (true)
            {
                var result = index.Query(query, _context, CancellationToken.None); // TODO arek

                if (newAutoIndex == false ||
                    result.IsStale == false ||
                    (result.Results.Count >= query.PageSize && query.PageSize > 0) ||
                    sp.Elapsed.TotalSeconds > 15)
                {
                    return result;
                }

                _context.Reset(); // dispose already open read transactions
                Thread.Sleep(100);
            }
        }

        private bool TryMatchExistingIndexToQuery(DynamicQueryMapping map, out Index index)
        {
            var dynamicQueryToIndex = new DynamicQueryToIndexMatcher(_indexStore);
            
            var matchResult = dynamicQueryToIndex.Match(map);

            switch (matchResult.MatchType)
            {
                case DynamicQueryMatchType.Complete:
                    index = _indexStore.GetIndex(matchResult.IndexName);
                    return true;
                case DynamicQueryMatchType.Partial:
                    // At this point, we found an index that has some fields we need and
                    // isn't incompatible with anything else we're asking for
                    // We need to clone that other index 
                    // We need to add all our requested indexes information to our cloned index
                    // We can then use our new index instead

                    var currentIndex = _indexStore.GetIndex(matchResult.IndexName);
                    map.ExtendMappingBasedOn(currentIndex.Definition);

                    break;
            }

            index = null;
            return false;
        }

        private static IndexQuery EnsureValidQuery(IndexQuery query, DynamicQueryMapping map)
        {
            foreach (var field in map.MapFields)
            {
                query.Query = query.Query.Replace(field.Name, IndexField.ReplaceInvalidCharactersInFieldName(field.Name));
            }

            return query;
        }
    }
}