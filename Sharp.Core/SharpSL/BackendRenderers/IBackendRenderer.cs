using System;
using System.Collections.Generic;
using SharpAsset;

namespace SharpSL.BackendRenderers
{
	public enum TextureRole
	{
		Depth,
		Stencil,
		Color0,
		Color1,
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

		void Draw(IndiceType indiceType, int start, int length);

		void Use(int Program);

		void Delete(int Program, int VertexID, int FragmentID);

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

		void SetupGraphic();

		void ClearBuffer();

		void ClearColor();

		//int PropertyToID(string property);
		void ClearColor(float r, float b, float g, float a);

		void ClearDepth();

		void EnableScissor();

		void Viewport(int x, int y, int width, int height);

		void SetStandardState();

		void SetFlatColorState();

		void WriteDepth(bool enable = true);

		void Clip(int x, int y, int width, int height);

		void FinishCommands();

		byte[] ReadPixels(int x, int y, int width, int height, TextureFormat pxFormat);
	}
}