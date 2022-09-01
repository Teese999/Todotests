using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using TodoApi;
using TodoApi.Models;

namespace Tests
{
    public class TodoControllerTests_e2e
    {
        [Test]
        [TestCase("ForDel", "newTodo", "editedName")]
        public async Task GetAddPutDelete_success(string forDelname, string newTodoName, string editedName)
        {
            //arrange
            var client = new TestServer(new WebHostBuilder().UseStartup<Startup>()).CreateClient();
            var newTodo = new TodoItem() { Name = newTodoName };

            //act

            //Get items
            var getQuery = await client.GetAsync(TodoControllerTests_helpers.ControllerPath);
            var items = await getQuery.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();
            var itemsList = items.ToList();
            //Post item
            var postQuery = await client.PostAsJsonAsync(TodoControllerTests_helpers.ControllerPath, newTodo);
            //Edit item
            var getQueryAfterPost = await client.GetAsync(TodoControllerTests_helpers.ControllerPath);
            var itemsAfterPost = await getQueryAfterPost.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();
            var itemsAfterPostList = itemsAfterPost.ToList();
            var editesTodo = new TodoItem() { Id = itemsList[0].Id, Name = editedName, IsComplete = itemsList[0].IsComplete };
            await client.PutAsJsonAsync(TodoControllerTests_helpers.ControllerPath + "/" + editesTodo.Id, editesTodo);
            //Delete item
            var postQueryToDel = await client.PostAsJsonAsync(TodoControllerTests_helpers.ControllerPath, new TodoItem{Name = forDelname });
            var getItemsPreferDel = await client.GetAsync(TodoControllerTests_helpers.ControllerPath);
            var itemsPreferDel = await getItemsPreferDel.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();
            var itemForDel = itemsPreferDel.First(x => x.Name == forDelname);

            var deleteQuery = await client.DeleteAsync(TodoControllerTests_helpers.ControllerPath + "/" + itemForDel.Id);
            var deletedItem = await deleteQuery.Content.ReadFromJsonAsync<TodoItem>();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(itemsList.Count, Is.EqualTo(1));
                Assert.That(itemsAfterPostList.Count, Is.EqualTo(2));
                Assert.That(itemsAfterPostList[1].Name, Is.EqualTo(newTodoName));
                Assert.That(itemsPreferDel.ToList()[0].Name, Is.EqualTo(editedName));
                Assert.That(deletedItem.Name, Is.EqualTo(forDelname));
            });
        }
    }
}

