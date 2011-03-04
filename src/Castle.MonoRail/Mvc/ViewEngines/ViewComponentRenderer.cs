namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Linq;
	using Primitives.Mvc;

	[Export(typeof(ViewComponentRenderer))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class ViewComponentRenderer
	{
		[ImportMany]
		public IEnumerable<Lazy<ViewComponentProvider, IOrderMeta>> ComponentProviders { get; set; }

		[Import]
		public IMonoRailServices Services { get; set; }

		public string Render(string componentView, ViewContext viewContext, object model = null)
		{
			return new PartialViewResult(componentView, model).Execute(viewContext, Services);
		}

		public string Render<T>(ViewContext viewContext, Action<T> configurer = null)
		{
			var component = typeof(T);

			dynamic instance = ResolveViewComponent(viewContext, component);

			if (configurer != null) configurer(instance);

			PartialViewResult result = instance.Render();

			return result.Execute(viewContext, Services, instance);
		}

		private object ResolveViewComponent(ViewContext viewContext, Type component)
		{
			foreach (var componentProvider in ComponentProviders.OrderBy(l => l.Metadata.Order))
			{
				var viewComponent = componentProvider.Value.Create(component, viewContext);

				if (viewComponent != null) return viewComponent;
			}

			throw new Exception("View Component could not be found.");
		}
	}
}
