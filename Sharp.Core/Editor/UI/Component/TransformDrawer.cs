using Sharp.Core.Editor.Attribs;
using Sharp.Editor;
using Sharp.Editor.UI;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using Squid;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sharp.Core.Editor.UI.Component
{
	static class RegisterTransform
	{
		[ModuleInitializer]
		internal static void Register()
		{
			InspectorView.RegisterDrawerFor<Transform>(() => new TransformDrawer());
		}
	}
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
						t.ModelMatrix = t.ModelMatrix * Matrix4x4.CreateTranslation(delta);
					}
					t.Position = pos;
				});
			position.AutoSize = AutoSize.Horizontal;
			eulerAngles = new Vector3Field(() => (Target as Transform).Rotation * NumericsExtensions.Rad2Deg,
				(pos) =>
				{
					var t = Target as Transform;
					var posInRad = pos * NumericsExtensions.Deg2Rad;
					var delta = posInRad - t.Rotation;
					if (delta != Vector3.Zero)
					{
						var deltaRot = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(delta.Y, delta.X, delta.Z));
						t.ModelMatrix = Matrix4x4.CreateFromQuaternion(deltaRot) * t.ModelMatrix;
					}
					t.Rotation = posInRad;
				});
			eulerAngles.AutoSize = AutoSize.Horizontal;
			scale = new Vector3Field(() => (Target as Transform).Scale, (pos) =>
			{
				var t = Target as Transform;
				var delta = pos - t.Scale;
				if (pos.X is 0) pos.X = 0.00001f;
				if (pos.Y is 0) pos.Y = 0.00001f;
				if (pos.Z is 0) pos.Z = 0.00001f;
				if (delta != Vector3.Zero)
				{
					t.ModelMatrix = Matrix4x4.CreateScale(pos / t.Scale) * t.ModelMatrix;
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
