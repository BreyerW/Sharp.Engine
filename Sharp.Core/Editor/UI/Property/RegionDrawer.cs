using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sharp.Editor.Attribs;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using Squid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	/* public class SLDrawer : Gwen.Control.Base
     {
         public Action render;

         public SLDrawer(Gwen.Control.Base parent) : base(parent)
         {
         }

         protected override void Render(Gwen.Skin.Base skin)
         {
             base.Render(skin);
             render();
         }
     }*/

	public class RegionDrawer : PropertyDrawer<Curve[]>//or IList<Curve>?
	{
		internal Color curveColor = Color.Red;
		private Vector2 scale = Vector2.One;
		private Vector2 translation;
		internal static Material line2dMat;
		internal static Material polyfill2dMat;
		internal (float x, float y, float width, float height) curvesRange = (-10, -10, 150, 20);
		internal Vector2[][] lines;
		internal Vector2[] region;

		private uint curvesEditor;
		public Frame curveFrame = new Frame();

		static RegionDrawer()
		{
			var mesh = new Mesh();
			mesh.UsageHint = UsageHint.DynamicDraw;
			mesh.FullPath = "dynamic_curve";
			Pipeline.Get<Mesh>().Register(mesh);

			var polyfill = new Mesh();
			polyfill.UsageHint = UsageHint.DynamicDraw;
			polyfill.FullPath = "dynamic_polyfill";
			Pipeline.Get<Mesh>().Register(polyfill);

			polyfill2dMat = new Material();
			polyfill2dMat.BindShader(0, (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\Polyfill2dEditorShader.shader"));
			polyfill2dMat.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("dynamic_polyfill"));
			
			line2dMat = new Material();
			line2dMat.BindShader(0,(Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\LineShader.shader"));
			line2dMat.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("dynamic_curve"));
		}
		public override bool CanApply(MemberInfo memInfo)
		{
			var range = memInfo.GetCustomAttribute<CurveRangeAttribute>();
			if (range is not null)
				curvesRange = range.curvesRange;
			else return false;
			return true;
		}
		public RegionDrawer(MemberInfo memInfo) : base(memInfo)//move all of this to canApply and remove memInfo from constructor?
		{
			curveFrame.Style = "textbox";
			curveFrame.NoEvents = false;
			curveFrame.Position = new Point(label.Size.x, 0);
			curveFrame.MouseUp += RegionDrawer_MouseDown;
			//Scissor = true;
			Childs.Add(curveFrame);
		}

		private void RegionDrawer_MouseDown(Control sender, MouseEventArgs args)
		{
			if (!Window.windows.ContainsKey(curvesEditor))
			{
				CurvesView.drawer = this;
				var win = new FloatingWindow("");
				curvesEditor = win.windowId;
				var curvesView = new CurvesView(curvesEditor, this);
				Window.OpenView(curvesView, MainEditorView.mainViews[curvesEditor].desktop);
				win.Size = (700, 500);

			}
			Window.windows[curvesEditor].OnFocus();
		}

		private void CreateRegion(Curve minCurve, Curve maxCurve)
		{
			List<Vector2> list = new List<Vector2>();
			var lines = new List<Vector2>[] { new List<Vector2>(), new List<Vector2>() };

			lines[0].AddRange(GetPoints(curvesRange.x, curvesRange.width, 0));
			lines[1].AddRange(GetPoints(curvesRange.x, curvesRange.width, 1));

			var controlPoints = lines[0].Union(lines[1], new Vector2Comparer()).ToList();

			//int id = lines[0].Count < lines[1].Count ? 1 : 0;

			List<Vector2> list2 = new List<Vector2>();
			foreach (var i in ..controlPoints.Count)
			{
				list2.Add(new Vector2(controlPoints[i].X - 0.00001f, 0));
				list2.Add(new Vector2(controlPoints[i].X + 0.00001f, 0));
			}
			var finalControlPoints = controlPoints.Concat(list2).ToList();
			finalControlPoints.Sort((v, next) =>
						{
							return v.X < next.X ? -1 : 1;
						});
			Vector2 vector = new Vector2(finalControlPoints[0].X, maxCurve.Evaluate(finalControlPoints[0].X));
			Vector2 vector2 = new Vector2(finalControlPoints[0].X, minCurve.Evaluate(finalControlPoints[0].X));
			foreach (var i in 1..finalControlPoints.Count)
			{
				Vector2 vector3 = new Vector2(finalControlPoints[i].X, maxCurve.Evaluate(finalControlPoints[i].X));
				Vector2 vector4 = new Vector2(finalControlPoints[i].X, minCurve.Evaluate(finalControlPoints[i].X));
				if (vector.Y >= vector2.Y && vector3.Y >= vector4.Y)
				{
					list.Add(vector);
					list.Add(vector4);
					list.Add(vector2);
					list.Add(vector);
					list.Add(vector3);
					list.Add(vector4);
				}
				else
				{
					if (vector.Y <= vector2.Y && vector3.Y <= vector4.Y)
					{
						list.Add(vector2);
						list.Add(vector3);
						list.Add(vector);
						list.Add(vector2);
						list.Add(vector4);
						list.Add(vector3);
					}
					else
					{
						Vector2 zero = Vector2.Zero;
						if (LineIntersection(vector, vector3, vector2, vector4, ref zero))
						{
							list.Add(vector);
							list.Add(zero);
							list.Add(vector2);
							list.Add(vector3);
							list.Add(zero);
							list.Add(vector4);
						}
						else
						{
							Console.WriteLine("Error: No intersection found! There should be one...");
						}
					}
				}
				vector = vector3;
				vector2 = vector4;
			}

			region = list.ToArray();
			this.lines = new Vector2[2][];
			foreach (var j in ..2)
			{
				this.lines[j] = lines[j].ToArray();
			}
			//Console.WriteLine("drawing");
		}

		internal bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
		{
			float num = p2.X - p1.X;
			float num2 = p2.Y - p1.Y;
			float num3 = p4.X - p3.X;
			float num4 = p4.Y - p3.Y;
			float num5 = num * num4 - num2 * num3;
			if (num5 == 0f)
			{
				return false;
			}
			float num6 = p3.X - p1.X;
			float num7 = p3.Y - p1.Y;
			float num8 = (num6 * num4 - num7 * num3) / num5;
			result = new Vector2(p1.X + num8 * num, p1.Y + num8 * num2);

			return true;
		}

		private Vector2[] GetPoints(float minTime, float maxTime, int curveId)
		{
			List<Vector2> list = new List<Vector2>();
			if (Value is null)
			{
				return list.ToArray();
			}
			list.Capacity = 1000 + Value.Length;
			float[,] array = new float[1, 2];
			array[0, 0] = minTime;
			array[0, 1] = maxTime;
			foreach (var i in ..array.GetLength(0))
			{
				AddPoints(ref list, array[i, 0], array[i, 1], minTime, maxTime, curveId);
			}
			if (list.Count > 0)
			{

				for (int j = 1; j < list.Count; j++)
				{
					if (list[j].X < list[j - 1].X)
					{
						list.RemoveAt(j);
						j--;
					}
					else if (list[j] == list[j - 1])
					{
						list.RemoveAt(j);
						j--;
					}
				}
			}
			return list.ToArray();
		}

		private void AddPoints(ref List<Vector2> points, float minTime, float maxTime, float visibleMinTime, float visibleMaxTime, int curveId)
		{
			var maxX = curvesRange.width + (curvesRange.x < 0 ? curvesRange.x : 0);
			var keys = Value[curveId].keys;
			if (keys[0].time > minTime)
			{
				points.Add(new Vector2(curvesRange.x, Value[curveId].Evaluate(curvesRange.x)));
				points.Add(new Vector2(keys[0].time, keys[0].value));//TODO: bug in corners or with evaluate?
			}
			foreach (var i in ..(keys.Length - 1))
			{
				Keyframe keyframe = keys[i];
				Keyframe keyframe2 = keys[i + 1];
				if (keyframe2.time >= minTime && keyframe.time <= maxTime)
				{
					points.Add(new Vector2(keyframe.time, keyframe.value));
					int segmentResolution = GetSegmentResolution(visibleMinTime, visibleMaxTime, keyframe.time, keyframe2.time);
					float num = Lerp(keyframe.time, keyframe2.time, 0.001f / segmentResolution);
					points.Add(new Vector2(num, Value[curveId].Evaluate(num)));
					foreach (float num2 in 1..segmentResolution)
					{
						num = Lerp(keyframe.time, keyframe2.time, num2 / segmentResolution);
						points.Add(new Vector2(num, Value[curveId].Evaluate(num)));
					}
					num = Lerp(keyframe.time, keyframe2.time, 1 - 0.001f / segmentResolution);
					points.Add(new Vector2(num, Value[curveId].Evaluate(num)));
					num = keyframe2.time;
					points.Add(new Vector2(num, keyframe2.value));
				}
			}
			if (keys[keys.Length - 1].time <= maxTime)
			{
				points.Add(new Vector2(keys[keys.Length - 1].time, keys[keys.Length - 1].value));
				points.Add(new Vector2(maxX, keys[keys.Length - 1].value));
			}
		}

		private static int GetSegmentResolution(float minTime, float maxTime, float keyTime, float nextKeyTime)
		{
			float num = maxTime - minTime;
			float num2 = nextKeyTime - keyTime;
			int value = (int)Math.Round(1000 * (num2 / num), MidpointRounding.AwayFromZero);
			return NumericsExtensions.Clamp(value, 1, 50);
		}

		private static Color PrepareColorForCurve(Color color, int curveId)
		{
			return color;
		}

		private static Color PrepareColorForRegion(Color color)
		{
			return new Color(color.R, color.G, color.B, (byte)(0.4f * 255));
		}

		internal static float Lerp(float val1, float val2, float time)
		{
			return val1 + (val2 - val1) * time;
			// return (val1 * (1.0f - time)) + (val2 * time);
		}

		internal static Vector2 CurveToViewSpace(Vector2 pointInCurveSpace, Vector2 scale, Vector2 translation)
		{
			return new Vector2((pointInCurveSpace.X * scale.X) + translation.X, (pointInCurveSpace.Y * scale.Y) + translation.Y);
		}

		internal static Vector2 ViewToCurveSpace(Vector2 pointInViewSpace, Vector2 scale, Vector2 translation)
		{
			return new Vector2((pointInViewSpace.X - translation.X) / scale.X, (pointInViewSpace.Y - translation.Y) / scale.Y);
		}

		protected override void OnAutoSize()
		{
			//base.OnAutoSize();
			//Size = new Squid.Point(Parent.Size.x, 20);
		}

		protected override void DrawAfter()
		{
			ref var curves = ref Value;
			for (int i = 0; i < curves.Length; i += 2)
				CreateRegion(curves[i], curves[i + 1]);
			var p = curveFrame.Location;

			var max = curvesRange.height + (curvesRange.y < 0 ? curvesRange.y : 0);
			//var maxY = curvesRange.width + (curvesRange.x < 0 ? curvesRange.x : 0);
			scale = new Vector2((curveFrame.Size.x - 2) / (curvesRange.width), -(curveFrame.Size.y - 2) / (curvesRange.height));//remember abount - on y
			translation = new Vector2(-curvesRange.x * scale.X + 1 + p.x, curveFrame.Size.y - 1 - curvesRange.y * scale.Y + p.y);
			var color = PrepareColorForRegion(curveColor);

			polyfill2dMat.BindProperty("color", color);

			ref var polyfill = ref Pipeline.Get<Mesh>().GetAsset("dynamic_polyfill");
			var array = new Basic2dVertexFormat[region.Length];
			var indices = new ushort[region.Length];
			foreach (var i in ..region.Length)
			{
				indices[i] = (ushort)i;
				array[i].vertex_position = CurveToViewSpace(region[i], scale, translation);
			}

			polyfill.LoadVertices<Basic2dVertexFormat>(array);
			polyfill.LoadIndices<ushort>(indices);
			polyfill2dMat.Draw();

			var c = new Color(curveColor.R, curveColor.G, curveColor.B, (byte)(curveColor.A * 0.75f));
			ref var mesh = ref Pipeline.Get<Mesh>().GetAsset("dynamic_curve");

			line2dMat.BindProperty("width", 2f);
			foreach (var j in ..2)
			{
				c = PrepareColorForCurve(c, j);
				line2dMat.BindProperty("color", c);

				Line2dVertexFormat[] verts = new Line2dVertexFormat[lines[j].Length * 4];
				indices = new ushort[lines[j].Length * 6];
				foreach (var i in ..(lines[j].Length - 1))
				{
					var start = CurveToViewSpace(lines[j][i], scale, translation);
					var end = CurveToViewSpace(lines[j][i + 1], scale, translation);
					GenerateCurveMesh(verts.AsSpan()[(i * 4)..], indices.AsSpan()[(i * 6)..], i * 4, start, end);
				}
				mesh.LoadIndices<ushort>(indices);
				mesh.LoadVertices<Line2dVertexFormat>(verts);
				line2dMat.Draw();
			}

		}
		public static void GenerateCurveMesh(Span<Line2dVertexFormat> verts, Span<ushort> indices, int i, Vector2 start, Vector2 end)
		{
			verts[0] = new Line2dVertexFormat()
			{
				vertex_position = new Vector2(start.X, start.Y),
				dir = start - end,
				miter = -1f
			};
			verts[1] = new Line2dVertexFormat()
			{
				vertex_position = new Vector2(start.X, start.Y),
				dir = start - end,
				miter = 1f
			};
			verts[2] = new Line2dVertexFormat()
			{
				vertex_position = new Vector2(end.X, end.Y),
				dir = start - end,
				miter = 1f
			};
			verts[3] = new Line2dVertexFormat()
			{
				vertex_position = new Vector2(end.X, end.Y),
				dir = start - end,
				miter = -1f
			};
			indices[0] = (ushort)(i);
			indices[1] = (ushort)(i + 1);
			indices[2] = (ushort)(i + 2);
			indices[3] = (ushort)(i);
			indices[4] = (ushort)(i + 2);
			indices[5] = (ushort)(i + 3);
		}
	}

	internal class Vector2Comparer : IEqualityComparer<Vector2>
	{
		public bool Equals(Vector2 x, Vector2 y)
		{
			return x.X == y.X;
		}

		public int GetHashCode(Vector2 v)
		{
			return v.GetHashCode();
		}
	}
}