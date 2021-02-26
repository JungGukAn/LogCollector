using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LogCollectorCore;
using MongoDB.Bson;

namespace LogControllerDemo
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
            services.AddControllers();
            services.AddLogCollector(configure =>
            {
                configure.BaseUri = "/logs";
                configure.ConcurrentCount = 2;
                configure.FlushIntervalSeconds = 30;
                configure.FlushMaxCount = 10;

                configure.RegistPipeline("mongo.**", (configure) =>
                {
                    configure.Add(tagLog =>
                    {
                        var tag = tagLog.Tag;
                        if (tag.Count <= 1)
                            return false;

                        return true;
                    })
                    .Add(tagLog =>
                    {
                        var tag = tagLog.Tag;
                        var log = tagLog.Log;

                        var now = DateTime.UtcNow;

                        // Insert tag token.
                        tag.Add(now.ToString("yyyy-MM-dd"));

                        // Add or modify field
                        if (log.ContainsKey("time"))
                        {
                            log.Remove("time");
                        }

                        log.time = now;
                    })
                    .Add((tag, logs) =>
                    {
                        var db = tag[1];
                        var collection = tag[2];

                        System.Collections.Generic.List<BsonDocument> documents = new System.Collections.Generic.List<BsonDocument>(logs.Count);
                        int failCount = 0;

                        foreach(var log in logs)
                        {
                            try
                            {
                                var document = BsonDocument.Create(log);
                                documents.Add(document);
                            }
                            catch(Exception ex)
                            {
                                failCount++;
                                Console.WriteLine(ex.Message);
                            }
                        }

                        DBManager.UseMongo(mongoClient =>
                        {
                            mongoClient.GetDatabase(db).GetCollection<BsonDocument>(collection).InsertMany(documents);
                            return true;
                        });

                        Console.WriteLine($"DB: {db}, Collection: {collection}, SuccessCount: {documents.Count}, FailCount: {failCount}");
                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(logs, Newtonsoft.Json.Formatting.Indented));
                    });
                });

                configure.RegistPipeline("keyValue.*", (configure) =>
                {
                    configure.Add((tag, logs) =>
                    {
                        var today = DateTime.UtcNow.Date;

                        string key = tag[1];
                        int count = logs.Count;

                        DBManager.UseMySql(command =>
                        {
                            command.CommandText = "insert into tblKeyCount (`key`, `count`) values (@key, @count) on duplicate key update `count` = `count` + values(`count`);";
                            command.Parameters.AddWithValue("@key", key);
                            command.Parameters.AddWithValue("@count", count);

                            return command.ExecuteNonQuery() > 0;
                        });

                        Console.WriteLine($"Key: {key}, Count: {count}");
                    });
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLogCollector();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
