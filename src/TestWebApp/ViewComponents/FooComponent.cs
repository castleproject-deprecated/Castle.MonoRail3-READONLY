namespace TestWebApp.ViewComponents
{
	using Castle.MonoRail;

	public class FooComponent
    {
		public string SomeProperty { get; set; }

		public ViewResult Render()
		{
			SomeProperty = "Some insightful and clever text here";

			return new ViewResult("Foo");
		}
    }
}