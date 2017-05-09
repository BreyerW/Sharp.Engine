using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using Sharp;

namespace SharpAsset
{
    [AttributeUsage(AttributeTargets.Field)]
    public class RegisterAsAttribute : Attribute
    {
        public static Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>> registeredVertexFormats = new Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>>();

        public IntPtr offset;

        public int Dimension
        {
            get
            {
                switch (format)
                {
                    case VertexAttribute.POSITION:
                        return 3;//generatedFillers.Count;
                    case VertexAttribute.COLOR:
                        return 4;

                    case VertexAttribute.UV:
                        return 2;

                    case VertexAttribute.NORMAL:
                        return 3;
                }
                throw new InvalidOperationException(nameof(format) + " have wrong value");
            }
        }

        public int shaderLocation;
        public VertexAttribute format;
        public VertexType type;
        public List<Action<IVertex, object>> generatedFillers = new List<Action<IVertex, object>>();

        public RegisterAsAttribute(VertexAttribute Format, VertexType Type)
        {
            format = Format;

            switch (format)
            {
                case VertexAttribute.POSITION:
                    shaderLocation = 0;
                    break;

                case VertexAttribute.COLOR:
                    shaderLocation = 1;
                    break;

                case VertexAttribute.UV:
                    shaderLocation = 2;
                    break;

                case VertexAttribute.NORMAL:
                    shaderLocation = 3;
                    break;
            }
            type = Type;
        }

        public static void ParseVertexFormat(Type type)
        {
            var fields = type.GetFields().Where(
                p => p.GetCustomAttribute<RegisterAsAttribute>() != null);
            int? lastFormat = null;
            var vertFormat = new Dictionary<VertexAttribute, RegisterAsAttribute>();

            foreach (var field in fields)
            {
                var attrib = field.GetCustomAttribute<RegisterAsAttribute>();
                if (lastFormat != (int)attrib.format)
                {
                    lastFormat = (int)attrib.format;
                    attrib.offset = Marshal.OffsetOf(type, field.Name);
                    attrib.generatedFillers = new List<Action<IVertex, object>>() { DelegateGenerator.GenerateSetter<IVertex>(field) };
                    vertFormat.Add(attrib.format, attrib);
                }
                else if (attrib.format == VertexAttribute.POSITION)
                {
                    //	dim++; //error prone
                    vertFormat[attrib.format].generatedFillers.Add(DelegateGenerator.GenerateSetter<IVertex>(field));
                }
                //else if (attrib.format == VertexAttribute.UV)
                //	numOfUV++;
            }
            registeredVertexFormats.Add(type, vertFormat);
        }
    }
}