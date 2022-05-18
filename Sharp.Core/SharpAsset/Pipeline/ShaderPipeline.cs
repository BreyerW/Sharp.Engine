using PluginAbstraction;
using Sharp;
using Sharp.Core;
using SharpSL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SharpAsset.AssetPipeline
{
	[SupportedFiles(".shader", ".glsl")]
	public class ShaderPipeline : Pipeline<Shader>
	{
		[ModuleInitializer]
		internal static void LoadPipeline()
		{
			allPipelines.Add(typeof(ShaderPipeline).BaseType, instance);
			extensionToTypeMapping.Add(".shader", typeof(ShaderPipeline).BaseType);
			extensionToTypeMapping.Add(".glsl", typeof(ShaderPipeline).BaseType);
		}

		public static readonly ShaderPipeline instance = new();
		protected override ref Shader ImportInternal(string pathToFile) //Change IAsset to INT
		{
			//var format = Path.GetExtension (pathToFile);
			//if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
			//throw new NotSupportedException (format+" format is not supported");
			ref var asset = ref base.ImportInternal(pathToFile);
			if (Unsafe.IsNullRef(ref asset) is false) return ref asset;

			List<string> shaderStringBuffer = new List<string>();
			using StreamReader shaderFile = new StreamReader(pathToFile);
			while (shaderFile.Peek() >= 0)
			{
				shaderStringBuffer.Add(shaderFile.ReadLine());
			}
			var shader = SplitShaders(shaderStringBuffer, ref pathToFile);
			shader.Program = -1;
			return ref this[Register(shader)];
		}

		#region SplitShaders

		/// <summary>
		/// Splits the shaders.
		/// </summary>
		/// <param name="shaderBuffer">The shader buffer.</param>
		private Shader SplitShaders(List<string> shaderBuffer, ref string name)
		{
			int vertexShaderOffset = 0, fragmentShaderOffset = 0, lineCount = 0;
			string version = "";
			ReadOnlySpan<char> blend = "";
			bool blendFound = false;
			foreach (string line in shaderBuffer)
			{
				if (line.Contains("#pragma vertex"))
				{
					vertexShaderOffset = lineCount + 1;
				}
				else if (line.Contains("#pragma fragment"))
				{
					fragmentShaderOffset = lineCount + 1;
				}
				else if (line.Contains("#version"))
				{
					version = line;
				}
				else if (line.Contains("#pragma enable blend"))
				{
					blend = line.AsSpan()["#pragma enable blend".Length..];
					blendFound = true;
				}
				lineCount++;
			}

			List<string> vertexShaderBuffer =
				shaderBuffer.GetRange(vertexShaderOffset, fragmentShaderOffset - vertexShaderOffset);
			vertexShaderBuffer.Insert(0, version);
			List<string> fragmentShaderBuffer =
				shaderBuffer.GetRange(fragmentShaderOffset, lineCount - fragmentShaderOffset);
			fragmentShaderBuffer.Insert(0, version);
			using (StreamReader pickingSupportFileReader = new StreamReader(Application.projectPath + "/Content/EditorPickingShader.shader"))
			{
				fragmentShaderBuffer.Insert(1, pickingSupportFileReader.ReadToEnd());
			}
			ProcessIncludes(vertexShaderBuffer, out var vertexShader);
			ProcessIncludes(fragmentShaderBuffer, out var fragmentShader);

			var dstColor = BlendEquation.None;
			var srcColor = BlendEquation.None;
			var dstAlpha = BlendEquation.None;
			var srcAlpha = BlendEquation.None;

			if (blendFound && blend.Length is not 0)
			{

			}
			else if (blendFound)
			{
				dstColor = BlendEquation.Default;
				srcColor = BlendEquation.Default;
				dstAlpha = BlendEquation.Default;
				srcAlpha = BlendEquation.Default;
			}

			var shader = new Shader()
			{
				uniformArray = new Dictionary<string, int>(),
				attribArray = new Dictionary<string, (int, int)>(),
				FullPath = name,
				VertexSource = vertexShader,
				FragmentSource = fragmentShader,
				dstColor = dstColor,
				srcColor = srcColor,
				dstAlpha = dstAlpha,
				srcAlpha = srcAlpha,
			};
			return shader;
		}

		#endregion SplitShaders

		#region ProcessIncludes

		/// <summary>
		/// Processes the includes.
		/// </summary>
		/// <param name="shaderBuffer">The shader buffer.</param>
		/// <param name="shaderOut">The shader out.</param>
		private void ProcessIncludes(List<string> shaderBuffer, out string shaderOut)
		{
			string finalShader = string.Empty;

			foreach (string line in shaderBuffer)
			{
				if (line.Contains("#pragma include"))
				{
					var splitLine = line.Split(' ');//TODO: spanify
					string includeFilename = $"{splitLine[2]}";
					finalShader += $"// included from source file {includeFilename}\n\r";
					using StreamReader includeFileReader = new StreamReader(includeFilename);
					finalShader += includeFileReader.ReadToEnd();
				}
				else
				{
					finalShader += line + "\n";
				}
			}
			shaderOut = finalShader;
		}

		#endregion ProcessIncludes

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}

		public override void ApplyAsset(in Shader asset, object context)
		{
			//throw new NotImplementedException();
		}

		protected override void GenerateGraphicDeviceId()
		{
			Span<int> id = stackalloc int[1];
			while (recentlyLoadedAssets.TryDequeue(out var i))
			{
				Console.WriteLine("shader allocation");
				ref var shader = ref GetAsset(i);
				PluginManager.backendRenderer.GenerateBuffers(Target.Shader, id);
				shader.Program = id[0];
				PluginManager.backendRenderer.GenerateBuffers(Target.VertexShader, id);
				shader.VertexID = id[0];
				PluginManager.backendRenderer.GenerateBuffers(Target.FragmentShader, id);
				shader.FragmentID = id[0];
				PluginManager.backendRenderer.Allocate(shader.Program, shader.VertexID, shader.FragmentID, shader.VertexSource, shader.FragmentSource, shader.uniformArray, shader.attribArray);
			}
		}
	}
}