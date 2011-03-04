namespace Castle.MonoRail.Mvc.ViewEngines
{
	using System.IO;

	public interface IPartialView
	{
		void Process(ViewContext viewContext, TextWriter writer, object model);
	}
}
