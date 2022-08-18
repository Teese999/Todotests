using NUnit.Framework;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Collections.Generic;
using TodoApi.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using System;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using TodoApi.Controllers;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Tests
{
    [TestFixture]
    public class TodoControllerTests
    {
        private string _controllerPath = "api/Todo";
        private WebApplicationFactory<Startup> _webHost = new WebApplicationFactory<Startup>().WithWebHostBuilder(_ => { });
        private List<TodoItem> _customTodos = new List<TodoItem>() {
                new TodoItem() { Id = 1, IsComplete = false, Name = "1" },
                new TodoItem() { Id = 2, IsComplete = true, Name = "2" },
                new TodoItem() { Id = 3, IsComplete = false, Name = "3" },
            };

        private async Task<HttpClient> _getClientWithCustomDb(string dbName = "test_db_custom")
        {
            WebApplicationFactory<Startup> _webHostWithDb = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(Services => {
                    var dbContextDescriptor = Services.Where(d => d.ServiceType == typeof(DbContextOptions<TodoContext>)).ToList();
                    dbContextDescriptor.ForEach(descriptor => { Services.Remove(descriptor); });

                    Services.AddDbContext<TodoContext>(Options =>
                    {
                        Options.UseInMemoryDatabase(dbName);

                    });
                });
            });
            TodoContext testDb = _webHostWithDb.Services.CreateScope().ServiceProvider.GetService<TodoContext>();


            await testDb.AddRangeAsync(_customTodos);

            await testDb.SaveChangesAsync();
            HttpClient client = _webHostWithDb.CreateClient();
            return client;
        }
        private async Task<TodoController> _getTodoController(string dbName = "controller_test_db_custom")
        {
            var optionsBuilder = new DbContextOptionsBuilder<TodoContext>();
            var options = optionsBuilder.UseInMemoryDatabase(dbName).Options;

            using (TodoContext db = new TodoContext(options))
            {
                db.AddRange(_customTodos);
                await db.SaveChangesAsync();
            }
            var controller = new TodoController(new TodoContext(options));
            return controller;
        }
        #region Func
        #region Common
        [Test]
        public async Task CheckStatus_shouldReturnOk()
        {

            //arrange

            HttpClient client = _webHost.CreateClient();

            //act
            HttpResponseMessage response = await client.GetAsync(_controllerPath);


            //assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        #endregion
        #region Get
        [Test]
        public async Task GetTodoItem_shouldReturnOneTodo()
        {
            //arrange

            HttpClient client = _webHost.CreateClient();

            //act
            HttpResponseMessage response = await client.GetAsync(_controllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, todos.Count());

        }
        [Test]
        public async Task GetTodoItem_firstItemNameShoudBeItem1()
        {
            //arrange

            HttpClient client = _webHost.CreateClient();

            //act
            HttpResponseMessage response = await client.GetAsync(_controllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual("Item1", todos.ToList()[0].Name);

        }
        [Test]
        public async Task GetTodoItem_withCustomDb()
        {
            //arrange
            HttpClient client = await _getClientWithCustomDb();
            //act
            HttpResponseMessage response = await client.GetAsync(_controllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual(3, todos.Count());
            Assert.AreEqual(1, todos.ToList()[0].Id);
            Assert.AreEqual(false, todos.ToList()[0].IsComplete);
            Assert.AreEqual("1", todos.ToList()[0].Name);

        }
        [Test]
        public async Task GetTodoItem_checkAutocreateTodo()
        {
            //arrange
            WebApplicationFactory<Startup> _webHostWithDb = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(Services => {
                    var dbContextDescriptor = Services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
                    Services.Remove(dbContextDescriptor);
                    Services.AddDbContext<TodoContext>(Options =>
                    {
                        Options.UseInMemoryDatabase("test_db");
                    });
                });
            });
            TodoContext testDb = _webHostWithDb.Services.CreateScope().ServiceProvider.GetService<TodoContext>();
            HttpClient client = _webHost.CreateClient();

            //act
            HttpResponseMessage response = await client.GetAsync(_controllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual("Item1", todos.ToList()[0].Name);

        }

        [Test]
        public async Task GetTodoItem_getUnexpectedId()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            string uri = _controllerPath + "/-1";
            //act
            HttpResponseMessage response = await client.GetAsync(uri);

            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        }
        [Test]
        public async Task GetTodoItem_getExistingId()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            string uri = _controllerPath + "/1";
            //act
            HttpResponseMessage response = await client.GetAsync(uri);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual("Item1", todo.Name);
            Assert.AreEqual(1, todo.Id);
            //Assert.AreSame
        }
        #endregion
        #region Put
        [Test]
        public async Task PutTodoItem_putExistingId_shoudReturnEditedTodo()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            TodoItem editedTodo = new TodoItem { Id = 3, IsComplete = false, Name = "EditedName" };
            string uri = _controllerPath + "/" + editedTodo.Id.ToString();
            //act
            HttpResponseMessage response = await client.PutAsJsonAsync(uri, editedTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(todo.Id, editedTodo.Id);
            Assert.AreEqual(todo.Name, editedTodo.Name);

        }
        [Test]
        public async Task PutTodoItem_putUnexpectedId_shoudReturnNotFound()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            TodoItem editedTodo = new TodoItem { Id = -1, IsComplete = false, Name = "EditedName" };
            JsonContent content = JsonContent.Create(editedTodo);
            string uri = _controllerPath + "/" + editedTodo.Id.ToString();
            //act
            HttpResponseMessage response = await client.PutAsJsonAsync(uri, editedTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        }
        [Test]
        public async Task PutTodoItem_putDifferentId_shouldReturnBadRequest()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            TodoItem editedTodo = new TodoItem { Id = -1, IsComplete = false, Name = "EditedName" };
            JsonContent content = JsonContent.Create(editedTodo);
            string uri = _controllerPath + "/" + 5;
            //act
            HttpResponseMessage response = await client.PutAsJsonAsync(uri, editedTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        }
        #endregion
        #region Post
        [Test]
        public async Task PostTodoItem_postNewItemWithoutid_shouldReturnNewTodo()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            TodoItem newTodo = new TodoItem { IsComplete = false, Name = "NewTodoName" };
            string uri = _controllerPath;
            //act
            HttpResponseMessage response = await client.PostAsJsonAsync(uri, newTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(todo.Name, newTodo.Name);

        }
        [Test]
        public async Task PostTodoItem_postNewItemWithExistingid_ShoudReturnInternalError()
        {
            //arrange
            HttpClient client = _webHost.CreateClient();
            TodoItem newTodo = new TodoItem { Id = 1, IsComplete = false, Name = "NewTodoName" };
            string uri = _controllerPath;
            //act
            HttpResponseMessage response = await client.PostAsJsonAsync(uri, newTodo);

            //assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        }
        [Test]
        public async Task PostTodoItem_postNewItemWithIncorrectid_ShoudReturnInternalError()
        {
            //arrange
            HttpClient client = await _getClientWithCustomDb();
            TodoItem newTodo = new TodoItem { Id = -1, IsComplete = false, Name = "NewTodoName" };
            string uri = _controllerPath;
            //act
            HttpResponseMessage response = await client.PostAsJsonAsync(uri, newTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        }
        #endregion
        #region Delete
        [Test]
        public async Task DeleteTodoItem_deleteExistingItem_dhouldReturnDeletedTodo()
        {
            //arrange
            HttpClient client = await _getClientWithCustomDb("delete");
            string uri = _controllerPath + "/" + _customTodos[2].Id;
            //act
            HttpResponseMessage response = await client.DeleteAsync(uri);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(todo.Id, _customTodos[2].Id);
            Assert.AreEqual(todo.Name, _customTodos[2].Name);
            Assert.AreEqual(todo.IsComplete, _customTodos[2].IsComplete);
        }
        [Test]
        public async Task DeleteTodoItem_deleteEUnexpectedid_shouldReturnNotFound()
        {
            //arrange
            HttpClient client = await _getClientWithCustomDb("delete2");
            string uri = _controllerPath + "/" + -1;
            //act
            HttpResponseMessage response = await client.DeleteAsync(uri);
            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
        #endregion
        #endregion
        #region Unit
        #region Get
        [Test]
        public async Task GetTodoItem_shouldReturnTodos_unit()
        {
            //arrange
            var controller = await _getTodoController("getitems1");
            //act
            var items = await controller.GetTodoItem();
            //assert
            Assert.AreEqual(items.Value.Count(), _customTodos.Count());
            Assert.AreEqual(items.Value.ToList()[0].Name, _customTodos[0].Name);
        }
        [Test]
        public async Task GetTodoItem_shouldReturnFirstTodo_unit()
        {
            //arrange
            var controller = await _getTodoController("getitems2");
            //act
            var item = await controller.GetTodoItem(1);
            //assert
            Assert.AreEqual(item.Value.Id, _customTodos[0].Id);
            Assert.AreEqual(item.Value.Name, _customTodos[0].Name);
        }
        [Test]
        public async Task GetTodoItem_unexpecteId_shouldReturnNull_unit()
        {
            //arrange
            var controller = await _getTodoController("getitems3");
            //act
            var item = await controller.GetTodoItem(-1);
            //assert
            Assert.AreEqual(item.Value, null);
        }
        #endregion
        #region Put
        [Test]
        public async Task PutTodoItem_putUnexpectedId_shoudReturn204_unit()
        {
            //arrange
            var controller = await _getTodoController("putitems1");
            //act
            var editedTodo = new TodoItem() { Id = -1, IsComplete = _customTodos[0].IsComplete, Name = _customTodos[0].Name + "Edited" };
            var result = await controller.PutTodoItem(-1, editedTodo);
            //assert
            Assert.AreEqual(204, ((StatusCodeResult)(result)).StatusCode);

        }
        [Test]
        public async Task PutTodoItem_putExistingId_shoudReturnEditedTodo_unit()
        {
            //arrange
            var controller = await _getTodoController("putitems2");
            //act
            var editedTodo = new TodoItem() { Id = _customTodos[0].Id, IsComplete = _customTodos[0].IsComplete, Name = _customTodos[0].Name + "Edited" };
            var result = await controller.PutTodoItem(editedTodo.Id, editedTodo);

            //assert
            Assert.AreEqual(result.GetType(), typeof(TodoItem));
        }
        [Test]
        public async Task PutTodoItem_putDifferentId_shouldReturnBadRequest_unit()
        {
            //arrange
            var controller = await _getTodoController("putitems3");
            var editedTodo = new TodoItem() { Id = _customTodos[0].Id, IsComplete = _customTodos[0].IsComplete, Name = _customTodos[0].Name + "Edited" };
            //act
            var result = await controller.PutTodoItem(-1, editedTodo);
            //assert
            Assert.AreEqual(400, ((StatusCodeResult)(result)).StatusCode);

        }
        #endregion
        #region Post
        [Test]
        public async Task PostTodoItem_postNewItemWithoutid_shouldReturnNewTodo_unit()
        {
            //arrange
            var controller = await _getTodoController("postitems1");
            var newTodo = new TodoItem() {Name = "newItem", IsComplete = false };
            //act
            var result = await controller.PostTodoItem(newTodo);
            //assert
            Assert.AreEqual(newTodo.Name, result.Value.Name);
        }
        [Test]
        public async Task PostTodoItem_postNewItemWithExistingid_shouldReturnArgimentException_unit()
        {
            //arrange
            var controller = await _getTodoController("postitems2");
            var newTodo = new TodoItem() {Id = 1, Name = "newItem", IsComplete = false };
            //assert
            Assert.ThrowsAsync<System.ArgumentException>(() => controller.PostTodoItem(newTodo));
        }
        [Test]
        public async Task PostTodoItem_postNewItem_shouldReturnAddeditem_unit()
        {
            //arrange
            var controller = await _getTodoController("postitems3");
            var newTodo = new TodoItem() { Id = 5, Name = "newItem", IsComplete = false };
            //act
            var result = await controller.PostTodoItem(newTodo);
            var returneditem = result.Value;
            //assert
            Assert.AreEqual(newTodo.Name, returneditem.Name);

        }
        #endregion
        #region Delete
        [Test]
        public async Task DeleteTodoItem_deleteUnexpectedid_shiuldReturn404_unit()
        {
            //arrange
            var controller = await _getTodoController("deleteitems1");
            //act
            var result = await controller.DeleteTodoItem(10);

            //assert
            Assert.AreEqual(404, ((StatusCodeResult)(result.Result)).StatusCode);

        }
        [Test]
        public async Task DeleteTodoItem_deleteExistingItem_shouldDeletedItem_unit()
        {
            //arrange
            var controller = await _getTodoController("deleteitems2");
            //act
            var result = await controller.DeleteTodoItem(1);
            var returnedItem = result.Value;

            //assert
            Assert.AreEqual(_customTodos.First(x => x.Id == 1).Name, returnedItem.Name);

        }
        #endregion
        #endregion
    }
}
