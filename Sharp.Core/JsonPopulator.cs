
using Microsoft.Toolkit.HighPerformance.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sharp;
using Sharp.Core;
using Sharp.Engine.Components;
using SharpAsset;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public class JsonPopulator
{
	public
	   void PopulateObject<T>(T target, string jsonSource) where T : class =>
   PopulateObject(target, jsonSource, typeof(T));

	void OverwriteProperty<T>(T target, JToken updatedProperty) where T : class =>
		   OverwriteProperty(target, updatedProperty, typeof(T));

	public void PopulateObject(object target, string jsonSource, Type type)
	{
		var json = JObject.Parse(jsonSource);

		foreach (var property in json.Properties())
		{
			OverwriteProperty(target, property, type);
		}
	}
	private void PopulateObject(object target, JProperty property, Type type)
	{
		OverwriteProperty(target, property, type);
	}
	void OverwriteProperty(object target, JToken updatedProperty, Type type)
	{
		var propName = "";
		if (updatedProperty is JProperty prop)
			propName = prop.Name;
		else return;
		object parsedValue;
		/*if (propName is "$ref")
		{
			parsedValue= updatedProperty.Value<Guid>()z.GetInstanceObject();
		}
		else*/
		//var posfield = type.GetField("position", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		var propertyInfo = type.GetField(propName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);//
		{


			if (propertyInfo == null)
			{
				return;
			}

			var propertyType = propertyInfo.FieldType;


			if (propertyType.IsPrimitive || propertyType == typeof(decimal) || propertyType == typeof(string) || propertyType == typeof(DateTime) || propertyType == typeof(DateTimeOffset) || propertyType == typeof(Guid) || propertyType == typeof(TimeSpan) || propertyType == typeof(Vector3))//|| propertyType == typeof(Matrix4x4)
			{
				parsedValue =
					(updatedProperty as JProperty).Value.ToObject(propertyType);
			}
			else
			{

				parsedValue = propertyInfo.GetValue(target);
				var obj = ((JProperty)updatedProperty).Value as JObject;
				foreach (var item in obj.Properties())
				{
					PopulateObject(
						parsedValue,
						 item,
						propertyType);
				}
			}
		}
		if (target.GetType().IsValueType)
		{
			var serializer = new JsonSerializer();
			var contract = serializer.ContractResolver.ResolveContract(target.GetType()) as JsonObjectContract;
			var p = contract.Properties.GetClosestMatchProperty(propName);
			p.ValueProvider.SetValue(target, parsedValue);
		}
		else
			propertyInfo.SetValue(target, parsedValue);
	}
	[StructLayout(LayoutKind.Explicit)]
	private sealed class RawObjectData
	{
		[FieldOffset(0)]
#pragma warning disable SA1401 // Fields should be private
		public byte Data;
#pragma warning restore SA1401
	}
	/*public virtual void PopulateObject(object obj, string jsonData)
	{
		//options ??= new JsonSerializerOptions();
		//var state = new JsonReaderState(new JsonReaderOptions { AllowTrailingCommas = options.AllowTrailingCommas, CommentHandling = options.ReadCommentHandling, MaxDepth = options.MaxDepth });
		var reader = new JsonTextReader(new StringReader(jsonData));
		new Worker(this, reader, obj);
	}

	protected virtual PropertyInfo GetProperty(JsonTextReader reader, object obj, string propertyName)
	{
		if (obj == null)
			throw new ArgumentNullException(nameof(obj));

		if (propertyName == null)
			throw new ArgumentNullException(nameof(propertyName));

		var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic);
		return prop;
	}

	protected virtual bool SetPropertyValue(JsonTextReader reader, object obj, string propertyName)
	{
		if (obj == null)
			throw new ArgumentNullException(nameof(obj));

		if (propertyName == null)
			throw new ArgumentNullException(nameof(propertyName));
		if (propertyName is "$type")
			return false;

		object value = null;
		var prop = GetProperty(reader, obj, propertyName);
		if (prop == null)
			return false;

		if (propertyName is "$ref")
		{
			value = new Guid(reader.ReadAsString()).GetInstanceObject();
			if (value is null) return false;
		}
		else
		{
			if (!TryReadPropertyValue(reader, prop.PropertyType, out value))
				return false;
		}
		prop.SetValue(obj, value);
		return true;
	}

	protected virtual bool TryReadPropertyValue(JsonTextReader reader, Type propertyType, out object value)
	{
		if (propertyType == null)
			throw new ArgumentNullException(nameof(reader));

		if (reader.TokenType == JsonToken.Null)
		{
			value = null;
			return !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;
		}

		if (propertyType == typeof(object)) { value = ReadValue(reader); return true; }
		if (propertyType == typeof(string)) { value = reader.ReadAsString(); return true; }
		if (propertyType == typeof(int)) { value = reader.ReadAsInt32(); return true; }
		if (propertyType == typeof(long)) { value = reader.ReadAsInt32(); return true; }
		if (propertyType == typeof(DateTime)) { value = reader.ReadAsDateTime(); return true; }
		if (propertyType == typeof(DateTimeOffset)) { value = reader.ReadAsDateTimeOffset(); return true; }
		if (propertyType == typeof(Guid)) { value = reader.ReadAsBytes(); return true; }
		if (propertyType == typeof(decimal)) { value = reader.ReadAsDecimal(); return true; }
		if (propertyType == typeof(double)) { value = reader.ReadAsDouble(); return true; }
		if (propertyType == typeof(float)) { value = (float)reader.ReadAsDouble(); return true; }
		if (propertyType == typeof(uint)) { value = reader.ReadAsInt32(); return true; }
		if (propertyType == typeof(ulong)) { value = reader.ReadAsInt32(); return true; }
		if (propertyType == typeof(byte[])) { value = reader.ReadAsBytes(); return true; }

		if (propertyType == typeof(bool))
		{
			if (reader.TokenType == JsonToken.Boolean)
			{
				value = reader.ReadAsBoolean();
				return true;
			}
		}

		// fallback here
		return TryConvertValue(reader, propertyType, out value);
	}

	protected virtual object ReadValue(JsonTextReader reader)
	{
		switch (reader.TokenType)
		{
			case JsonToken.Boolean: return reader.ReadAsBoolean(); break;
			case JsonToken.Null: return null; break;
			case JsonToken.String: return reader.ReadAsString(); break;

			case JsonToken.Integer: // is there a better way?
				return reader.ReadAsInt32();
				/*if (reader.TryGetInt64(out var i64))
					return i64;

				if (reader.TryGetUInt64(out var ui64)) // uint is already handled by i64
					return ui64;*

				break;
			case JsonToken.Float:
				//if (reader.TryGetSingle(out var sgl))
				//	return sgl;

				//if (reader.TryGetDouble(out var dbl))
				//	return dbl;

				//if (reader.TryGetDecimal(out var dec))
				//	return dec;
				if (reader.ValueType == typeof(float))
					return (float)reader.ReadAsDouble();
				return reader.ReadAsDouble();
				break;

		}
		throw new NotSupportedException();
	}

	// we're here when json types & property types don't match exactly
	protected virtual bool TryConvertValue(JsonTextReader reader, Type propertyType, out object value)
	{
		if (propertyType == null)
			throw new ArgumentNullException(nameof(reader));

		if (propertyType == typeof(bool))
		{
			//if () // one size fits all
			{
				value = reader.ReadAsInt32() != 0;
				return true;
			}
		}

		// TODO: add other conversions

		value = null;
		return false;
	}

	protected virtual object CreateInstance(JsonTextReader reader, Type propertyType)
	{
		if (propertyType.GetConstructor(Type.EmptyTypes) == null)
			return null;

		// TODO: handle custom instance creation
		try
		{
			return Activator.CreateInstance(propertyType);
		}
		catch
		{
			// swallow
			return null;
		}
	}

	private class Worker
	{
		private readonly Stack<WorkerProperty> _properties = new Stack<WorkerProperty>();
		private readonly Stack<object> _objects = new Stack<object>();

		public Worker(JsonPopulator populator, JsonTextReader reader, object obj)
		{
			_objects.Push(obj);
			WorkerProperty prop;
			WorkerProperty peek;
			while (reader.Read())
			{
				switch (reader.TokenType)
				{
					case JsonToken.PropertyName:
						prop = new WorkerProperty();
						prop.PropertyName = reader.ReadAsString();
						if (prop.PropertyName is not "$ref" and not "$type")
							_properties.Push(prop);
						break;

					case JsonToken.StartObject:
					case JsonToken.StartArray:
						if (_properties.Count > 0)
						{
							object child = null;
							var parent = _objects.Peek();
							PropertyInfo pi = null;
							if (parent != null)
							{
								pi = populator.GetProperty(reader, parent, _properties.Peek().PropertyName);
								if (pi != null)
								{
									child = pi.GetValue(parent); // mimic ObjectCreationHandling.Auto
									if (child == null && pi.CanWrite)
									{
										if (reader.TokenType == JsonToken.StartArray)
										{
											if (!typeof(IList).IsAssignableFrom(pi.PropertyType))
												break;  // don't create if we can't handle it
										}

										if (reader.TokenType == JsonToken.StartArray && pi.PropertyType.IsArray)
										{
											child = Activator.CreateInstance(typeof(List<>).MakeGenericType(pi.PropertyType.GetElementType())); // we can't add to arrays...
										}
										else
										{
											child = populator.CreateInstance(reader, pi.PropertyType);
											if (child != null)
											{
												pi.SetValue(parent, child);
											}
										}
									}
								}
							}

							if (reader.TokenType == JsonToken.StartObject)
							{
								_objects.Push(child);
							}
							else if (child != null) // StartArray
							{
								peek = _properties.Peek();
								peek.IsArray = pi.PropertyType.IsArray;
								peek.List = (IList)child;
								peek.ListPropertyType = GetListElementType(child.GetType());
								peek.ArrayPropertyInfo = pi;
							}
						}
						break;

					case JsonToken.EndObject:
						_objects.Pop();
						if (_properties.Count > 0)
						{
							_properties.Pop();
						}
						break;

					case JsonToken.EndArray:
						if (_properties.Count > 0)
						{
							prop = _properties.Pop();
							if (prop.IsArray)
							{
								var array = Array.CreateInstance(GetListElementType(prop.ArrayPropertyInfo.PropertyType), prop.List.Count); // array is finished, convert list into a real array
								prop.List.CopyTo(array, 0);
								prop.ArrayPropertyInfo.SetValue(_objects.Peek(), array);
							}
						}
						break;

					case JsonToken.Boolean:
					case JsonToken.Null:
					case JsonToken.Float:
					case JsonToken.Integer:
					case JsonToken.String:
						peek = _properties.Peek();
						if (peek.List != null)
						{
							if (populator.TryReadPropertyValue(reader, peek.ListPropertyType, out var item))
							{
								peek.List.Add(item);
							}
							break;
						}

						prop = _properties.Pop();
						var current = _objects.Peek();
						if (current != null)
						{
							populator.SetPropertyValue(reader, current, prop.PropertyName);
						}
						break;
				}
			}
		}

		private static Type GetListElementType(Type type)
		{
			if (type.IsArray)
				return type.GetElementType();

			foreach (Type iface in type.GetInterfaces())
			{
				if (!iface.IsGenericType) continue;
				if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>)) return iface.GetGenericArguments()[1];
				if (iface.GetGenericTypeDefinition() == typeof(IList<>)) return iface.GetGenericArguments()[0];
				if (iface.GetGenericTypeDefinition() == typeof(ICollection<>)) return iface.GetGenericArguments()[0];
				if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return iface.GetGenericArguments()[0];
			}
			return typeof(object);
		}
	}

	private class WorkerProperty
	{
		public string PropertyName;
		public IList List;
		public Type ListPropertyType;
		public bool IsArray;
		public PropertyInfo ArrayPropertyInfo;

		public override string ToString() => PropertyName;
	}*/
}
