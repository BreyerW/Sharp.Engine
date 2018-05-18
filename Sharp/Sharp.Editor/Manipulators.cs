using System;
using System.Numerics;
using Sharp.Editor.Views;
using Sharp.Commands;

namespace Sharp.Editor
{
    public static class Manipulators
    {
        private static readonly int halfCircleSegments = 64;
        public static readonly Color selectedColor = new Color(0xFF1080FF);
        public static readonly Color fillColor = new Color(0x801080FF);
        public static readonly Color xColor = new Color(0xFF0000AA);
        public static readonly Color yColor = new Color(0xFF00AA00);
        public static readonly Color zColor = new Color(0xFFAA0000);

        internal static int selectedAxisId = 0;
        internal static float? rotAngleOrigin;
        internal static float angle;
        internal static Vector3 currentAngle = Vector3.Zero;
        internal static Vector3? relativeOrigin;
        internal static Vector3? planeOrigin;
        internal static Vector3? rotVectSource;
        internal static Vector3? scaleOrigin;
        internal static Vector3 scaleOffset;
        internal static Vector4 transformationPlane;
        internal static Vector3 startAxis;
        internal static Matrix4x4 startMat;
        internal static ChangeValueCommand newCommand;

        public static void DrawCombinedGizmos(Entity entity)
        {
            DrawCombinedGizmos(entity, (selectedAxisId == 1 ? selectedColor : xColor), (selectedAxisId == 2 ? selectedColor : yColor), (selectedAxisId == 3 ? selectedColor : zColor), (selectedAxisId == 4 ? selectedColor : xColor), (selectedAxisId == 5 ? selectedColor : yColor), (selectedAxisId == 6 ? selectedColor : zColor), (selectedAxisId == 7 ? selectedColor : xColor), (selectedAxisId == 8 ? selectedColor : yColor), (selectedAxisId == 9 ? selectedColor : zColor), 3f);
        }

        public static void DrawCombinedGizmos(Entity entity, Color xColor, Color yColor, Color zColor, Color xRotColor, Color yRotColor, Color zRotColor, Color xScaleColor, Color yScaleColor, Color zScaleColor, float thickness = 5f)
        {
            float scale = (Camera.main.entityObject.Position - entity.Position).Length() / 25f;
            DrawHelper.DrawTranslationGizmo(thickness, scale, xColor, yColor, zColor);
            DrawHelper.DrawRotationGizmo(thickness, scale, xRotColor, yRotColor, zRotColor);
            DrawHelper.DrawScaleGizmo(thickness, scale, xScaleColor, yScaleColor, zScaleColor, scaleOffset);
            if (rotVectSource.HasValue)
            {
                var cross = Vector3.Cross(startAxis, currentAngle).Normalize();
                var fullAngle = NumericsExtensions.CalculateAngle(startAxis, currentAngle);
                var incAngle = fullAngle / halfCircleSegments;
                var vectors = new Vector3[halfCircleSegments + 1];
                vectors[0] = new Vector3(0, 0, 0);
                for (uint i = 1; i < halfCircleSegments + 1; i++)
                {
                    var rotateMat = Matrix4x4.CreateFromAxisAngle(cross, incAngle * (i - 1));
                    vectors[i] = startAxis.Transformed(rotateMat) * 3f * scale;
                }
                Matrix4x4.Decompose(startMat, out _, out var r, out _);
                var rot = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(r));
                var mat = rot * startMat * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
                var fill = new Color(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
                DrawHelper.DrawFilledPolyline(thickness, 3f * scale, fill, ref mat, ref vectors);
            }
        }

        public static void Reset()
        {
            //if (newCommand != null)
            //    newCommand.StoreCommand();
            //UI.Property.PropertyDrawer.StopCommandCommits = false;
            newCommand = null;
            rotVectSource = null;
            rotAngleOrigin = null;
            planeOrigin = null;
            relativeOrigin = null;
            scaleOrigin = null;
            scaleOffset = Vector3.Zero;
            currentAngle = Vector3.Zero;
            angle = 0;
            startAxis = Vector3.Zero;
            startMat = default;
        }

