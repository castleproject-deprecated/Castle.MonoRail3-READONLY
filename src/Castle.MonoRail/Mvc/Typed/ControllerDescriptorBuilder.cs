#region License
//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

namespace Castle.MonoRail.Mvc.Typed
{
	using System;
	using System.ComponentModel.Composition;
	using System.Collections.Concurrent;
	using System.Reflection;

	// [Export]
	// [PartCreationPolicy(CreationPolicy.Shared)]
	public class ControllerDescriptorBuilder
	{
		private readonly ConcurrentDictionary<Type, ControllerDescriptor> _type2Descriptor = new ConcurrentDictionary<Type, ControllerDescriptor>();

		public virtual ControllerDescriptor Build(Type controllerType)
		{
			ControllerDescriptor desc;
			if (_type2Descriptor.TryGetValue(controllerType, out desc))
				return desc;

			string name = controllerType.Name;
			
			name = name.Substring(0, name.Length - "Controller".Length).ToLowerInvariant();

			var controllerDesc = new ControllerDescriptor(controllerType, name, string.Empty);

			foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
			{
				var action = BuildActionDescriptor(method);

				if (action != null)
					controllerDesc.Actions.Add(action);
			}

			_type2Descriptor.TryAdd(controllerType, controllerDesc);

			return controllerDesc;
		}

		private static ActionDescriptor BuildActionDescriptor(MethodInfo method)
		{
			if (method.IsSpecialName ||
				method.IsGenericMethodDefinition || method.IsStatic ||
				method.DeclaringType == typeof(object))
			{
				return null;
			}

			return new MethodInfoActionDescriptor(method);
		}
	}
}
