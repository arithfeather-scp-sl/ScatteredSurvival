using System.Collections.Generic;
using System.Globalization;
using Smod2.API;
using UnityEngine;

namespace ArithFeather.ScatteredSurvival
{
    public static class Tools
    {
        /// <summary>
        /// Converts Vector to Vector3
        /// </summary>
        public static Vector3 VectorTo3(Vector v) => new Vector3(v.x, v.y, v.z);
        /// <summary>
        /// Converts Vector3 to Vector
        /// </summary>
        public static Vector Vec3ToVector(Vector3 v) => new Vector(v.x, v.y, v.z);

        public static Vector ParseRot(string vectorData)
        {
            string[] vector = vectorData.Split(',');
            if (vector.Length != 3)
            {
                return null;
            }
            if (!float.TryParse(vector[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(vector[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                || !float.TryParse(vector[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return null;
            }

            return new Vector(x, y, z);
        }
    }

    public class DistinctRoomComparer : IEqualityComparer<Room>
    {
        public bool Equals(Room x, Room y)
        {
            return y != null && (x != null && x.RoomType == y.RoomType);
        }
        public int GetHashCode(Room obj)
        {
            return obj.RoomType.GetHashCode();
        }
    }
}