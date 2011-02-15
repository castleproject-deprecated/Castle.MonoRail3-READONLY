namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System;
	using System.ComponentModel.Composition;
	using System.IO;
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
			var viewEngines = Services.ViewEngines;

			var result = viewEngines.ResolveViewComponent(component.Name.Replace("Component", ""), new ViewResolutionContext(viewContext.ActionContext));

			if (result.Successful)
			{
				try
				{
					using (var writer = new StringWriter())
					{
						result.ViewComponent.Process(viewContext, writer, ComponentProvider.Create(component).ComponentInstance);

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
				throw new Exception("Could not find view " + component.Name + ". Searched at " + string.Join(", ", result.SearchedLocations));
			}
		}
	}
}
