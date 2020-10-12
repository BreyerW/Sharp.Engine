using System;
using System.IO;
using System.Collections.Generic;
using Sharp;
using Sharp.Editor.Views;
using System.Numerics;

namespace SharpAsset
{
	[Serializable]
	public struct Shader : IAsset//move backendrenderer here?
	{
		public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }

		public string FullPath { get; set; }
		public bool IsAllocated => !(Program is -1);
		public string VertexSource;
		public string FragmentSource;

		internal int VertexID;
		internal int FragmentID;

		internal int Program;

		internal Dictionary<string, int> uniformArray;//=new Dictionary<UniformType, Dictionary<string, int>> ();
		internal Dictionary<string, (int location, int size)> attribArray;
		//internal static Dictionary<string, int> globalUniformArray;

		public override string ToString()
		{
			return Name.ToString();
		}

		public void Dispose()
		{
			MainWindow.backendRenderer.Delete(Program, VertexID, FragmentID);
		}

		public void PlaceIntoScene(Entity context, Vector3 worldPos)
		{
		}
	}

	public enum UniformType
	{
		Int = 5124,
		UnsignedInt,
		Float,
		Double = 5130,
		FloatVec2 = 35664,
		FloatVec3,
		FloatVec4,
		IntVec2,
		IntVec3,
		IntVec4,
		Bool,
		BoolVec2,
		BoolVec3,
		BoolVec4,
		FloatMat2,
		FloatMat3,
		FloatMat4,
		Sampler1D,
		Sampler2D,
		Sampler3D,
		SamplerCube,
		Sampler1DShadow,
		Sampler2DShadow,
		Sampler2DRect,
		Sampler2DRectShadow,
		FloatMat2x3,
		FloatMat2x4,
		FloatMat3x2,
		FloatMat3x4,
		FloatMat4x2,
		FloatMat4x3,
		Sampler1DArray = 36288,
		Sampler2DArray,
		SamplerBuffer,
		Sampler1DArrayShadow,
		Sampler2DArrayShadow,
		SamplerCubeShadow,
		UnsignedIntVec2,
		UnsignedIntVec3,
		UnsignedIntVec4,
		IntSampler1D,
		IntSampler2D,
		IntSampler3D,
		IntSamplerCube,
		IntSampler2DRect,
		IntSampler1DArray,
		IntSampler2DArray,
		IntSamplerBuffer,
		UnsignedIntSampler1D,
		UnsignedIntSampler2D,
		UnsignedIntSampler3D,
		UnsignedIntSamplerCube,
		UnsignedIntSampler2DRect,
		UnsignedIntSampler1DArray,
		UnsignedIntSampler2DArray,
		UnsignedIntSamplerBuffer,
		DoubleVec2 = 36860,
		DoubleVec3,
		DoubleVec4,
		SamplerCubeMapArray = 36876,
		SamplerCubeMapArrayShadow,
		IntSamplerCubeMapArray,
		UnsignedIntSamplerCubeMapArray,
		Image1D = 36940,
		Image2D,
		Image3D,
		Image2DRect,
		ImageCube,
		ImageBuffer,
		Image1DArray,
		Image2DArray,
		ImageCubeMapArray,
		Image2DMultisample,
		Image2DMultisampleArray,
		IntImage1D,
		IntImage2D,
		IntImage3D,
		IntImage2DRect,
		IntImageCube,
		IntImageBuffer,
		IntImage1DArray,
		IntImage2DArray,
		IntImageCubeMapArray,
		IntImage2DMultisample,
		IntImage2DMultisampleArray,
		UnsignedIntImage1D,
		UnsignedIntImage2D,
		UnsignedIntImage3D,
		UnsignedIntImage2DRect,
		UnsignedIntImageCube,
		UnsignedIntImageBuffer,
		UnsignedIntImage1DArray,
		UnsignedIntImage2DArray,
		UnsignedIntImageCubeMapArray,
		UnsignedIntImage2DMultisample,
		UnsignedIntImage2DMultisampleArray,
		Sampler2DMultisample = 37128,
		IntSampler2DMultisample,
		UnsignedIntSampler2DMultisample,
		Sampler2DMultisampleArray,
		IntSampler2DMultisampleArray,
		UnsignedIntSampler2DMultisampleArray,
		UnsignedIntAtomicCounter = 37595
	}
}