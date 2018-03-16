﻿using System;
using System.Collections.Generic;
using System.IO;
using Sharp;

namespace SharpAsset.Pipeline
{
    [SupportedFiles(".shader", ".glsl")]
    public class ShaderPipeline : Pipeline<Shader>
    {
        public override IAsset Import(string pathToFile)
        {
            //var format = Path.GetExtension (pathToFile);
            //if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
            //throw new NotSupportedException (format+" format is not supported");
            var name = Path.GetFileNameWithoutExtension(pathToFile);

            if (nameToKey.Contains(name))
                return this[nameToKey.IndexOf(name)];

            List<string> shaderStringBuffer = new List<string>();
            using (StreamReader shaderFile = new StreamReader(pathToFile))
            {
                while (shaderFile.Peek() >= 0)
                {
                    shaderStringBuffer.Add(shaderFile.ReadLine());
                }
                var shader = SplitShaders(shaderStringBuffer, ref pathToFile);
                nameToKey.Add(name);

                this[nameToKey.IndexOf(name)] = shader;
                return shader;
            }
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
                lineCount++;
            }

            List<string> vertexShaderBuffer =
                shaderBuffer.GetRange(vertexShaderOffset, fragmentShaderOffset - vertexShaderOffset);
            vertexShaderBuffer.Insert(0, version);
            List<string> fragmentShaderBuffer =
                shaderBuffer.GetRange(fragmentShaderOffset, lineCount - fragmentShaderOffset);
            fragmentShaderBuffer.Insert(0, version);

            string vertexShader, fragmentShader;
            ProcessIncludes(vertexShaderBuffer, out vertexShader);
            ProcessIncludes(fragmentShaderBuffer, out fragmentShader);
            var shader = new Shader();
            shader.uniformArray = new Dictionary<string, int>();
            shader.FullPath = name;
            shader.VertexSource = vertexShader;
            shader.FragmentSource = fragmentShader;
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
                    var splitLine = line.Split(' ');
                    string includeFilename = $"{splitLine[2]}";
                    finalShader += $"// included from source file {includeFilename}\n\r";
                    using (StreamReader includeFileReader = new StreamReader(includeFilename))
                    {
                        finalShader += includeFileReader.ReadToEnd();
                    }
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

        public override ref Shader GetAsset(int index)
        {
            ref var shader = ref this[index];
            if (!shader.allocated)
            {
                Console.WriteLine("shader allocation");
                MainWindow.backendRenderer.GenerateBuffers(ref shader.Program, ref shader.VertexID, ref shader.FragmentID);
                MainWindow.backendRenderer.Allocate(ref shader.Program, ref shader.VertexID, ref shader.FragmentID, ref shader.VertexSource, ref shader.FragmentSource, ref shader.uniformArray);
                shader.allocated = true;
            }
            return ref this[index]; //ref
        }
    }
}