using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using aspnetcore_cassandra.Models;
using Cassandra;
using Cassandra.Mapping;

namespace aspnetcore_cassandra.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private Cluster _cluster;
        private string _keyspace;

        public CosmosDbService(string username, string password, string contactPoints, int port, string keyspace)
        {
            SSLOptions options = new SSLOptions(SslProtocols.Tls12, true, (sender, certificate, chain, sslPolicyErrors) => true);
            options.SetHostNameResolver((ipAddress) => contactPoints);
            _cluster = Cluster
                .Builder()
                .WithCredentials(username, password)
                .WithPort(port)
                .AddContactPoint(contactPoints)
                .WithSSL(options)
                .Build();
            this._keyspace = keyspace;
        }

        //public async Task AddItemAsync(MyItem item)
        //{
        //    await this._container.CreateItemAsync<MyItem>(item, new PartitionKey(item.Id));
        //}

        //public async Task DeleteItemAsync(string id)
        //{
        //    await this._container.DeleteItemAsync<MyItem>(id, new PartitionKey(id));
        //}

        //public async Task<MyItem> GetItemAsync(string id)
        //{
        //    try
        //    {
        //        ItemResponse<MyItem> response = await this._container.ReadItemAsync<MyItem>(id, new PartitionKey(id));
        //        return response.Resource;
        //    }
        //    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        //    {
        //        return null;
        //    }

        //}

        public async Task<IEnumerable<MyItem>> GetItemsAsync()
        {
            ISession session = await _cluster.ConnectAsync(_keyspace).ConfigureAwait(false);
            IMapper mapper = new Mapper(session);
            try
            {
                IList<MyItem> list = new List<MyItem>();
                foreach (MyItem item in await mapper.FetchAsync<MyItem>("Select * from myitem"))
                {
                    list.Add(item);
                }
                return list;
            }
            finally
            {
                session.Dispose();
            }
        }

        //public async Task UpdateItemAsync(string id, MyItem item)
        //{
        //    await this._container.UpsertItemAsync<MyItem>(item, new PartitionKey(id));
        //}
    }
}
