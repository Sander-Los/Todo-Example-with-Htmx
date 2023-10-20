using System.Text.Json;
using Htmx;
using Lucene.Net.Search;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Website.Controllers;

namespace Todo_Example_with_Htmx.Controllers
{
    public class TodoHandlerController : SurfaceController
    {
        private readonly IContentService _contentService;
        private readonly UmbracoHelper _umbracoHelper;

        public TodoHandlerController(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches,
            IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IContentService contentService, UmbracoHelper umbracoHelper) : base(umbracoContextAccessor, databaseFactory,
            services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _contentService = contentService;
            _umbracoHelper = umbracoHelper;
        }

        [HttpGet]
        public IActionResult GetNumberOfTodos()
        {
            var todos = _umbracoHelper
                .ContentAtRoot()
                .FirstOrDefault(x => x.IsDocumentType(Todos.ModelTypeAlias))?
                .Children<Todo>();
            return PartialView("Todos/_TodoCount", todos?.Count());
        }

        [HttpDelete]
        public IActionResult DeleteTodo(int id)
        {
            // simulate error on delete
            var todo = _umbracoHelper.Content(id) as Todo;
            if (todo != null && todo.CannotBeDeleted)
            {
                return StatusCode(500, "Something went wrong on the server example");
            }
            var content = _contentService.GetById(id);
            if (content == null)
            {
                return NotFound();
            }

            _contentService.Delete(content);
            Response.Htmx(h =>
            {
                h.WithTrigger("todoCountUpdated");
            });
            return PartialView("Todos/_EmptyItem", id);
        }

        [HttpPut]
        public IActionResult MarkCompleted(int id)
        {
            var content = _contentService.GetById(id);
            if (content == null)
            {
                return NotFound();
            }

            content.SetValue(nameof(Todo.Completed), true);
            _contentService.SaveAndPublish(content);

            var todo = _umbracoHelper.Content(id) as Todo;
            return PartialView("Todos/_Todo", todo);
        }

        [HttpPut]
        public IActionResult UnmarkCompleted(int id)
        {
            var content = _contentService.GetById(id);
            if (content == null)
            {
                return NotFound();
            }

            content.SetValue(nameof(Todo.Completed), false);
            _contentService.SaveAndPublish(content);

            var todo = _umbracoHelper.Content(id) as Todo;
            return PartialView("Todos/_Todo", todo);
        }



        [HttpPost]
        public IActionResult AddTodo(IFormCollection form)
        {
            form.TryGetValue("todo", out var todo);
            var todosNode = _umbracoHelper.ContentAtRoot().FirstOrDefault(x => x.IsDocumentType(Todos.ModelTypeAlias));
            if (todosNode == null || string.IsNullOrEmpty(todo))
            {
                return CurrentUmbracoPage();
            }

            var newTodo = _contentService.Create(todo, todosNode.Key, Todo.ModelTypeAlias);
            var result = _contentService.SaveAndPublish(newTodo);
            var model = _umbracoHelper.Content(result.Content.Id).SafeCast<Todo>();

            Response.Htmx(h =>
            {
                h.WithTrigger("todoCountUpdated");
            });
            return PartialView("Todos/_Todo", model);
        }
    }
}
