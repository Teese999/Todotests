using System;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using TodoApi;
using TodoApi.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using TodoApi.Controllers;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Data.Entity;
using Microsoft.AspNetCore.Hosting.Server;
using System.Xml.Linq;
using NUnit.Framework;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Tests
{
    public static class TodoControllerTests_helpers
    {
        private static string _controllerPath = "api/Todo";
        private static HttpClient _client { get
            {
                WebApplicationFactory<Startup> _webHostWithDb = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(Services => {
                        var dbContextDescriptor = Services.Where(d => d.ServiceType == typeof(DbContextOptions<TodoContext>)).ToList();
                        dbContextDescriptor.ForEach(descriptor => { Services.Remove(descriptor); });

                        Services.AddDbContext<TodoContext>(Options =>
                        {
                            Options.UseInMemoryDatabase(Guid.NewGuid().ToString());

                        });
                    });
                });
                return _webHostWithDb.CreateClient();
            }
        }
        private static HttpClient _clientWithCustomDb {
            get
            {

                var server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
                var context = server.Host.Services.GetService(typeof(TodoContext)) as TodoContext;

                context.RemoveRange(context.TodoItems.ToList());
                context.AddRange(_customTodos);
                context.SaveChanges();

                return server.CreateClient();
            }

        }
        private static TodoController _controllerWithCustomDb
        { get
            {
                var context = new TodoContext(new DbContextOptionsBuilder<TodoContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
                context.RemoveRange(context.TodoItems.ToList());
                context.AddRange(_customTodos);
                context.SaveChanges();
                return new TodoController(context);
            }
        }
        private static TodoController _controller
        {
            get
            {
                return new TodoController(new TodoContext(new DbContextOptionsBuilder<TodoContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
            }
        }
        private static List<TodoItem> _customTodos = new List<TodoItem>() {
                new TodoItem() { Id = 1, IsComplete = false, Name = "1" },
                new TodoItem() { Id = 2, IsComplete = true, Name = "2" },
                new TodoItem() { Id = 3, IsComplete = false, Name = "3" },
        };

        public static HttpClient ClientWithCustomDb => _clientWithCustomDb;
        public static HttpClient Client => _client;
        public static TodoController Controller => _controller;
        public static TodoController ControllerWithCustomDb => _controllerWithCustomDb;
        public static List<TodoItem> CustomTodos => _customTodos;
        public static string ControllerPath => _controllerPath;

        static TodoControllerTests_helpers()
        {
            //_context = new TodoContext(new DbContextOptionsBuilder<TodoContext>().UseInMemoryDatabase("testDb").Options);
            //_client = new TestServer(new WebHostBuilder().UseStartup<Startup>()).CreateClient();
        }
        //public static async Task<HttpClient>  GetClientWithCustomDb(string DbName = "testDb")
        //{
          
        //    var server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
        //    var context = server.Host.Services.GetService(typeof(TodoContext)) as TodoContext;

        //    context.RemoveRange(context.TodoItems.ToList());
        //    await context.AddRangeAsync(_customTodos);
        //    await context.SaveChangesAsync();
        //    var client = server.CreateClient();

        //    return client;
        //}

    }
}

