using System;
using System.Net.Http;
using System.Net.Http.Headers;
using aspnetcore_cassandra.Services;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace aspnetcore_cassandra
{
    public class Startup
    {

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
            string scope = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_SCOPE");
            string listKeyUrl = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_LISTKEYURL");
            string keyspace = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_KEYSPACE");

            string accessToken = GetAccessTokenByMsIdentity(scope);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage result = httpClient.PostAsync(listKeyUrl, new StringContent("")).Result;
            DatabaseAccountListKeysResult connStrResult = result.Content.ReadAsAsync<DatabaseAccountListKeysResult>().Result;

            string username = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_USERNAME");
            string password = connStrResult.PrimaryMasterKey;
            string contactPoints = Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_CONTACTPOINT");
            int port = int.Parse(Environment.GetEnvironmentVariable("RESOURCECONNECTOR_TESTWEBAPPSYSTEMASSIGNEDIDENTITYCONNECTIONSUCCEEDED_PORT"));

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
    }
}
