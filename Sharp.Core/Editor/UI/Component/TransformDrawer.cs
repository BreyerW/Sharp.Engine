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
						Matrix4x4.Decompose(t.ModelMatrix, out var scale, out var rot, out var trans);
						Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(scale);
						Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans);
						Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot);
						t.ModelMatrix = scaleOrigin * rotateOrigin * Matrix4x4.CreateTranslation(delta) * translateOrigin;
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
					{
						Matrix4x4.Decompose(t.ModelMatrix, out var scale, out var rot, out var trans);
						var deltaRot = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll((delta.Y) * NumericsExtensions.Deg2Rad, (delta.X) * NumericsExtensions.Deg2Rad, (delta.Z) * NumericsExtensions.Deg2Rad));
						Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(scale);
						Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans);
						Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot * deltaRot);
						t.ModelMatrix = scaleOrigin * rotateOrigin * translateOrigin;
					}
					t.Rotation = pos;
				});
			eulerAngles.AutoSize = AutoSize.Horizontal;
			scale = new Vector3Field(() => (Target as Transform).Scale, (pos) =>
			{
				var t = Target as Transform;
				var delta = pos - t.Scale;
				if (delta != Vector3.Zero)
				{
					Matrix4x4.Decompose(t.ModelMatrix, out var scale, out var rot, out var trans);
					Matrix4x4 scaleOrigin = Matrix4x4.CreateScale(scale);
					Matrix4x4 translateOrigin = Matrix4x4.CreateTranslation(trans);
					Matrix4x4 rotateOrigin = Matrix4x4.CreateFromQuaternion(rot);
					t.ModelMatrix = Matrix4x4.CreateScale(pos) * rotateOrigin * translateOrigin;
				}
				t.Scale = pos;
			});
			scale.AutoSize = AutoSize.Horizontal;
		}
		public override void OnInitializeGUI()
		{
			var tmpLabel = new Label();
			tmpLabel.Text = "Position: ";
			//tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;
			position.Position = new Point(100, position.Position.y);
			Frame.Controls.Add(tmpLabel);
			Frame.Controls.Add(position);

			tmpLabel = new Label();
			tmpLabel.Text = "Rotation: ";
			//tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;
			eulerAngles.Position = new Point(100, eulerAngles.Position.y);
			Frame.Controls.Add(tmpLabel);
			Frame.Controls.Add(eulerAngles);

			tmpLabel = new Label();
			tmpLabel.Text = "Scale: ";
			//tmpLabel.Size = new Point(0, Size.y/2f);
			tmpLabel.AutoSize = AutoSize.Horizontal;
			//scale.Margin = new Margin(scale.Margin.Left + 10, scale.Margin.Top, scale.Margin.Right, scale.Margin.Bottom);
			Frame.Controls.Add(tmpLabel);
			Frame.Controls.Add(scale);
		}
	}
}
