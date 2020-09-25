using Sharp.Core.Editor.Attribs;
using Sharp.Editor;
using Sharp.Editor.UI;
using Sharp.Engine.Components;
using Squid;
using System;
using System.Numerics;

namespace Sharp.Core.Editor.UI.Component
{
	[Priority(short.MaxValue)]
	class TransformDrawer : ComponentDrawer<Transform>//TODO: remove oninitializedgui by making componentdrawer control itself?
	{
		private Vector3Field position;
		private Vector3Field eulerAngles;
		private Vector3Field scale;

		public TransformDrawer()
		{
			position = new Vector3Field(() => (Target as Transform).Position,
				(pos) =>
				{
					var t = Target as Transform;
					var delta = pos - t.Position;
					if (delta != Vector3.Zero)
					{
						var angles = t.Rotation * NumericsExtensions.Deg2Rad;
						t.ModelMatrix = Matrix4x4.CreateScale(t.Scale) * Matrix4x4.CreateFromYawPitchRoll(angles.Y, angles.X, angles.Z) * Matrix4x4.CreateTranslation(pos);
					}
					t.Position = pos;
				});
			position.AutoSize = AutoSize.Horizontal;
			eulerAngles = new Vector3Field(() => (Target as Transform).Rotation,
				(pos) =>
				{
					var t = Target as Transform;
					var delta = pos - t.Rotation;
					if (delta != Vector3.Zero)
						t.ModelMatrix = Matrix4x4.CreateScale(t.Scale) * Matrix4x4.CreateFromYawPitchRoll((pos.Y) * NumericsExtensions.Deg2Rad, (pos.X) * NumericsExtensions.Deg2Rad, (pos.Z) * NumericsExtensions.Deg2Rad) * Matrix4x4.CreateTranslation(t.Position);
					t.Rotation = pos;
				});
			eulerAngles.AutoSize = AutoSize.Horizontal;
			scale = new Vector3Field(() => (Target as Transform).Scale, (pos) =>
			{
				var t = Target as Transform;
				var delta = pos - t.Scale;
				if (delta != Vector3.Zero)
				{
					var angles = t.Rotation * NumericsExtensions.Deg2Rad;
					t.ModelMatrix = Matrix4x4.CreateScale(pos) * Matrix4x4.CreateFromYawPitchRoll(angles.Y, angles.X, angles.Z) * Matrix4x4.CreateTranslation(t.Position);
				}
				t.Scale = pos;
			});
			scale.AutoSize = AutoSize.Horizontal;
		}
		public override void OnInitializeGUI()
		{
			Frame.Controls.Add(position);
			Frame.Controls.Add(eulerAngles);
			Frame.Controls.Add(scale);
		}
	}
}
