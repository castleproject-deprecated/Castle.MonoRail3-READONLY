namespace Castle.MonoRail.Tests.Mvc.Typed
{
	using System.Linq;
	using Fakes;
	using MonoRail.Mvc.Typed;
	using NUnit.Framework;

	[TestFixture]
	public class CecilBasedControllerDescriptorBuilderTestCase
	{
		[Test]
		public void Build_should_inspect_controller_type_to_collect_and_normalize_name()
		{
			var builder = new CecilReflectionMod2.CecilBasedControllerDescriptorBuilder();

			var descriptor = builder.Build(typeof (SomeTestController));

			Assert.AreEqual("sometest", descriptor.Name);
		}

		[Test]
		public void Build_should_inspect_controller_type_to_collect_actions()
		{
			var builder = new CecilReflectionMod2.CecilBasedControllerDescriptorBuilder();

			var descriptor = builder.Build(typeof(SomeTestController));

			Assert.IsTrue(descriptor.Actions.Any(a => a.Name == "Index"));
		}

		[Test]
		public void Actions_should_reflect_parameters()
		{
			var builder = new CecilReflectionMod2.CecilBasedControllerDescriptorBuilder();

			var descriptor = builder.Build(typeof(SomeTestController));

			var action = descriptor.Actions.First(a => a.Name == "ActionWithArgs");

			Assert.IsNotNull(action);
			Assert.AreEqual("ActionWithArgs", action.Name);
			Assert.AreEqual(2, action.Parameters.Count);

			Assert.AreEqual("id", action.Parameters.ElementAt(0).Name);
			Assert.AreEqual(typeof(int), action.Parameters.ElementAt(0).Type);

			Assert.AreEqual("key", action.Parameters.ElementAt(1).Name);
			Assert.AreEqual(typeof(string), action.Parameters.ElementAt(1).Type);
		}

		[Test]
		public void Delegate_is_invocable_for_void_return_actions()
		{
			var builder = new CecilReflectionMod2.CecilBasedControllerDescriptorBuilder();

			var descriptor = builder.Build(typeof(InvocableTestController));

			var action = descriptor.Actions.First(a => a.Name == "Index");

			Assert.IsNotNull(action);
			Assert.AreEqual("Index", action.Name);
			Assert.AreEqual(0, action.Parameters.Count);

			var cont = new InvocableTestController();
			var result = action.Action(cont, new object[0]);
			Assert.IsNull(result);
			Assert.IsTrue(cont.IndexCalled);
		}
		
		[Test]
		public void Delegate_is_invocable_for_nonvoid_return_actions()
		{
			var builder = new CecilReflectionMod2.CecilBasedControllerDescriptorBuilder();

			var descriptor = builder.Build(typeof(InvocableTestController));

			var action = descriptor.Actions.First(a => a.Name == "ActionWithReturn");

			Assert.IsNotNull(action);
			Assert.AreEqual("ActionWithReturn", action.Name);
			Assert.AreEqual(0, action.Parameters.Count);

			var cont = new InvocableTestController();
			var result = action.Action(cont, new object[0]);
			Assert.IsNotNull(result);
			Assert.IsTrue(cont.ActionWithReturnCalled);
		}
	}

	public class InvocableTestController
	{
		public bool IndexCalled;
		public bool ActionWithReturnCalled;

		public void Index()
		{
			IndexCalled = true;
		}

		public object ActionWithReturn()
		{
			ActionWithReturnCalled = true;
			return "";
		}
	}
}
