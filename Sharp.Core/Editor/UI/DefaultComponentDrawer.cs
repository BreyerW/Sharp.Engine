using Newtonsoft.Json.Serialization;
using Sharp.Core;
using Sharp.Editor.UI.Property;
using Sharp.Editor.Views;
using Sharp.Serializer;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI
{
	static partial class Registerer
	{
		[ModuleInitializer]
		internal static void Register2()
		{
			InspectorView.RegisterDrawerFor<Component>(() => new DefaultComponentDrawer());
		}
	}
	public class DefaultComponentDrawer : ComponentDrawer<Component>
	{
		public override void OnInitializeGUI()//OnSelect
		{
			var type = Target.GetType();
			var contract = JSONSerializer.serializerSettings.ContractResolver.ResolveContract(type) as JsonObjectContract;
			foreach (var prop in contract.Properties)
			{
				if (prop.Ignored is false)
				{
					PropertyDrawer p = null;
					if (InspectorView.mappedPropertyDrawers.TryGetValue(prop.PropertyType, out var factory))
					{
						p = factory();
					}
					else if (prop.PropertyType.IsAssignableTo(typeof(IList))/*.GetInterfaces()
.Any(i => i == typeof(IList))*/)//isassignablefrom?
					{
						p = new ArrayDrawer();
					} //prop = Activator.CreateInstance(typeof(InvisibleSentinel<>).MakeGenericType(propertyInfo.GetUnderlyingType().IsByRef ? propertyInfo.GetUnderlyingType().GetElementType() : propertyInfo.GetUnderlyingType()), propertyInfo) as PropertyDrawer;
					else
						continue;
					//prop.attributes = attribs.ToArray();
					p.setter = prop.ValueProvider.SetValue;
					p.getter = prop.ValueProvider.GetValue;
					p.AutoSize = Squid.AutoSize.Horizontal;
					p.Target = Target;
					p.label.Text = prop.PropertyName + ": ";
					Frame.Controls.Add(p);
				}
			}
			/*while (type is not null)
            {
                var props = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => !p.IsStatic);//.Where(p => p.CanRead && (p.CanWrite || p.PropertyType.IsByRef));

                //var props = Target.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic);
                foreach (var prop in props)
                {
                    Console.WriteLine(prop.Name);
                    if (prop.GetCustomAttribute<NonSerializableAttribute>(false) != null) continue;

                    var propDrawer = InspectorView.Add(prop);
                    if (propDrawer is null)
                        continue;
                    propDrawer.Target = Target;
                    Frame.Controls.Add(propDrawer);
                }
                type = type.BaseType;
            }*/
		}
	}
}