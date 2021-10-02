using System;
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
    public enum AttributeType
    {
        Byte = 5120,
        UnsignedByte,
        Short,
        UnsignedShort,
        Int,
        UnsignedInt,
        Float,
        Double = 5130,
        HalfFloat,
        Fixed,
        UnsignedInt2101010Rev = 33640,
        Int2101010Rev = 36255
    }
    public interface IBackendRenderer
    {
        uint currentWindow { get; set; }
        IntPtr CreateContext(Func<string, IntPtr> GetProcAddress, Func<IntPtr> GetCurrentContext);

        Func<IntPtr, IntPtr, int> MakeCurrent { get; set; }
        Action<IntPtr> SwapBuffers { get; set; }

        void Start();

        //void Do(Work whatToDo, ref Shader shader);
        //void Do<IndexType> (Work whatToDo,ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible;
        void Allocate(Target target, UsageHint usageHint, ref byte addr, int length, bool reuse = false);

        void Allocate(int Program, int VertexID, int FragmentID, string VertexSource, string FragmentSource, Dictionary<string, int> uniformArray, Dictionary<string, (int location, int size)> attribArray);

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


        void BindVertexAttrib(AttributeType type, int shaderLoc, int dim, int stride, int offset);//vertextype and IndicesType move to SL

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
