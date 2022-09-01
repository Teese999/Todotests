using System;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TodoApi;
using TodoApi.Models;
using System.Linq;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    public class TodoControllerTests_func
    {
        #region Func
        #region Get
        [Test]
        public async Task GetTodoItem_shouldReturnOneTodo()
        {   
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.GetAsync(TodoControllerTests_helpers.ControllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual(todos.Count(), 1);

        }
        [Test]
        public async Task GetTodoItem_firstItemNameShoudBeItem1()
        {

            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.GetAsync(TodoControllerTests_helpers.ControllerPath);
            var todos = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();

            //assert
            Assert.AreEqual(todos.ToList()[0].Name, "Item1");

        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public async Task GetTodoItem_getUnexpectedId(int id)
        {
            //arrange
            string uri = TodoControllerTests_helpers.ControllerPath + $"/{id.ToString()}";
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.GetAsync(uri);

            //assert
            Assert.AreEqual(response.StatusCode, HttpStatusCode.NotFound);

        }
        [Test]
        [TestCase(1)]
        public async Task GetTodoItem_getExistingId(int id)
        {
            //arrange
            string uri = TodoControllerTests_helpers.ControllerPath + $"/{id.ToString()}";
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.GetAsync(uri);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(todo.Name, Is.EqualTo("Item1"));
                Assert.That(todo.Id, Is.EqualTo(1));
            });

        }
        #endregion
        #region Put
        [Test]
        [TestCase(1)]
        public async Task PutTodoItem_putExistingId_shoudReturnNoContent(int id)
        {
            //arrange
            string uri = TodoControllerTests_helpers.ControllerPath + $"/{id.ToString()}";
            HttpResponseMessage getResponse = await TodoControllerTests_helpers.ClientWithCustomDb.GetAsync(uri);
            var todo = await getResponse.Content.ReadFromJsonAsync<TodoItem>();
            string newName = "newName";
            todo.Name = newName;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.ClientWithCustomDb.PutAsJsonAsync(uri, todo);
            //assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        }
        [Test]
        public async Task PutTodoItem_putUnexpectedId_shoudReturnNotFound()
        {
            //arrange
            TodoItem editedTodo = new TodoItem { Id = -1, IsComplete = false, Name = "EditedName" };
            JsonContent content = JsonContent.Create(editedTodo);
            string uri = TodoControllerTests_helpers.ControllerPath + "/" + editedTodo.Id.ToString();
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.PutAsJsonAsync(uri, editedTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));


        }
        [Test]
        public async Task PutTodoItem_putDifferentId_shouldReturnBadRequest()
        {
            //arrange
            TodoItem editedTodo = new TodoItem { Id = -1, IsComplete = false, Name = "EditedName" };
            JsonContent content = JsonContent.Create(editedTodo);
            string uri = TodoControllerTests_helpers.ControllerPath + "/" + 5;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.PutAsJsonAsync(uri, editedTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }
        #endregion
        #region Post
        [Test]
        public async Task PostTodoItem_postNewItemWithoutid_shouldReturnNewTodo()
        {
            //arrange
            TodoItem newTodo = new TodoItem { IsComplete = false, Name = "NewTodoName" };
            string uri = TodoControllerTests_helpers.ControllerPath;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.PostAsJsonAsync(uri, newTodo);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.That(todo.Name, Is.EqualTo(newTodo.Name));

        }
        [Test]
        public async Task PostTodoItem_postNewItemWithExistingid_ShoudReturnInternalError()
        {
            //arrange
            TodoItem newTodo = new TodoItem { Id = 1, IsComplete = false, Name = "NewTodoName" };
            string uri = TodoControllerTests_helpers.ControllerPath;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.PostAsJsonAsync(uri, newTodo);

            //assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

        }   
        #endregion
        #region Delete
        [Test]
        [TestCase(1)]
        public async Task DeleteTodoItem_deleteExistingItem_dhouldReturnDeletedTodo(int id)
        {
            //arrange
            string uri = TodoControllerTests_helpers.ControllerPath + "/" + id;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.ClientWithCustomDb.DeleteAsync(uri);
            var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(HttpStatusCode.OK, Is.EqualTo(response.StatusCode));
                Assert.That(todo.Id, Is.EqualTo(TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id).Id));
                Assert.That(todo.Name, Is.EqualTo(TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id).Name));
                Assert.That(todo.IsComplete, Is.EqualTo(TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id).IsComplete));
            });

        }
        [Test]
        public async Task DeleteTodoItem_deleteEUnexpectedid_shouldReturnNotFound()
        {
            //arrange
            string uri = TodoControllerTests_helpers.ControllerPath + "/" + -1;
            //act
            HttpResponseMessage response = await TodoControllerTests_helpers.Client.DeleteAsync(uri);
            //assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        #endregion
        #endregion
    }
}

