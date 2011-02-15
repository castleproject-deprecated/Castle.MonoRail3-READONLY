namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System;
	using System.ComponentModel.Composition;
	using System.IO;

	[Export(typeof(ViewComponentRenderer))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class ViewComponentRenderer
	{
		[Import]
		public IMonoRailServices Services { get; set; }

		public string Render(string componentView, ViewContext viewContext)
		{
			var viewEngines = Services.ViewEngines;

			var result = viewEngines.ResolveView(componentView, null, new ViewResolutionContext(viewContext.ActionContext));

			if (result.Successful)
			{
				try
				{
					using (var writer = new StringWriter())
					{
						result.View.Process(viewContext, writer);

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
	}
}
