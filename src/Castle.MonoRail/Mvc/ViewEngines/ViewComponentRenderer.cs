namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System;
	using System.ComponentModel.Composition;
	using System.IO;
	using Microsoft.CSharp.RuntimeBinder;
	using Primitives.Mvc;

	[Export(typeof(ViewComponentRenderer))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class ViewComponentRenderer
	{
		[Import]
		public ViewComponentProvider ComponentProvider { get; set; }

		[Import]
		public IMonoRailServices Services { get; set; }

		public string Render(string componentView, ViewContext viewContext, object model)
		{
			var viewEngines = Services.ViewEngines;

			var result = viewEngines.ResolveViewComponent(componentView, new ViewResolutionContext(viewContext.ActionContext));

			if (result.Successful)
			{
				try
				{
					using (var writer = new StringWriter())
					{
						result.ViewComponent.Process(viewContext, writer, model);

						return writer.ToString();
					}
				}
				finally
				{
					result.ViewEngine.Release(result.View);
				}
			}
			else
			{
				throw new Exception("Could not find view " + componentView + ". Searched at " + string.Join(", ", result.SearchedLocations));
			}
		}

		public string Render(Type component, ViewContext viewContext)
		{
			ViewResult result = null;

			var viewEngines = Services.ViewEngines;

			dynamic instance = ComponentProvider.Create(component).ComponentInstance;

			result = instance.Render();

			var viewName = component.Name.Replace("Component", "");
			if (result != null)
			{
				viewName = result.ViewName;
			}

			var vcView = viewEngines.ResolveViewComponent(viewName, new ViewResolutionContext(viewContext.ActionContext));

			if (vcView.Successful)
			{
				try
				{
					using (var writer = new StringWriter())
					{
						vcView.ViewComponent.Process(viewContext, writer, instance);

						return writer.ToString();
					}
				}
				finally
				{
					vcView.ViewEngine.Release(vcView.View);
				}
			}
			else
			{
				throw new Exception("Could not find view " + component.Name + ". Searched at " + string.Join(", ", vcView.SearchedLocations));
			}
		}
	}
}
