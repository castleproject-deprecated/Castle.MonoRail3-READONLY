namespace Castle.MonoRail.WindsorIntegration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Web;
	using Mvc;
	using Mvc.ViewEngines;
	using Primitives.Mvc;
	using Windsor;

	[Export(typeof(ViewComponentProvider))]
	[ExportMetadata("Order", 1000)]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class WindsorViewComponentProvider : ViewComponentProvider
	{
		public override object Create(Type type, ViewContext viewContext)
		{
			var accessor = viewContext.HttpContext.ApplicationInstance as IContainerAccessor;

			if (accessor != null)
			{
				var container = accessor.Container;

				if (!container.Kernel.HasComponent(type)) return null;

				return container.Resolve(type, new Dictionary<string, object> {{"viewContext", viewContext}});
			}

			return null;
		}
	}
}
