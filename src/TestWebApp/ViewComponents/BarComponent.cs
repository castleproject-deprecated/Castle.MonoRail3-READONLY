namespace TestWebApp.ViewComponents
{
	using System;
	using System.Web.WebPages;
	using Castle.MonoRail;

	public class BarComponent
	{
		public string SomeProperty { get; set; }

		public Func<BarComponent, HelperResult> Section { get; set; }

		public PartialResult Render()
		{
			return new PartialResult();
		}
	}
}