        private static Vector3 GetAxis()
        {
            if (selectedAxisId == 1 || selectedAxisId == 4 || selectedAxisId == 7)
            {
                return Vector3.UnitX;
            }
            else if (selectedAxisId == 2 || selectedAxisId == 5 || selectedAxisId == 8)
            {
                return Vector3.UnitY;
            }
            else
                return Vector3.UnitZ;
        }

        public static void HandleTranslation(Entity entity, ref Ray ray)
        {
            var v = GetAxis();
            if (!SceneView.globalMode)
            {
                Matrix4x4.Decompose(entity.ModelMatrix, out _, out var r, out _);
                v.Transform(r).Normalize();//TransformVector
            }
            var len = ComputeLength(ref ray, entity.Position);
            if (!relativeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                relativeOrigin = (planeOrigin - entity.Position) * (1f / (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix)));
                //UI.Property.PropertyDrawer.StopCommandCommits = true;
            }
            var newPos = ray.origin + ray.direction * len;
            var newOrigin = newPos - relativeOrigin.Value * (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix));
            var delta = newOrigin - entity.Position;
            var lenOnAxis = Vector3.Dot(delta, v);
            delta = v * lenOnAxis;
            entity.Position += delta;
        }

        //private static Quaternion startRot = Quaternion.Identity;
        private static Vector3 MakePositive(Vector3 euler)
        {
            float negativeFlip = -0.0001f * NumericsExtensions.Rad2Deg;
            float positiveFlip = 360.0f + negativeFlip;

            if (euler.X < negativeFlip)
                euler.X += 360.0f;
            else if (euler.X > positiveFlip)
                euler.X -= 360.0f;

            if (euler.Y < negativeFlip)
                euler.Y += 360.0f;
            else if (euler.Y > positiveFlip)
                euler.Y -= 360.0f;

            if (euler.Z < negativeFlip)
                euler.Z += 360.0f;
            else if (euler.Z > positiveFlip)
                euler.Z -= 360.0f;

            return euler;
        }

        public static void HandleRotation(Entity entity, ref Ray ray)//bugged when rescaled
        {
            var v = GetAxis();
            var unchangedV = v;
            //var rot = Quaternion.CreateFromYawPitchRoll(entity.rotation.Y * NumericsExtensions.Deg2Rad, entity.rotation.X * NumericsExtensions.Deg2Rad, entity.rotation.Z * NumericsExtensions.Deg2Rad);
            Matrix4x4.Decompose(entity.ModelMatrix, out _, out var rot, out _);
            if (!SceneView.globalMode)
            {
                v.Transform(rot).Normalize();
            }
            var plane = BuildPlane(entity.Position, v);
            transformationPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
            var len = ComputeLength(ref ray, entity.Position);

            if (!rotVectSource.HasValue)
            {
                var origin = ray.origin + ray.direction * len - entity.Position;
                var rotVec = constrain(origin, v).Normalize();
                rotVectSource = rotVec;
                rotAngleOrigin = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
                startMat = entity.ModelMatrix;
                startAxis = origin.Normalized();
                //UI.Property.PropertyDrawer.StopCommandCommits = true;
            }
            /* var currentVect = constrain((ray.origin + ray.direction * len - entity.Position), v).Normalized();
             var cross = Vector3.Cross(rotVectSource.Value, currentVect);
             var dot = Vector3.Dot(rotVectSource.Value, currentVect);
             var quat = new Quaternion(cross, 1 + dot).Normalized();
             entity.Rotation = Entity.rotationMatrixToEulerAngles(Matrix4.CreateFromQuaternion(quat)) * (180.0f / NumericsExtensions.Pi);*///entity.ModelMatrix.ExtractRotation().Normalized() *
            angle = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
            //entity.ModelMatrix.Inverted().ExtractRotation();
            var rotAxis = new Vector3(transformationPlane.X, transformationPlane.Y, transformationPlane.Z).Transform(Quaternion.Inverse(rot)).Normalize();
            var deltaRot = Matrix4x4.CreateFromAxisAngle(rotAxis, angle - rotAngleOrigin.Value);
            entity.Rotation = Entity.rotationMatrixToEulerAngles(deltaRot * entity.ModelMatrix) * NumericsExtensions.Rad2Deg;
            currentAngle = (ray.origin + ray.direction * len - entity.Position).Normalize();
            rotAngleOrigin = angle;
            //rotvectsource = constrain((ray.origin + ray.direction * len - entity.Position), v).Normalized();
            //startRot = quat * startRot;
        }

        public static void HandleScale(Entity entity, ref Ray ray)
        {
            var v = GetAxis();
            if (!SceneView.globalMode)
            {
                Matrix4x4.Decompose(entity.ModelMatrix, out _, out var r, out _);
                v.Transform(r).Normalize(); //TransformVector
            }
            var len = ComputeLength(ref ray, entity.Position);
            if (!planeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                scaleOrigin = entity.Scale;
                // UI.Property.PropertyDrawer.StopCommandCommits = true;
            }
            var newPos = ray.origin + ray.direction * len;
            var delta = (newPos - entity.Position).Length() / (planeOrigin.Value - entity.Position).Length();
            scaleOffset = newPos * v;
            entity.Scale = scaleOrigin.Value + v * delta - v;
        }

        private static float GetUniform(Vector3 pos, Matrix4x4 mat)
        {
            var trf = new Vector4(pos, 1f);
            trf = Vector4.Transform(trf, mat);
            return trf.W;
        }

        private static Vector3 constrain(Vector3 vec, Vector3 axis)
        {
            var onPlane = Vector3.Subtract(vec, axis * Vector3.Dot(axis, vec));
            var norm = onPlane.LengthSquared();
            if (norm > 0)
            {
                if (onPlane.Z < 0) onPlane = -onPlane;
                return onPlane * (1 / (float)Math.Sqrt(norm));
            }
            if (axis.Z is 1) onPlane = Vector3.UnitX;
            else onPlane = new Vector3(-axis.Y, axis.X, 0).Normalize();
            return onPlane;
        }

        private static Plane BuildPlane(Vector3 pos, Vector3 normal)
        {
            Vector4 baseForPlane = Vector4.Zero;
            normal.Normalize();
            baseForPlane.W = Vector3.Dot(pos, normal);
            baseForPlane.X = normal.X;
            baseForPlane.Y = normal.Y;
            baseForPlane.Z = normal.Z;
            return new Plane(baseForPlane);
        }

        private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalize();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, new Vector3(plane.X, plane.Y, plane.Z)).Normalize();
            var angle = NumericsExtensions.CalculateAngle(localPos, rotVectSource.Value);//(float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= NumericsExtensions.Clamp((Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f, -0.9999f, 0.9999f);
        }

        /*private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalized();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, plane.Xyz).Normalized();
            var angle = (float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));//(float)Math.Acos(NumericsExtensions.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= (Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f;
        }*/

        private static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            var angle = Math.Atan2(vector1.Y, vector1.X) - Math.Atan2(vector2.Y, vector2.X);// * (180 / Math.PI);
            if (angle < 0)
            {
                //angle = angle + NumericsExtensions.TwoPi;
            }
            return angle;
        }

        private static float ComputeLength(ref Ray ray, Vector3 pos)
        {
            //var plane = BuildPlane(pos, -ray.direction);
            //var intersectPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
            return ray.IntersectPlane(ref transformationPlane);
        }
    }
}

/*var v = GetAxis();
            var len = ComputeLength(ref ray, entity.Position);
            if (!relativeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                relativeOrigin = (planeOrigin - entity.Position);// * (1f / (1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix)));
            }
            var newPos = ray.origin + ray.direction * len;
            var newOrigin = newPos - relativeOrigin.Value;// * (1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix));
            var delta = newOrigin - entity.Position;
            var lenOnAxis = Vector3.Dot(v, delta);
            delta = v * lenOnAxis;
            var baseVector = planeOrigin.Value - entity.Position;
            float ratio = Vector3.Dot(v, baseVector + delta) / Vector3.Dot(v, baseVector);
            var scale = ratio * v;
            scale.X = scale.X == 0 ? entity.Scale.X : scale.X;
            scale.Y = scale.Y == 0 ? entity.Scale.Y : scale.Y;
            scale.Z = scale.Z == 0 ? entity.Scale.Z : scale.Z;
            entity.Scale = scale;*/