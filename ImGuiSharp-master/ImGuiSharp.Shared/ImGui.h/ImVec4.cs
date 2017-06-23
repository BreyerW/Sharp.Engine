﻿namespace ImGui
{
    using System.Runtime.CompilerServices;

    public struct ImVec4
    {
        public float x, y, z, w;
        public ImVec4(float _x, float _y, float _z, float _w) { x = _x; y = _y; z = _z; w = _w; }

        public static bool operator ==(ImVec4 lhs, ImVec4 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w; }
        public static bool operator !=(ImVec4 lhs, ImVec4 rhs) { return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z || lhs.w != rhs.w; }
        public static ImVec4 operator *(ImVec4 lhs, float rhs)
        {
            lhs.x *= rhs;
            lhs.y *= rhs;
            lhs.z *= rhs;
            lhs.w *= rhs;
            return lhs;
        }
        public static ImVec4 operator -(ImVec4 lhs, ImVec4 rhs) { return new ImVec4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w); }

        public override int GetHashCode()
        {
            var hash = this.x.GetHashCode();
            hash = ImGui.CombineHashCodes(hash, y.GetHashCode());
            hash = ImGui.CombineHashCodes(hash, z.GetHashCode());
            hash = ImGui.CombineHashCodes(hash, w.GetHashCode());
            return hash;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this Vector3 instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this Vector3; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is ImVec4))
                return false;
            return Equals((ImVec4)obj);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Vector3 is equal to this Vector3 instance.
        /// </summary>
        /// <param name="other">The Vector3 to compare this instance to.</param>
        /// <returns>True if the other Vector3 is equal to this instance; False otherwise.</returns>
        public bool Equals(ImVec4 other)
        {
            return x == other.x &&
                   y == other.y &&
                   z == other.z &&
                   w == other.w;
        }

        public override string ToString()
        {
            return string.Format("{{ X: {0}, Y: {1}, Z: {2}, W: {3} }}", x, y, z, w);
        }
    };
}
