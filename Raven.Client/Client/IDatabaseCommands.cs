using System;
using Newtonsoft.Json.Linq;
using Raven.Database;
using Raven.Database.Data;

namespace Raven.Client.Client
{
    public interface IDatabaseCommands : IDisposable
	{
		JsonDocument Get(string key);
		PutResult Put(string key, Guid? etag, JObject document, JObject metadata);
		void Delete(string key, Guid? etag);
		string PutIndex(string name, string indexDef);
		QueryResult Query(string index, string query, int start, int pageSize);
		void DeleteIndex(string name);
        JsonDocument[] Get(string[] ids);

        void Commit(Guid txId);
        void Rollback(Guid txId);
	}
}