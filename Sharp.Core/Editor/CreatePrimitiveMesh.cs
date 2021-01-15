using Sharp;
using Sharp.Editor;
using SharpAsset;
using SharpAsset.Pipeline;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class CreatePrimitiveMesh
{
	//TODO: move all these as method parameters
	public static int numVertices = 10;
	public static float radiusTop = 0.5f;
	public static float radiusBottom = 0.5f;
	public static bool outside = true;
	public static bool inside = false;

	public static Mesh GenerateLine()
	{
		var lineData = new LineVertexFormat[4] {
						new LineVertexFormat(){ vertex_position=new Vector3(0,0,0),prev_position=new Vector3(0,0,0),next_position=new Vector3(1,0,0),dir=-1 },
						new LineVertexFormat(){ vertex_position=new Vector3(0,0,0f),prev_position=new Vector3(0,0,0),next_position=new Vector3(1,0,0),dir=1 },
						new LineVertexFormat(){ vertex_position=new Vector3(1,0,0f),prev_position=new Vector3(0,0,0),next_position=new Vector3(1,0,0),dir=1 },
						new LineVertexFormat(){ vertex_position=new Vector3(1,0,0),prev_position=new Vector3(0,0,0),next_position=new Vector3(1,0,0),dir=-1 },

					}.AsSpan();
		var newMesh = new Mesh
		{
			FullPath = "line",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadVertices(lineData);
		newMesh.LoadIndices(new ushort[] { 0, 1, 2, 0, 2, 3 }.AsSpan());
		return newMesh;
	}
	public static Mesh GenerateSquare(in Matrix4x4 transform, string name = "square", Color? vertexColor = null)
	{
		var Mesh = new Mesh
		{
			FullPath = name,
			UsageHint = UsageHint.StaticDraw
		};
		var vertices = new UIVertexFormat[4] {
						new (){position=Vector3.Transform(new (0,0,0),transform),texcoords=new (0,0) },
						new (){position=Vector3.Transform(new (0,1,0),transform),texcoords=new (0,1) },
						new (){position=Vector3.Transform(new (1,1,0),transform),texcoords=new (1,1) },
						new (){position=Vector3.Transform(new (1,0,0),transform),texcoords=new (1,0) }
					};
		List<byte> bytes = new();
		if (vertexColor.HasValue)

			foreach (var i in ..vertices.Length)
			{
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vertices[i], 1)).ToArray());
				var color = vertexColor.GetValueOrDefault();
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref color, 1)).ToArray());

			}
		else
			bytes.AddRange(MemoryMarshal.AsBytes(vertices.AsSpan()).ToArray());

		Mesh.VertType = vertexColor.HasValue ? typeof(VertexColorFormat) : typeof(UIVertexFormat);
		Mesh.LoadVertices(bytes.ToArray());
		Mesh.LoadIndices(new ushort[] { 0, 1, 2, 0, 2, 3 }.AsSpan());
		return Mesh;
	}
	public static Mesh GenerateEditorCube()
	{
		return Unsafe.Unbox<Mesh>(Pipeline.Get<Mesh>().Import(Application.projectPath + @"\Content\viewcube.dae"));
	}
	public static Mesh GenerateCylinder(in Matrix4x4 transform, string meshName = "cylinder", Color? vertexColor = null)
	{
		var newMesh = new Mesh
		{
			FullPath = meshName,
			UsageHint = UsageHint.StaticDraw
		};
		Vector3 end_point = Vector3.Zero;
		Vector3 axis = Vector3.UnitX;
		Vector3 v1;
		if (axis.Z is < -0.01f or > 0.01f)
			v1 = new Vector3(axis.Z, axis.Z, -axis.X - axis.Y);
		else
			v1 = new Vector3(-axis.Y - axis.Z, axis.X, axis.X);
		Vector3 v2 = Vector3.Cross(v1, axis);

		// Make the vectors have length radius.
		var vTop1 = v1 * (radiusTop / v1.Length());
		var vTop2 = v2 * (radiusTop / v2.Length());
		var vBottom1 = v1 * (radiusBottom / v1.Length());
		var vBottom2 = v2 * (radiusBottom / v2.Length());
		// Make the top end cap.
		// Make the end point.
		List<UIVertexFormat> vertices = new();
		ushort pt0 = (ushort)vertices.Count; // Index of end_point.
		vertices.Add(new() { position = Vector3.Transform(end_point, transform) });
		var indices = new List<ushort>();
		float theta = 0;
		float dtheta = 2 * MathF.PI / numVertices;
		ushort pt1 = 0;
		ushort pt2 = 0;
		// Make the top points.
		if (radiusTop > float.Epsilon)
		{
			for (int i = 0; i < numVertices; i++)
			{
				vertices.Add(new()
				{
					position = Vector3.Transform(end_point +
					MathF.Cos(theta) * vTop1 +
					MathF.Sin(theta) * vTop2, transform)
				});
				theta += dtheta;
			}

			// Make the top triangles.
			pt1 = (ushort)(vertices.Count - 1); // Index of last point.
			pt2 = (ushort)(pt0 + 1);                  // Index of first point.
			for (int i = 0; i < numVertices; i++)
			{
				indices.Add(pt0);
				indices.Add(pt1);
				indices.Add(pt2);
				pt1 = pt2++;
			}
		}
		// Make the bottom end cap.
		// Make the end point.
		pt0 = (ushort)vertices.Count; // Index of end_point2.
		Vector3 end_point2 = end_point + axis;
		vertices.Add(new()
		{
			position = Vector3.Transform(end_point2, transform)
		});

		// Make the bottom points.
		if (radiusBottom > float.Epsilon)
		{
			theta = 0;
			for (int i = 0; i < numVertices; i++)
			{
				vertices.Add(new()
				{
					position = Vector3.Transform(end_point2 +
					MathF.Cos(theta) * vBottom1 +
					MathF.Sin(theta) * vBottom2, transform)
				});
				theta += dtheta;
			}

			// Make the bottom triangles.
			theta = 0;
			pt1 = (ushort)(vertices.Count - 1); // Index of last point.
			pt2 = (ushort)(pt0 + 1);                  // Index of first point.
			for (int i = 0; i < numVertices; i++)
			{
				indices.Add((ushort)(numVertices + 1));    // end_point2
				indices.Add(pt2);
				indices.Add(pt1);
				pt1 = pt2++;
			}
		}

		// Make the sides.
		// Add the points to the mesh.
		ushort first_side_point = (ushort)vertices.Count;
		theta = 0;
		for (int i = 0; i < numVertices; i++)
		{
			Vector3 p1 = end_point +
				MathF.Cos(theta) * vTop1 +
				MathF.Sin(theta) * vTop2;
			Vector3 p2 = end_point2 +
				MathF.Cos(theta) * vBottom1 +
				MathF.Sin(theta) * vBottom2;
			vertices.Add(new()
			{
				position = Vector3.Transform(p1, transform)
			});

			vertices.Add(new()
			{
				position = Vector3.Transform(p2, transform)
			});
			theta += dtheta;
		}
		List<byte> bytes = new();
		var verts = CollectionsMarshal.AsSpan(vertices);
		if (vertexColor.HasValue)
			foreach (var i in ..vertices.Count)
			{
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref verts[i], 1)).ToArray());
				var color = vertexColor.GetValueOrDefault();
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref color, 1)).ToArray());

			}
		else
			bytes.AddRange(MemoryMarshal.AsBytes(verts).ToArray());
		newMesh.VertType = vertexColor.HasValue ? typeof(VertexColorFormat) : typeof(UIVertexFormat);
		newMesh.LoadVertices(bytes.ToArray());
		// Make the side triangles.
		pt1 = (ushort)(vertices.Count - 2);
		pt2 = (ushort)(pt1 + 1);
		ushort pt3 = first_side_point;
		ushort pt4 = (ushort)(pt3 + 1);
		for (int i = 0; i < numVertices; i++)
		{
			indices.Add(pt1);
			indices.Add(pt2);
			indices.Add(pt4);

			indices.Add(pt1);
			indices.Add(pt4);
			indices.Add(pt3);

			pt1 = pt3;
			pt3 += 2;
			pt2 = pt4;
			pt4 += 2;
		}

		newMesh.LoadIndices<ushort>(indices.ToArray());

		radiusBottom = 0.5f;
		return newMesh;
	}
	public static Mesh GenerateCone(in Matrix4x4 transform, string name = "cone", Color? vertexColor = null)
	{
		if (radiusTop > float.Epsilon)
			radiusBottom = 0;
		return GenerateCylinder(transform, name, vertexColor);
	}
	public static Mesh GenerateCube(in Matrix4x4 transform, string name = "cube", Color? vertexColor = null)
	{
		var newMesh = new Mesh
		{
			FullPath = name,
			UsageHint = UsageHint.StaticDraw
		};
		#region UVs
		Vector2 _00 = new Vector2(0f, 0f);
		Vector2 _10 = new Vector2(1f, 0f);
		Vector2 _01 = new Vector2(0f, 1f);
		Vector2 _11 = new Vector2(1f, 1f);

		#endregion
		#region Vertices
		Vector3 p0 = Vector3.Transform(new Vector3(0, 0, 0), transform);
		Vector3 p1 = Vector3.Transform(new Vector3(1, 0, 0), transform);
		Vector3 p2 = Vector3.Transform(new Vector3(1, 0, 1), transform);
		Vector3 p3 = Vector3.Transform(new Vector3(0, 0, 1), transform);

		Vector3 p4 = Vector3.Transform(new Vector3(0, 1, 0), transform);
		Vector3 p5 = Vector3.Transform(new Vector3(1, 1, 0), transform);
		Vector3 p6 = Vector3.Transform(new Vector3(1, 1, 1), transform);
		Vector3 p7 = Vector3.Transform(new Vector3(0, 1, 1), transform);

		var vertices = new UIVertexFormat[]
		{
					new (){position=p0, texcoords=_11 },
					new (){position=p1, texcoords=_01 },
					new (){position=p2, texcoords=_00 },
					new (){position=p3, texcoords=_10 },

					new (){position=p7, texcoords=_11 },
					new (){position=p4, texcoords=_01 },
					new (){position=p0, texcoords=_00 },
					new (){position=p3, texcoords=_10 },

					new (){position=p4, texcoords=_11 },
					new (){position=p5, texcoords=_01 },
					new (){position=p1, texcoords=_00 },
					new (){position=p0, texcoords=_10 },

					new (){position=p6, texcoords=_11 },
					new (){position=p7, texcoords=_01 },
					new (){position=p3, texcoords=_00 },
					new (){position=p2, texcoords=_10 },

					new (){position=p5, texcoords=_11 },
					new (){position=p6, texcoords=_01 },
					new (){position=p2, texcoords=_00 },
					new (){position=p1, texcoords=_10 },

					new (){position=p7, texcoords=_11 },
					new (){position=p6, texcoords=_01 },
					new (){position=p5, texcoords=_00 },
					new (){position=p4, texcoords=_10 }
		};
		List<byte> bytes = new();
		if (vertexColor.HasValue)
			foreach (var i in ..vertices.Length)
			{
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref vertices[i], 1)).ToArray());
				var color = vertexColor.GetValueOrDefault();
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref color, 1)).ToArray());

			}
		else
			bytes.AddRange(MemoryMarshal.AsBytes(vertices.AsSpan()).ToArray());
		newMesh.VertType = vertexColor.HasValue ? typeof(VertexColorFormat) : typeof(UIVertexFormat);
		newMesh.LoadVertices(bytes.ToArray());
		#endregion

		/*			#region Normales
					Vector3 up = Vector3.UnitY;
					Vector3 down = -Vector3.UnitY;
					Vector3 front = Vector3.UnitZ;
					Vector3 back = -Vector3.UnitZ;
					Vector3 left = -Vector3.UnitX;
					Vector3 right = Vector3.UnitX;

					Vector3[] normales = new Vector3[]
					{
		// Bottom
		down, down, down, down,

		// Left
		left, left, left, left,

		// Front
		front, front, front, front,

		// Back
		back, back, back, back,

		// Right
		right, right, right, right,

		// Top
		up, up, up, up
					};
					#endregion*/



		#region Triangles
		ushort[] indices = new ushort[]
	{
	// Bottom
	3, 1, 0,
	3, 2, 1,			
 
	// Left
	3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
	3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
 
	// Front
	3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
	3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
 
	// Back
	3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
	3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
 
	// Right
	3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
	3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
 
	// Top
	3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
	3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,

	};
		#endregion

		newMesh.LoadIndices<ushort>(indices);
		return newMesh;
	}
	public static float outerRadius = 1.0f;
	public static float innerRadius = 0.0f;
	public static float totalAngleDeg = 360.0f;

	public static Mesh GenerateDisc()
	{
		var vertices = new UIVertexFormat[2 * numVertices];
		//uv = new VectorArray2f(2 * Slices);
		//normals = new VectorArray3f(2 * Slices);
		var indices = new ushort[2 * numVertices * 3 + 3];

		bool bFullDisc = (totalAngleDeg > 359.99f);
		float fTotalRange = (totalAngleDeg) * NumericsExtensions.Deg2Rad;
		float fDelta = (bFullDisc) ? fTotalRange / numVertices : fTotalRange / (numVertices - 1);
		float fUVRatio = innerRadius / outerRadius;
		for (int k = 0; k < numVertices; ++k)
		{
			float angle = (float)k * fDelta;
			float cosa = MathF.Cos(angle), sina = MathF.Sin(angle);
			vertices[k] = new UIVertexFormat() { position = new Vector3(innerRadius * cosa, 0, innerRadius * sina) };
			vertices[numVertices + k] = new UIVertexFormat() { position = new Vector3(outerRadius * cosa, 0, outerRadius * sina) };
			//uv[k] = new Vector2f(0.5f * (1.0f + fUVRatio * cosa), 0.5f * (1.0f + fUVRatio * sina));
			//uv[Slices + k] = new Vector2f(0.5f * (1.0f + cosa), 0.5f * (1.0f + sina));
			//normals[k] = normals[Slices + k] = Vector3f.AxisY;
		}

		int ti = 0;
		for (int k = 0; k < numVertices - 1; ++k)
		{
			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k + 1);

			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(numVertices + k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k);
		}
		if (bFullDisc)
		{      // close disc if we went all the way
			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)0;
			indices[3 * ti + 2] = (ushort)numVertices;

			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)numVertices;
			indices[3 * ti + 2] = (ushort)(2 * numVertices - 1);
		}
		/*vertices[0].prev_position = vertices[0].vertex_position;
		vertices[0].next_position = vertices[1].vertex_position;
		vertices[2 * numVertices - 1].prev_position = vertices[2 * numVertices - 2].vertex_position;
		vertices[2 * numVertices - 1].next_position = vertices[2 * numVertices - 1].vertex_position;

		for (var id = 1; id < 2 * numVertices - 1; id++)
		{
			vertices[id].prev_position = vertices[id - 1].vertex_position;
			vertices[id].next_position = vertices[id + 1].vertex_position;
			id++;
		}*/
		var newMesh = new Mesh
		{
			FullPath = "disc",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices);
		return newMesh;
	}
	internal static Mesh GenerateEditorDisc(Vector3 startAxis, Vector3 nextAxis)
	{
		var vertices = new UIVertexFormat[2 * numVertices];
		//uv = new VectorArray2f(2 * Slices);
		//normals = new VectorArray3f(2 * Slices);
		var indices = new ushort[2 * numVertices * 3 + 3];

		bool bFullDisc = (totalAngleDeg > 359.99f);
		float fTotalRange = (totalAngleDeg) * NumericsExtensions.Deg2Rad;
		float fDelta = (bFullDisc) ? fTotalRange / numVertices : fTotalRange / (numVertices - 1);
		float fUVRatio = innerRadius / outerRadius;
		var cross = Vector3.Cross(startAxis, nextAxis).Normalize();
		for (int k = 0; k < numVertices; ++k)
		{
			float angle = (float)k * fDelta;
			//float cosa = MathF.Cos(angle), sina = MathF.Sin(angle);
			var rotateMat = Matrix4x4.CreateFromAxisAngle(cross, angle);
			vertices[k] = new UIVertexFormat()
			{
				position = startAxis.Transformed(rotateMat) * innerRadius
			};
			vertices[numVertices + k] = new UIVertexFormat()
			{
				position = startAxis.Transformed(rotateMat) * outerRadius
			};
			//uv[k] = new Vector2f(0.5f * (1.0f + fUVRatio * cosa), 0.5f * (1.0f + fUVRatio * sina));
			//uv[Slices + k] = new Vector2f(0.5f * (1.0f + cosa), 0.5f * (1.0f + sina));
			//normals[k] = normals[Slices + k] = Vector3f.AxisY;
		}

		int ti = 0;
		for (int k = 0; k < numVertices - 1; ++k)
		{
			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k + 1);

			ti++;
			indices[3 * ti] = (ushort)k;
			indices[3 * ti + 1] = (ushort)(numVertices + k + 1);
			indices[3 * ti + 2] = (ushort)(numVertices + k);
		}
		if (bFullDisc)
		{      // close disc if we went all the way
			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)0;
			indices[3 * ti + 2] = (ushort)numVertices;

			ti++;
			indices[3 * ti] = (ushort)(numVertices - 1);
			indices[3 * ti + 1] = (ushort)numVertices;
			indices[3 * ti + 2] = (ushort)(2 * numVertices - 1);
		}
		/*vertices[0].prev_position = vertices[0].vertex_position;
		vertices[0].next_position = vertices[1].vertex_position;
		vertices[2 * numVertices - 1].prev_position = vertices[2 * numVertices - 2].vertex_position;
		vertices[2 * numVertices - 1].next_position = vertices[2 * numVertices - 1].vertex_position;

		for (var id = 1; id < 2 * numVertices - 1; id++)
		{
			vertices[id].prev_position = vertices[id - 1].vertex_position;
			vertices[id].next_position = vertices[id + 1].vertex_position;
			id++;
		}*/
		var newMesh = new Mesh
		{
			FullPath = "editor_disc",
			UsageHint = UsageHint.StaticDraw
		};
		newMesh.LoadIndices<ushort>(indices);
		newMesh.LoadVertices<UIVertexFormat>(vertices);
		return newMesh;
	}
	public static Mesh GenerateTorus(in Matrix4x4 transform, string name = "torus", Color? vertexColor = null)
	{
		float radius = 1f;
		float tube = 0.025f;
		int radialSegments = numVertices;
		int tubularSegments = numVertices;
		float arc = 360f;
		var newMesh = new Mesh
		{
			FullPath = name,
			UsageHint = UsageHint.StaticDraw
		};
		//List<Vector2> uvs = new List<Vector2>();
		List<UIVertexFormat> vertices = new List<UIVertexFormat>();
		//List<Vector3> normals = new List<Vector3>();
		List<ushort> indices = new List<ushort>();

		var center = new Vector3();

		for (var j = 0; j <= radialSegments; j++)
		{
			for (var i = 0; i <= tubularSegments; i++)
			{
				var u = i / (float)tubularSegments * arc * NumericsExtensions.Deg2Rad;
				var v = j / (float)radialSegments * MathF.PI * 2.0f;

				center.X = radius * MathF.Cos(u);
				center.Y = radius * MathF.Sin(u);

				var vertex = new Vector3();
				vertex.X = (radius + tube * MathF.Cos(v)) * MathF.Cos(u);
				vertex.Y = (radius + tube * MathF.Cos(v)) * MathF.Sin(u);
				vertex.Z = tube * MathF.Sin(v);

				vertices.Add(new UIVertexFormat() { position = Vector3.Transform(vertex, transform) });

				//uvs.Add(new Vector2(i / (float)tubularSegments, j / (float)radialSegments));
				//Vector3 normal = vertex - center;
				//normal.Normalize();
				//normals.Add(normal);
			}
		}
		List<byte> bytes = new();
		var verts = CollectionsMarshal.AsSpan(vertices);
		if (vertexColor.HasValue)
			foreach (var i in ..vertices.Count)
			{
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref verts[i], 1)).ToArray());
				var color = vertexColor.GetValueOrDefault();
				bytes.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref color, 1)).ToArray());

			}
		else
			bytes.AddRange(MemoryMarshal.AsBytes(verts).ToArray());
		newMesh.VertType = vertexColor.HasValue ? typeof(VertexColorFormat) : typeof(UIVertexFormat);
		newMesh.LoadVertices(bytes.ToArray());

		for (var j = 1; j <= radialSegments; j++)
		{
			for (var i = 1; i <= tubularSegments; i++)
			{
				var a = (tubularSegments + 1) * j + i - 1;
				var b = (tubularSegments + 1) * (j - 1) + i - 1;
				var c = (tubularSegments + 1) * (j - 1) + i;
				var d = (tubularSegments + 1) * j + i;

				indices.Add((ushort)a);
				indices.Add((ushort)b);
				indices.Add((ushort)d);

				indices.Add((ushort)b);
				indices.Add((ushort)c);
				indices.Add((ushort)d);
			}

		}

		newMesh.LoadIndices<ushort>(indices.ToArray());
		return newMesh;
	}
}