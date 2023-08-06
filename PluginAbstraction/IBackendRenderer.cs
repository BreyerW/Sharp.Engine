﻿using System;
using System.Collections.Generic;

namespace PluginAbstraction
{
    public enum UsageHint
    {
        StreamDraw = 35040,
        StreamRead,
        StreamCopy,
        StaticDraw = 35044,
        StaticRead,
        StaticCopy,
        DynamicDraw = 35048,
        DynamicRead,
        DynamicCopy
    }
    public enum TextureFormat
    {
        R,
        RGB,
        RGBA,
        RUInt,
        RGUInt,
        RGBUInt,
        RGBAUInt,
        A,
        RGBAFloat,
        RG16_SNorm,
        DepthFloat,
    }
    public enum Target
    {
        Texture,
        Shader,
        VertexShader,
        FragmentShader,
        Mesh,
        Indices,
        Frame,
        WriteFrame,
        ReadFrame,
        OcclusionQuery,
    }
    public enum BlendEquation
    {
        None,
        Default,
        One,
        OneMinuSrcAlpha,
        SrcAlpha
    }
    public enum TextureRole
    {
        Depth,
        Stencil,
        Color0,
        Color1,
    }
    public enum DepthFunc
    {
        Never,
        Less,
        Lequal,
        Equal,
        NotEqual,
        Gequal,
        Greater,
        Always
    }
    [Flags]
    public enum RenderState
    {
        DepthTest = 1 << 1,
        DepthMask = 1 << 2,
        Blend = 1 << 3,
        Texture2D = 1 << 4,
        CullFace = 1 << 5,
        CullBack = 1 << 6,
        ScissorTest = 1 << 7
    }

	public enum ParameterType
	{
		FLOAT,
		INT,
		VECTOR2,
		VECTOR3,
		VECTOR4,
		COLOR3,
		COLOR4,
		MATRIX16,
		FLOAT_ARRAY,
		INT_ARRAY,
		VECTOR2_ARRAY,
		VECTOR3_ARRAY,
		VECTOR4_ARRAY,
		COLOR3_ARRAY,
		COLOR4_ARRAY,
		MATRIX16_ARRAY,
		TEXTURE,
		MESH,
	}
	public interface IBackendRenderer
	{
		/*public const byte FLOAT = 0;
		public const byte VECTOR2 = 1;
		public const byte VECTOR3 = 2;
		public const byte MATRIX4X4 = 3;
		public const byte TEXTURE = 4;
		public const byte MESH = 5;
		public const byte COLOR4 = 6;
		public const byte UVECTOR2 = 7;
		public const byte MATRIX4X4PTR = byte.MaxValue;
		public const byte COLOR4PTR = byte.MaxValue - 1;*/

		uint currentWindow { get; set; }
        IntPtr CreateContext(Func<string, IntPtr> GetProcAddress, Func<IntPtr> GetCurrentContext);

        Func<IntPtr, IntPtr, int> MakeCurrent { get; set; }
        Action<IntPtr> SwapBuffers { get; set; }

		T ConvertParameterToAttributeType<T>(ParameterType type) where T : struct, Enum;
		ParameterType ConvertAttributeToParameterType<T>(T type) where T : struct, Enum;
		void Start();

        //void Do(Work whatToDo, ref Shader shader);
        //void Do<IndexType> (Work whatToDo,ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
        void Allocate(Target target, UsageHint usageHint, ref byte addr, int length, bool reuse = false);

        void Allocate(int Program, int VertexID, int FragmentID, string VertexSource, string FragmentSource, Dictionary<string, int> uniformArray, Dictionary<int, (ParameterType location, int size)> attribArray);

        void Allocate(ref byte bitmap, int width, int height, TextureFormat pixelFormat);

        void Draw(int indexStride, int start, int length);

        void Use(int Program);
        void GetQueryResult(int id, out int result);
        void DeleteBuffers(Target target, Span<int> id);
        void DeleteBuffer(Target target, int id);
        void SendMatrix4(int location, ref byte mat);

        //void Send(ref int location,ref int[] i);
        public void BindRenderTexture(int tbo, TextureRole role);
        void SendTexture2D(int location, ref byte tbo/*, int slot*/);

        void SendUniform1(int location, ref byte data);

        void SendUniformFloat2(int location, ref byte data);
        void SendUniformUInt2(int location, ref byte data);
        void SendUniform3(int location, ref byte data);

        void SendUniform4(int location, ref byte data);//TODO: int stride to differentiate floats ints etc?

        //void StoreGlobalUniform4(int location, ref byte data); //TODO: move shader and material storage to backend renderer?
        void GenerateBuffers(Target target, Span<int> id);

        void BindBuffers(Target target, int TBO);


        void BindVertexAttrib(ParameterType type, int shaderLoc, int dim, int stride, int offset);//vertextype and IndicesType move to SL

        void ClearBuffer();

        void ClearColor();

        //int PropertyToID(string property);
        void ClearColor(float r, float b, float g, float a);

        void ClearDepth();

        void Viewport(int x, int y, int width, int height);

        void Clip(int x, int y, int width, int height);


        byte[] ReadPixels(int x, int y, int width, int height, TextureFormat pxFormat);
        void SetColorMask(bool r, bool g, bool b, bool a);
        void SetBlendState(BlendEquation dstColor, BlendEquation srcColor, BlendEquation dstAlpha, BlendEquation srcAlpha);
        void SetDepthFunc(DepthFunc func);
        void EnableState(RenderState state);
        void DisableState(RenderState state);
        QueryScope StartQuery(Target target, int id);
        void EndQuery(Target target);
	}
	//public interface IBackendRenderer<T> where T : struct, Enum {
	//T ConvertParameterToAttributeType(ParameterType type);
	//ParameterType ConvertAttributeToParameterType(T type);
	//}


	public readonly ref struct QueryScope
    {
        private readonly IBackendRenderer renderer;
        private readonly Target target;
        public QueryScope(IBackendRenderer r, Target t)
        {
            renderer = r;
            target = t;
        }
        public void Dispose()
        {
            renderer.EndQuery(target);
        }
    }
}
