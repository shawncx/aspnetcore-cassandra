using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using aspnetcore_cassandra.Services;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace aspnetcore_cassandra
{
    public class Startup
    {
        private const string CassandraUsernameConnectionStringKey = "Username";
        private const string CassandraPasswordConnectionStringKey = "Password";
        private const string CassandraContactPointConnectionStringKey = "HostName";
        private const string CassandraPortConnectionStringKey = "Port";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            CosmosDbService sev = InitializeCosmosClientInstance();
            services.AddSingleton<ICosmosDbService>(sev);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=MyItem}/{action=Index}/{id?}");
            });
        }

        private static CosmosDbService InitializeCosmosClientInstance()
        {
            string resourceEndpoint = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_RESOURCEENDPOINT");
            string scope = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_SCOPE");
            string connUrl = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_CONNECTIONSTRINGURL");
            string keyspace = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_KEYSPACE");

            string accessToken = GetAccessTokenByMsIdentity(scope);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage result = httpClient.PostAsync(connUrl, new StringContent("")).Result;
            DatabaseAccountListConnectionStringsResult connStrResult = result.Content.ReadAsAsync<DatabaseAccountListConnectionStringsResult>().Result;

            string connectionString = string.Empty;
            foreach (DatabaseAccountConnectionString connStr in connStrResult.ConnectionStrings)
            {
                if (connStr.Description.Contains("Primary") && connStr.Description.Contains("Cassandra"))
                {
                    connectionString = connStr.ConnectionString;
                }
            }

            IDictionary<string, string> connStrDict = ParseConnectionString(connectionString);
            string username = connStrDict[CassandraUsernameConnectionStringKey];
            string password = connStrDict[CassandraPasswordConnectionStringKey];
            string contactPoints = connStrDict[CassandraContactPointConnectionStringKey];
            int port = int.Parse(connStrDict[CassandraPortConnectionStringKey]);

            CosmosDbService cosmosDbService = new CosmosDbService(
                username,
                password,
                contactPoints,
                port,
                keyspace);
            return cosmosDbService;
        }

        private static string GetAccessTokenByMsIdentity(string scope)
        {
            ManagedIdentityCredential cred = new ManagedIdentityCredential();
            TokenRequestContext reqContext = new TokenRequestContext(new string[] { scope });
            AccessToken token = cred.GetTokenAsync(reqContext).Result;
            return token.Token;
        }

        private static IDictionary<string, string> ParseConnectionString(string connectionString)
        {
            // connection string is in format: HostName={hostname};Username={username};Password={password};Port={port}
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string seg in connectionString.Split(";"))
            {
                int index = seg.IndexOf("=");
                if (index < 0)
                {
                    continue;
                }
                string key = seg.Substring(0, index);
                string value = seg.Substring(index + 1);
                dict.Add(key, value);
            }
            return dict;
        }
    }
}
