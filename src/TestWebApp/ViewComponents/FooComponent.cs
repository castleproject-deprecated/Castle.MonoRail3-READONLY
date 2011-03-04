namespace TestWebApp.ViewComponents
{
	using Castle.MonoRail;

	public class FooComponent
    {
		public string SomeProperty { get; set; }

		public PartialResult Render()
		{
			SomeProperty = "Some insightful and clever text here";

			return new PartialResult("alternative");
		}
    }
}