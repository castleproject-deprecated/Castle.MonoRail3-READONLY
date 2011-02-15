namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System.IO;

	public interface IViewComponent
	{
		void Process(ViewContext viewContext, TextWriter writer, object model);
	}
}
