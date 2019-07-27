using System;
using System.Collections.Generic;
using Smod2.API;
using UnityEngine;
using Random = System.Random;

namespace ArithFeather.ScatteredSurvival
{
    [Serializable]
    public class SpawnData
    {
        private List<SpawnPoint> points = new List<SpawnPoint>();

        public List<SpawnPoint> Points
        {
            get
            {
                if (points == null)
                {
                    points = new List<SpawnPoint>();
                }
                return points;
            }
        }
    }

    public class CustomRoom
    {
        public Transform Transform;
        public string Name;
        public ZoneType Zone;

        public CustomRoom(Transform transform, string name, ZoneType zone)
        {
            Transform = transform;
            Name = name;
            Zone = zone;
        }

        public List<ItemSpawnPoint> ItemSpawnPoints = new List<ItemSpawnPoint>();

        private int currentItemsSpawned;
        public int CurrentItemsSpawned
        {
            get => currentItemsSpawned;
            set
            {
                currentItemsSpawned = value;
                AtMaxItemSpawns = (value >= MaxItemsAllowed);
            }
        }
        
        public int MaxItemsAllowed;
        public bool IsSafe;
        public bool IsFree = true;

        public bool AtMaxItemSpawns { get; private set; }
    }

    public class ItemSpawnPoint : SpawnPoint
    {
        public ItemSpawnPoint(string roomType, ZoneType zoneType, Vector position, Vector rotation) : base(roomType, zoneType, position, rotation) { }

        private Pickup itemPickup;
        public Pickup ItemPickup
        {
            set
            {
                itemPickup = value;
                IsFree = false;
            }
            get => itemPickup;
        }
        public bool IsFree = true;
    }

    [Serializable]
    public class SpawnPoint
    {
        public SpawnPoint(string roomType, ZoneType zoneType, Vector position, Vector rotation)
        {
            RoomType = roomType;
            ZoneType = zoneType;
            Position = position;
            Rotation = rotation;
        }

        public string RoomType { get; set; }
        public ZoneType ZoneType { get; set; }
        public Vector Position { get; set; }
        public Vector Rotation { get; set; }
		public bool IsFreePoint { get; set; }
    }

    public static class Extensions
    {
        public static Random Random = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
	        for (var i = list.Count - 1; i > 1; i--)
            {
                var rnd = Random.Next(i + 1);

                var value = list[rnd];
                list[rnd] = list[i];
                list[i] = value;
            }
        }
    }

    public struct PosVectorPair
    {
        public readonly Vector Position;
        public readonly Vector Rotation;
        public PosVectorPair(Vector position, Vector rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    #region Item Spawning

    /// <summary>
    /// Start 123 444 55 66 77 8 9999
    /// LCZ  00 11 2 3 444 55 6 7 99 - 15 per spawn
    /// ENT  00 11 2 3 444 55 6 7 99 - 15 per spawn
    /// HCZ  123
    /// </summary>
    public enum ItemRarity : byte
    {
        /// <summary>
        /// Research Supervisor
        /// </summary>
        KeyCheckpoint = 0,

        /// <summary>
        /// Guard, Cadet, Containment Engineer
        /// </summary>
        KeyWeapons12Escape = 1,

        /// <summary>w
        /// Facility Manager, Lieutenant
        /// </summary>
        KeyManager = 2,

        /// <summary>
        /// MTF Commander, 05-Level
        /// </summary>
        KeyAdmin = 3,

        /// <summary>
        /// Radio, Medkit
        /// </summary>
        RadioMedkit = 4,

        /// <summary>
        /// COM15, 
        /// </summary>
        Pistol = 5,

        /// <summary>
        /// MP7, Project90
        /// </summary>
        /// 
        SMG = 6,

        /// <summary>
        /// Logicer, MTF-E11-SR
        /// </summary>
        Rifles = 7,

        /// <summary>
        /// MicroHID
        /// </summary>
        HID = 8,

        /// <summary>
        /// Frag
        /// </summary>
        Grenade = 9
    }

    #endregion
}