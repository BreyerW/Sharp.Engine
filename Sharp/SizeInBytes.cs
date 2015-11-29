using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using System;

namespace Sharp
{
	public static class SizeInBytes
	{
		public static int Byte=Marshal.SizeOf (typeof(byte));
		public static int SByte=Marshal.SizeOf (typeof(sbyte));

		public static int Float=Marshal.SizeOf (typeof(float));
		public static int Double=Marshal.SizeOf (typeof(double));

		public static int UShort=Marshal.SizeOf (typeof(ushort));
		public static int Short=Marshal.SizeOf (typeof(short));
		public static int UInt=Marshal.SizeOf (typeof(uint));
		public static int Int=Marshal.SizeOf (typeof(int));


		public static int GetSizeInBytes(TexCoordPointerType pointerType){
			switch (pointerType) {
			case TexCoordPointerType.Float:
				return Float;
			case TexCoordPointerType.Double:
				return Double;
			case TexCoordPointerType.HalfFloat:
				return Float;
			}
			throw new NotSupportedException(pointerType+" is not supported for TexCoord pointer");
		}
		public static int GetSizeInBytes(ColorPointerType pointerType){
			switch (pointerType) {
			case ColorPointerType.UnsignedByte:
				return Byte;
			case ColorPointerType.Byte:
				return SByte;
			case ColorPointerType.HalfFloat:
				return Float;
			case ColorPointerType.Float:
				return Float;
			case ColorPointerType.Double:
				return Double;
			}
			throw new NotSupportedException(pointerType+" is not supported for Color pointer");
		}
		public static int GetSizeInBytes(VertexPointerType pointerType){
			switch (pointerType) {
			case VertexPointerType.Short:
				return Short;
			case VertexPointerType.Int:
				return Int;
			case VertexPointerType.HalfFloat:
				return Float;
			case VertexPointerType.Float:
				return Float;
			case VertexPointerType.Double:
				return Double;
			}
			throw new NotSupportedException(pointerType+" is not supported for Vertex pointer");
		}
		public static int GetSizeInBytes(NormalPointerType pointerType){
			switch (pointerType) {
			case NormalPointerType.Byte:
				return SByte;
			case NormalPointerType.Short:
				return Short;
			case NormalPointerType.Int:
				return Int;
			case NormalPointerType.HalfFloat:
				return Float;
			case NormalPointerType.Float:
				return Float;
			case NormalPointerType.Double:
				return Double;
			}
			throw new NotSupportedException(pointerType+" is not supported for Normal pointer");
		}
		public static int GetSizeInBytes(VertexAttribPointerType pointerType){
			switch (pointerType) {
			case VertexAttribPointerType.Byte:
				return SByte;
			case VertexAttribPointerType.Short:
				return Short;
			case VertexAttribPointerType.Int:
				return Int;
			case VertexAttribPointerType.HalfFloat:
				return Float;
			case VertexAttribPointerType.Float:
				return Float;
			case VertexAttribPointerType.Double:
				return Double;
			}
			throw new NotSupportedException(pointerType+" is not supported for VertexAttribPointer pointer");
		}
	}
}

