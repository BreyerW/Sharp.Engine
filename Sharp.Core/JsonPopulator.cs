
using Microsoft.Toolkit.HighPerformance.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sharp;
using Sharp.Core;
using Sharp.Engine.Components;
using Sharp.Serializer;
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
using System.Text.Json.Serialization;

public static class JsonPopulator
{

	public static object PopulateObject(this JsonSerializer serializer, object target, string jsonSource, Type type)
	{

		JsonTextReader reader = new JsonTextReader(new StringReader(jsonSource));
		reader.Read();
		if (reader.TokenType is JsonToken.StartObject)
			reader.Read();
		if (reader.Value is string and "$id" or "$ref")
		{
			reader.Read();
			reader.Read();
		}
		if (reader.Value is string and "$type")
		{
			reader.Read();
			reader.Read();
		}
		//if (reader.Value is string and "$ref")
		//just use resolvereference since $ref contains no data
		//	else
		while (reader.TokenType is not JsonToken.EndObject)
		{
			OverwriteProperty(serializer, target, reader, type);
		}
		reader.Read();
		(target as IJsonOnDeserialized)?.OnDeserialized();
		return target;
	}
	public static object PopulateObject(this JsonSerializer serializer, string jsonSource, Type type)
	{
		JsonTextReader reader = new JsonTextReader(new StringReader(jsonSource));
		var target = serializer.PopulateObject(reader, type);
		(target as IJsonOnDeserialized)?.OnDeserialized();
		return target;
	}
	public static object PopulateObject(this JsonSerializer serializer, JsonReader reader, Type type)
	{
		object target = null;
		if (type.IsPrimitive || type == typeof(DateTime) || type == typeof(decimal) || type == typeof(string))
		{
			var t = reader.Value;
			if (type == typeof(float))
				t = (float)(double)t;
			else if (type == typeof(int))
				t = (int)(long)t;
			else if (type == typeof(byte))
				t = (byte)(long)t;
			else if (type == typeof(uint))
				t = (uint)(long)t;
			reader.Read();
			return t;
		}
		else if (type.IsArray)
		{
			var arrConverter = FindCustomConverter(serializer, type);
			target = arrConverter.ReadJson(reader, type, null, serializer);
			reader.Read();
			return target;
		}
		reader.Read();
		if (reader.TokenType is JsonToken.StartObject)
			reader.Read();
		var id = string.Empty;
		var valueId = string.Empty;
		if (reader.Value is string and "$id" or "$ref")
		{
			id = reader.Value as string;
			reader.Read();
			valueId = reader.Value as string;
			reader.Read();
		}
		if (reader.Value is string and "$type")
		{
			reader.Read();
			reader.Read();
		}
		if (valueId is not null)
			target = serializer.ReferenceResolver.ResolveReference(serializer, valueId) ?? RuntimeHelpers.GetUninitializedObject(type);
		else
			target = RuntimeHelpers.GetUninitializedObject(type);
		//if (reader.Value is string and "$ref")
		//just use resolvereference since $ref contains no data
		//	else
		while (reader.TokenType is not JsonToken.EndObject)
		{
			OverwriteProperty(serializer, target, reader, type);
		}
		reader.Read();
		(target as IJsonOnDeserialized)?.OnDeserialized();
		return target;
	}
	private static Newtonsoft.Json.JsonConverter FindCustomConverter(JsonSerializer serializer, Type propertyType)
	{
		foreach (var conv in serializer.Converters)
			if (conv.CanConvert(propertyType))
				return conv;
		return null;
	}
	static void OverwriteProperty(JsonSerializer serializer, object target, JsonReader reader, Type type)
	{

		var propName = (string)reader.Value;
		reader.Read();
		object parsedValue = null;
		/*if (propName is "$ref")
		{
			parsedValue= updatedProperty.Value<Guid>()z.GetInstanceObject();
		}
		else*/
		var tmpType = type;
		var propertyInfo = tmpType.GetField(propName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		if (propertyInfo is null)
			while (tmpType.BaseType is not null && propertyInfo is null)
			{
				tmpType = tmpType.BaseType;
				propertyInfo = tmpType.GetField(propName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			}

		if (propertyInfo == null)
		{
			reader.Skip();
			reader.Read();
			return;
		}
		var contract = serializer.ContractResolver.ResolveContract(target.GetType()) as JsonObjectContract;
		var p = contract.Properties.GetClosestMatchProperty(propName);

		if (p.Ignored)
		{
			reader.Skip();
			reader.Read();
			return;
		}
		//we ignore InternalConverter because all of them have wrong deserialize support for reference handling. 
		var converter = contract.Converter;//contract.Converter is null ? contract.InternalConverter : 
		var propertyType = propertyInfo.FieldType;

		if (propertyType.IsPrimitive || propertyType == typeof(DateTime) || propertyType == typeof(decimal) || propertyType == typeof(string))
		{
			parsedValue = reader.Value;
			if (propertyType == typeof(float))
				parsedValue = (float)(double)parsedValue;

			reader.Read();
		}
		else if (propertyType.IsArray)
		{
			var arrConverter = FindCustomConverter(serializer, propertyType);
			parsedValue = arrConverter.ReadJson(reader, propertyType, null, serializer);
			reader.Read();
		}
		else
		{
			var customConverter = FindCustomConverter(serializer, propertyType) ?? converter;
			if (customConverter is null)
			{
				if (reader.TokenType is JsonToken.StartObject)
					reader.Read();
				var id = string.Empty;
				var valueId = string.Empty;
				if (reader.Value is string and "$id" or "$ref")
				{
					id = reader.Value as string;
					reader.Read();
					valueId = reader.Value as string;
					reader.Read();
				}
				if (reader.Value is string and "$type")
				{
					reader.Read();
					reader.Read();
				}
				if (propertyType.IsClass)
				{
					if (id is "$id")
					{
						var o = RuntimeHelpers.GetUninitializedObject(propertyType);
						serializer.ReferenceResolver.AddReference(null, valueId, o);
						parsedValue = o;
					}
					else
						parsedValue = serializer.ReferenceResolver.ResolveReference(null, valueId);
				}
				else
					parsedValue = RuntimeHelpers.GetUninitializedObject(propertyType);

				while (reader.TokenType is not JsonToken.EndObject)
				{
					OverwriteProperty(serializer, parsedValue, reader, propertyType);
				}
				(parsedValue as IJsonOnDeserialized)?.OnDeserialized();
				reader.Read();
			}
			else
			{
				parsedValue = customConverter.ReadJson(reader, propertyType, null, serializer);
			}
		}
		p.ValueProvider.SetValue(target, parsedValue);
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
