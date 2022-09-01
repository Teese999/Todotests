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
    public class TodoControllerTests_unit
    {
        private TodoController _controllerWithCustomDb => TodoControllerTests_helpers.ControllerWithCustomDb;
        #region Unit
        #region Get
        [Test]
        public async Task GetTodoItem_shouldReturnTodos_unit()
        {
            //act
            var items = await _controllerWithCustomDb.GetTodoItem();
            //assert
            Assert.Multiple(() =>
            {
                 Assert.That(items.Value.Count(), Is.EqualTo(TodoControllerTests_helpers.CustomTodos.Count()));
                 Assert.That(items.Value.ToList()[0].Name, Is.EqualTo(TodoControllerTests_helpers.CustomTodos[0].Name));
            });
        }
        [Test]
        public async Task GetTodoItem_shouldReturnFirstTodo_unit()
        {
            //act
            var item = await _controllerWithCustomDb.GetTodoItem(1);
            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(item.Value.Id, TodoControllerTests_helpers.CustomTodos[0].Id);
                Assert.AreEqual(item.Value.Name, TodoControllerTests_helpers.CustomTodos[0].Name);
            });

        }
        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public async Task GetTodoItem_unexpecteId_shouldReturnNull_unit(int id)
        {
            //act
            var item = await TodoControllerTests_helpers.Controller.GetTodoItem(-1);
            //assert
            Assert.That(item.Value, Is.EqualTo(null));
        }
        #endregion
        #region Put
        [Test]
        public async Task PutTodoItem_putUnexpectedId_shoudReturn404unit()
        {
            //act
            var editedTodo = new TodoItem() { Id = -1, IsComplete = TodoControllerTests_helpers.CustomTodos[0].IsComplete, Name = TodoControllerTests_helpers.CustomTodos[0].Name + "Edited" };
            var result = await TodoControllerTests_helpers.Controller.PutTodoItem(-1, editedTodo);
            //assert
            Assert.That(((StatusCodeResult)(result)).StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));

        }
        [Test]
        [TestCase(1)]
        public async Task PutTodoItem_putExistingId_shoudReturnEditedTodo_unit(int id)
        {
            //arrange
            var lookTodo= TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id);
            var lookTodoIndex = TodoControllerTests_helpers.CustomTodos.IndexOf(lookTodo);
            var todoQuery = await TodoControllerTests_helpers.ControllerWithCustomDb.GetTodoItem(id);
            var todo = todoQuery.Value;
            todo.Name = "edited";
            //act
            var result = await TodoControllerTests_helpers.ControllerWithCustomDb.PutTodoItem(todo.Id, todo);
            var getResult = await TodoControllerTests_helpers.ControllerWithCustomDb.GetTodoItem(id);
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(((StatusCodeResult)(result)).StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
                Assert.That(todo.Name, Is.EqualTo(getResult.Value.Name));
            });
        }
        [Test]
        [TestCase(1)]
        public async Task PutTodoItem_putDifferentId_shouldReturnBadRequest_unit(int id)
        {
            //arrange
            var lookTodo = TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id);
            var lookTodoIndex = TodoControllerTests_helpers.CustomTodos.IndexOf(lookTodo);
            var todoQuery = await TodoControllerTests_helpers.ControllerWithCustomDb.GetTodoItem(id);
            var todo = todoQuery.Value;
            todo.Name = "edited";
            //act
            var result = await TodoControllerTests_helpers.ControllerWithCustomDb.PutTodoItem(todo.Id+1, todo);
            var getResult = await TodoControllerTests_helpers.ControllerWithCustomDb.GetTodoItem(id);
            //assert
            Assert.That(((StatusCodeResult)(result)).StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        }
        #endregion
        #region Post
        [Test]
        public async Task PostTodoItem_postNewItemWithoutid_itemShouldBeAdded_unit()
        {
            //arrange
            var controller = TodoControllerTests_helpers.ControllerWithCustomDb;
            var newTodo = new TodoItem() { Name = "newItem", IsComplete = false };
            var todosCountBeforeQuery = await controller.GetTodoItem();
            var todosCountBefore = todosCountBeforeQuery.Value.Count();
            //act
            await controller.PostTodoItem(newTodo);
            var todosCountAfterQuery = await controller.GetTodoItem();
            var todosCountAfter = todosCountAfterQuery.Value.Count();
            //assert
            Assert.IsTrue(todosCountAfter - todosCountBefore == 1);
        }
        [Test]
        public async Task PostTodoItem_postNewItemWithExistingid_shouldReturnInvalidOperationException_unit()
        {
            //arrange
            var newTodo = new TodoItem() { Id = 1, Name = "newItem", IsComplete = false };
            //assert
            Assert.ThrowsAsync<System.InvalidOperationException>(() => TodoControllerTests_helpers.Controller.PostTodoItem(newTodo));
        }
        [Test]
        [TestCase(55)]
        public async Task PostTodoItem_postNewItemWithId_itemShouldBeAdded(int id)
        {
            //arrange
            var controller = TodoControllerTests_helpers.ControllerWithCustomDb;
            var newTodo = new TodoItem() { Id = id, Name = "newItem", IsComplete = false };
            //act
            var result = await controller.PostTodoItem(newTodo);
            var getQuery = await controller.GetTodoItem(id);
            var gettedTodo = getQuery.Value;
            //assert
            Assert.AreEqual(newTodo, gettedTodo);
        }
        #endregion
        #region Delete
        [Test]
        public async Task DeleteTodoItem_deleteUnexpectedid_shiuldReturn404_unit()
        {
            //act
            var result = await TodoControllerTests_helpers.ControllerWithCustomDb.DeleteTodoItem(10);

            //assert
            Assert.That(((StatusCodeResult)(result.Result)).StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));

        }
        [Test]
        [TestCase(1)]
        [TestCase(3)]
        public async Task DeleteTodoItem_deleteExistingItem_shouldDeletedItem_unit(int id)
        {
            //arrange
            var controller = TodoControllerTests_helpers.ControllerWithCustomDb;
            //act
            var result = await controller.DeleteTodoItem(id);
            var returnedItem = result.Value;
            //assert
            Assert.That(returnedItem.Name, Is.EqualTo(TodoControllerTests_helpers.CustomTodos.First(x => x.Id == id).Name));

        }
        #endregion
        #endregion
    }
}
