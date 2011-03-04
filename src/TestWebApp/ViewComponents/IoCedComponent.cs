namespace TestWebApp.ViewComponents
{
	using Castle.MonoRail;
	using Castle.MonoRail.Mvc.ViewEngines;

	public class IoCedComponent
	{
		private readonly ViewContext viewContext;

		public IoCedComponent(ViewContext viewContext)
		{
			this.viewContext = viewContext;
		}

		public PartialViewResult Render()
		{
			return new PartialViewResult();
		}
	}
}