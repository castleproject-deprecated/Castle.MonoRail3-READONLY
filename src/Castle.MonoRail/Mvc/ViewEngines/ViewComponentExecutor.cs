namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System;
	using System.ComponentModel.Composition;
	using Primitives.Mvc;

	[Export(typeof(ViewComponentExecutor))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class ViewComponentExecutor
	{
		[Import]
		public ViewComponentProvider ComponentProvider { get; set; }

		[Import]
		public IMonoRailServices Services { get; set; }

		public string Render(string componentView, ViewContext viewContext, object model = null)
		{
			return new PartialResult(componentView, model).Execute(viewContext, Services);
		}

		public string Render<T>(ViewContext viewContext, Action<T> configurer = null)
		{
			var component = typeof(T);

			dynamic instance = ComponentProvider.Create(component).ComponentInstance;

			if (configurer != null) configurer(instance);

			PartialResult result = instance.Render();

			return result.Execute(viewContext, Services, instance);
		}
	}
}
