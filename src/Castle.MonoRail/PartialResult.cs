namespace Castle.MonoRail
{
	using System;
	using System.IO;
	using Extensions;
	using Mvc.ViewEngines;

	public class PartialResult
	{
		public PartialResult(string partialViewName = null, object model = null)
		{
			Model = model;

			PartialViewName = partialViewName;
		}

		private object Model { get; set; }

		public string PartialViewName { get; private set; }

		public string Execute(ViewContext viewContext, IMonoRailServices services, object viewComponent = null)
		{
			ApplyConventions(viewComponent);

			var result = services.ViewEngines.ResolvePartialView(PartialViewName, ResolvePartialResolutionContext(viewContext, viewComponent));

			if (result.Successful)
			{
				try
				{
					using (var writer = new StringWriter())
					{
						result.ViewComponent.Process(viewContext, writer, Model);

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
				throw new Exception("Could not find view " + PartialViewName + ". Searched at " + string.Join(", ", result.SearchedLocations));
			}
		}

		private PartialResolutionContext ResolvePartialResolutionContext(ViewContext viewContext, object viewComponent)
		{
			if (viewComponent != null)
				return new PartialResolutionContext(null, viewComponent.GetType().Name.RemoveSufix("Component"), PartialViewName, false);

			return new PartialResolutionContext(viewContext.ActionContext);
		}

		private void ApplyConventions(object viewComponent)
		{
			if (viewComponent == null) return;

			if (string.IsNullOrEmpty(PartialViewName))
				PartialViewName = "default";

			if (Model == null)
				Model = viewComponent;
		}
	}
}
