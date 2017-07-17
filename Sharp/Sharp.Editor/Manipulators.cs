using System;
using OpenTK;
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
        internal static Matrix4 startMat;
        internal static ChangeValueCommand newCommand;

        public static void DrawCombinedGizmos(Entity entity)
        {
            DrawCombinedGizmos(entity, (selectedAxisId == 1 ? selectedColor : xColor), (selectedAxisId == 2 ? selectedColor : yColor), (selectedAxisId == 3 ? selectedColor : zColor), (selectedAxisId == 4 ? selectedColor : xColor), (selectedAxisId == 5 ? selectedColor : yColor), (selectedAxisId == 6 ? selectedColor : zColor), (selectedAxisId == 7 ? selectedColor : xColor), (selectedAxisId == 8 ? selectedColor : yColor), (selectedAxisId == 9 ? selectedColor : zColor), 3f);
        }

        public static void DrawCombinedGizmos(Entity entity, Color xColor, Color yColor, Color zColor, Color xRotColor, Color yRotColor, Color zRotColor, Color xScaleColor, Color yScaleColor, Color zScaleColor, float thickness = 5f)
        {
            float scale = (Camera.main.entityObject.Position - entity.position).Length / 25f;
            DrawHelper.DrawTranslationGizmo(thickness, scale, xColor, yColor, zColor);
            DrawHelper.DrawRotationGizmo(thickness, scale, xRotColor, yRotColor, zRotColor);
            DrawHelper.DrawScaleGizmo(thickness, scale, xScaleColor, yScaleColor, zScaleColor, scaleOffset);
            if (rotVectSource.HasValue)
            {
                var cross = Vector3.Cross(startAxis, currentAngle);
                var fullAngle = Vector3.CalculateAngle(startAxis, currentAngle);
                var incAngle = fullAngle / halfCircleSegments;
                var vectors = new Vector3[halfCircleSegments + 1];
                vectors[0] = new Vector3(0, 0, 0);
                for (uint i = 1; i < halfCircleSegments + 1; i++)
                {
                    var rotateMat = Matrix3.CreateFromAxisAngle(cross, incAngle * (i - 1));
                    var rotatedVec = Vector3.Transform(startAxis, rotateMat) * 3f * scale;
                    vectors[i] = new Vector3(rotatedVec.X, rotatedVec.Y, rotatedVec.Z);
                }

                var rot = Matrix4.CreateFromQuaternion(startMat.ExtractRotation().Inverted());
                var mat = rot * startMat * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
                var fill = new Color(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
                DrawHelper.DrawFilledPolyline(thickness, 3f * scale, fill, ref mat, ref vectors);
            }
        }

        public static void Reset()
        {
            if (rotVectSource.HasValue || relativeOrigin.HasValue || planeOrigin.HasValue)
                newCommand.StoreCommand();

            rotVectSource = null;
            rotAngleOrigin = null;
            planeOrigin = null;
            relativeOrigin = null;
            scaleOrigin = null;
            scaleOffset = Vector3.Zero;
            currentAngle = Vector3.Zero;
            angle = 0;
            startAxis = Vector3.Zero;
            startMat = Matrix4.Zero;
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
                v = Vector3.TransformVector(v, entity.ModelMatrix.ClearTranslation().ClearScale()).Normalized();
            var len = ComputeLength(ref ray, entity.Position);
            if (!relativeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                relativeOrigin = (planeOrigin - entity.Position) * (1f / (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix)));
                newCommand = new ChangeValueCommand((o) => { Squid.UI.isDirty = true; entity.Position = (Vector3)o; }, entity.position);
            }
            var newPos = ray.origin + ray.direction * len;
            var newOrigin = newPos - relativeOrigin.Value * (0.1f * GetUniform(entity.Position, Camera.main.ProjectionMatrix));
            var delta = newOrigin - entity.Position;
            var lenOnAxis = Vector3.Dot(delta, v);
            delta = v * lenOnAxis;
            entity.Position += delta;
            newCommand.newValue = entity.position;
        }

        //private static Quaternion startRot = Quaternion.Identity;

        public static void HandleRotation(Entity entity, ref Ray ray)//bugged when rescaled
        {
            var v = GetAxis();
            var unchangedV = v;
            if (!SceneView.globalMode)
                v = Vector3.Transform(v, entity.ModelMatrix.ExtractRotation()).Normalized();

            var plane = BuildPlane(entity.Position, v);
            transformationPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
            var len = ComputeLength(ref ray, entity.Position);

            if (!rotVectSource.HasValue)
            {
                rotVectSource = constrain((ray.origin + ray.direction * len - entity.Position), v).Normalized();
                rotAngleOrigin = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
                newCommand = new ChangeValueCommand((o) => { Squid.UI.isDirty = true; entity.Rotation = (Vector3)o; }, entity.rotation);
                startMat = entity.ModelMatrix;
                startAxis = (ray.origin + ray.direction * len - entity.Position).Normalized();
            }
            /* var currentVect = constrain((ray.origin + ray.direction * len - entity.Position), v).Normalized();
             var cross = Vector3.Cross(rotVectSource.Value, currentVect);
             var dot = Vector3.Dot(rotVectSource.Value, currentVect);
             var quat = new Quaternion(cross, 1 + dot).Normalized();
             entity.Rotation = Entity.rotationMatrixToEulerAngles(Matrix4.CreateFromQuaternion(quat)) * (180.0f / MathHelper.Pi);*///entity.ModelMatrix.ExtractRotation().Normalized() *
            angle = ComputeAngleOnPlane(entity, ref ray, ref transformationPlane);
            //entity.ModelMatrix.Inverted().ExtractRotation();
            var rotAxis = Vector3.Transform(transformationPlane.Xyz, entity.ModelMatrix.Inverted().ExtractRotation()).Normalized();
            var deltaRot = Matrix4.CreateFromAxisAngle(rotAxis, angle - rotAngleOrigin.Value);
            entity.Rotation = Entity.rotationMatrixToEulerAngles(deltaRot * entity.ModelMatrix) * (180.0f / MathHelper.Pi);
            currentAngle = (ray.origin + ray.direction * len - entity.Position).Normalized();
            rotAngleOrigin = angle;
            newCommand.newValue = entity.rotation;
            //rotvectsource = constrain((ray.origin + ray.direction * len - entity.Position), v).Normalized();
            //startRot = quat * startRot;
        }

        public static void HandleScale(Entity entity, ref Ray ray)
        {
            var v = GetAxis();
            if (!SceneView.globalMode)
                v = Vector3.TransformVector(v, entity.ModelMatrix.ClearTranslation().ClearScale()).Normalized();
            var len = ComputeLength(ref ray, entity.Position);
            if (!planeOrigin.HasValue)
            {
                planeOrigin = ray.origin + ray.direction * len;
                scaleOrigin = entity.Scale;
                newCommand = new ChangeValueCommand((o) => { Squid.UI.isDirty = true; entity.Scale = (Vector3)o; }, entity.scale);
            }
            var newPos = ray.origin + ray.direction * len;
            var delta = (newPos - entity.Position).Length / (planeOrigin.Value - entity.Position).Length;
            scaleOffset = newPos * v;
            entity.Scale = scaleOrigin.Value + v * delta - v;
            newCommand.newValue = entity.scale;
        }

        private static float GetUniform(Vector3 pos, Matrix4 mat)
        {
            var trf = new Vector4(pos, 1f);
            trf = Vector4.Transform(trf, mat);
            return trf.W;
        }

        private static Vector3 constrain(Vector3 vec, Vector3 axis)
        {
            var onPlane = Vector3.Subtract(vec, axis * Vector3.Dot(axis, vec));
            var norm = onPlane.LengthSquared;
            if (norm > 0)
            {
                if (onPlane.Z < 0) onPlane = -onPlane;
                return onPlane * (1 / (float)Math.Sqrt(norm));
            }
            if (axis.Z is 1) onPlane = Vector3.UnitX;
            else onPlane = new Vector3(-axis.Y, axis.X, 0).Normalized();
            return onPlane;
        }

        private static System.Numerics.Plane BuildPlane(Vector3 pos, Vector3 normal)
        {
            System.Numerics.Vector4 baseForPlane = System.Numerics.Vector4.Zero;
            normal.Normalize();
            baseForPlane.W = Vector3.Dot(normal, pos);
            baseForPlane.X = normal.X;
            baseForPlane.Y = normal.Y;
            baseForPlane.Z = normal.Z;
            return new System.Numerics.Plane(baseForPlane);
        }

        private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalized();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, plane.Xyz).Normalized();
            var angle = Vector3.CalculateAngle(localPos, rotVectSource.Value);//(float)Math.Acos(MathHelper.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= MathHelper.Clamp((Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f, -0.9999f, 0.9999f);
        }

        /*private static float ComputeAngleOnPlane(Entity entity, ref Ray ray, ref Vector4 plane)
        {
            var len = ray.IntersectPlane(ref plane);
            var localPos = (ray.origin + ray.direction * len - entity.Position).Normalized();
            var perpendicularVect = Vector3.Cross(rotVectSource.Value, plane.Xyz).Normalized();
            var angle = (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));//(float)Math.Acos(MathHelper.Clamp(Vector3.Dot(localPos, rotVectSource.Value), -0.9999f, 0.9999f));

            return angle *= (Vector3.Dot(localPos, perpendicularVect) < 0.0f) ? 1.0f : -1.0f;
        }*/

        private static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            var angle = Math.Atan2(vector1.Y, vector1.X) - Math.Atan2(vector2.Y, vector2.X);// * (180 / Math.PI);
            if (angle < 0)
            {
                //angle = angle + MathHelper.TwoPi;
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