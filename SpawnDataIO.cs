using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Smod2.API;

namespace ArithFeather.ScatteredSurvival
{
    public static class SpawnDataIO
    {
        public static SpawnData Open(string filePath)
        {
            var data = new SpawnData();

            if (FileManager.FileExists(filePath))
            {
                using (StreamReader reader = File.OpenText(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        var item = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }

                        if (item[0] == '#')
                        {
                            continue;
                        }

                        string[] sData = item.Split(':');

                        if (sData.Length == 0)
                        {
                            continue;
                        }
                        if (sData.Length != 4)
                        {
                            continue;
                        }
                        var room = sData[0];
                        if (!Enum.TryParse(sData[1].Trim(), out ZoneType zone))
                        {
                            continue;
                        }
                        Vector position = VectorParser(sData[2].Trim());
                        if (position == null)
                        {
                            continue;
                        }
                        Vector rotation = VectorParser(sData[3].Trim());
                        if (rotation == null)
                        {
                            continue;
                        }

                        data.Points.Add(new SpawnPoint(room, zone, position, rotation));
                    }
                }
            }
            return data;
        }

        public static void Save(SpawnData data, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(File.Create(filePath)))
            {
                foreach (SpawnPoint point in data.Points)
                {
                    writer.WriteLine(SpawnInfoToStr(point));
                }
            }
        }

        private static string SpawnInfoToStr(SpawnPoint spawnInfo)
        {
            return spawnInfo.RoomType + ':' +
                              spawnInfo.ZoneType +
                        ':' + spawnInfo.Position.x.ToString(CultureInfo.InvariantCulture) +
                        ',' + spawnInfo.Position.y.ToString(CultureInfo.InvariantCulture) +
                        ',' + spawnInfo.Position.z.ToString(CultureInfo.InvariantCulture) +
                        ':' + spawnInfo.Rotation.x.ToString(CultureInfo.InvariantCulture) +
                        ',' + spawnInfo.Rotation.y.ToString(CultureInfo.InvariantCulture) +
                        ',' + spawnInfo.Rotation.z.ToString(CultureInfo.InvariantCulture);
        }

        private static Vector VectorParser(string vectorData)
        {
            string[] vector = vectorData.Split(',');
            if (vector.Length != 3)
            {
                return null;
            }
            if (!float.TryParse(vector[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(vector[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                || !float.TryParse(vector[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return null;
            }
            return new Vector(x, y, z);
        }

        public static void LoadItemRoomData(ItemRoomData data)
        {
            if (!File.Exists("sm_plugins/SSRooms.txt"))
            {
                using (var writer = new StreamWriter(File.Create("sm_plugins/SSRooms.txt")))
                {
                    var uniqueRooms = new List<CustomRoom>();
                    var roomCount = data.Rooms.Count;
                    for (var i = 0; i < roomCount; i++)
                    {
                        var room = data.Rooms[i];
                        var foundRoom = false;

                        for (int j = 0; j < uniqueRooms.Count; j++)
                        {
                            if (room.Name == uniqueRooms[j].Name)
                            {
                                foundRoom = true;
                                break;
                            }
                        }

                        if (!foundRoom)
                        {
                            uniqueRooms.Add(room);
                        }
                    }

                    string s = "#ItemRarities:";
                    var itemRarities = Enum.GetNames(typeof(ItemRarity));
                    var itemCount = itemRarities.Length;
                    for (int i = 0; i < itemCount; i++)
                    {
                        var item = itemRarities[i];
                        s += "|" + item + "=" + i;
                    }
                    writer.WriteLine(s + "|");
                    writer.WriteLine("BaseItemSpawnQueue:4,9,4,6,3,4,2,7,4,6,5,1,4,7,2,4,6,9,4,5,2,4,5,4,1,7,2,3,4,6,7,9,8");
                    writer.WriteLine("SpawnItemsSpawnQueue:0,4,9");
                    writer.WriteLine("NumberItemsOnDeath:5");
                    writer.WriteLine("NumberItemsOnStart:20");
                    writer.WriteLine();
                    writer.WriteLine("#RoomName:NumberOfItemsMax");
                    foreach (CustomRoom room in uniqueRooms)
                    {
                        writer.WriteLine(room.Name + ":2");
                    }
                }
            }
            else
            {
                using (StreamReader reader = File.OpenText("sm_plugins/SSRooms.txt"))
                {
                    while (!reader.EndOfStream)
                    {
                        var item = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }

                        if (item[0] == '#')
                        {
                            continue;
                        }

                        string[] sData = item.Split(':');

                        if (sData.Length == 0)
                        {
                            continue;
                        }

                        switch (sData[0])
                        {
	                        case "BaseItemSpawnQueue":
	                        {
		                        var items = sData[1].Split(',');
		                        var itemCount = items.Length;
		                        int[] intItems = new int[itemCount];
		                        for (int i = 0; i < itemCount; i++)
		                        {
			                        intItems[i] = int.Parse(items[i]);
		                        }
		                        data.BaseItemSpawnQueue = intItems;
		                        break;
	                        }

	                        case "SpawnItemsSpawnQueue":
	                        {
		                        var items = sData[1].Split(',');
		                        var itemCount = items.Length;
		                        int[] intItems = new int[itemCount];
		                        for (int i = 0; i < itemCount; i++)
		                        {
			                        intItems[i] = int.Parse(items[i]);
		                        }
		                        data.SafeItemsSpawnQueue = intItems;
		                        break;
	                        }

	                        case "NumberItemsOnDeath":
		                        data.NumberItemsOnDeath = int.Parse(sData[1]);
		                        break;
	                        case "NumberItemsOnStart":
		                        data.NumberItemsOnStart = int.Parse(sData[1]);
		                        break;
	                        default:
		                        data.AddRoomData(sData[0], int.Parse(sData[1]));
		                        break;
                        }
                    }
                }
            }
        }
    }
}