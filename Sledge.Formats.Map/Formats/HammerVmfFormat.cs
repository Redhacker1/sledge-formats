using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Sledge.Formats.Map.Formats.VmfObjects;
using Sledge.Formats.Map.Objects;
using Sledge.Formats.Valve;

namespace Sledge.Formats.Map.Formats
{
    public class HammerVmfFormat : IMapFormat
    {
        public string Name => "Hammer VMF";
        public string Description => "The .vmf file format used by Valve Hammer Editor 4.";
        public string ApplicationName => "Hammer";
        public string Extension => "vmf";
        public string[] AdditionalExtensions => new[] { "vmx" };
        public string[] SupportedStyleHints => new string[0];

        readonly SerialisedObjectFormatter _formatter;

        public HammerVmfFormat()
        {
            _formatter = new SerialisedObjectFormatter();
        }

        public MapFile Read(Stream stream)
        {
            MapFile map = new MapFile();

            List<SerialisedObject> objs = new List<SerialisedObject>();
            foreach (SerialisedObject so in _formatter.Deserialize(stream))
            {
                switch (so.Name?.ToLower())
                {
                    case "visgroups":
                        LoadVisgroups(map, so);
                        break;
                    case "cameras":
                        LoadCameras(map, so);
                        break;
                    case "world":
                    case "entity":
                        objs.Add(so);
                        break;
                    default:
                        map.AdditionalObjects.Add(so);
                        break;
                }
            }
            LoadWorld(map, objs);

            return map;
        }

        #region Read

        void LoadWorld(MapFile map, List<SerialisedObject> objects)
        {
            List<VmfObject> vos = objects.Select(VmfObject.Deserialise).Where(vo => vo != null).ToList();
            VmfWorld world = vos.OfType<VmfWorld>().FirstOrDefault() ?? new VmfWorld(new SerialisedObject("world"));

            // A map of loaded object -> vmf id
            Dictionary<MapObject, int> mapToSource = new Dictionary<MapObject, int>();
            world.Editor.Apply(map.Worldspawn);
            mapToSource.Add(map.Worldspawn, world.ID);

            map.Worldspawn.ClassName = world.ClassName;
            map.Worldspawn.SpawnFlags = world.SpawnFlags;
            foreach (KeyValuePair<string, string> wp in world.Properties) map.Worldspawn.Properties[wp.Key] = wp.Value;

            List<VmfObject> tree = new List<VmfObject>();

            foreach (VmfObject vo in vos)
            {
                if (vo.Editor.ParentID == 0) vo.Editor.ParentID = world.ID;

                // Flatten the tree (nested hiddens -> no more hiddens)
                // (Flat tree includes self as well)
                List<VmfObject> flat = vo.Flatten().ToList();

                // Set the default parent id for all the child objects
                foreach (VmfObject child in flat)
                {
                    if (child.Editor.ParentID == 0) child.Editor.ParentID = vo.ID;
                }

                // Add the objects to the tree
                tree.AddRange(flat);
            }

            world.Editor.ParentID = 0;
            tree.Remove(world);

            // All objects should have proper ids by now, get rid of anything with parentid 0 just in case
            Dictionary<int, List<VmfObject>> grouped = tree.GroupBy(x => x.Editor.ParentID).ToDictionary(x => x.Key, x => x.ToList());

            // Step through each level of the tree and add them to their parent branches
            List<MapObject> leaves = new List<MapObject> { map.Worldspawn };

            // Use a iteration limit of 1000. If the tree's that deep, I don't want to load your map anyway...
            for (int i = 0; i < 1000 && leaves.Any(); i++) // i.e. while (leaves.Any())
            {
                List<MapObject> newLeaves = new List<MapObject>();
                foreach (MapObject leaf in leaves)
                {
                    int sourceId = mapToSource[leaf];
                    if (!grouped.ContainsKey(sourceId)) continue;

                    List<VmfObject> items = grouped[sourceId];

                    // Create objects from items
                    foreach (VmfObject item in items)
                    {
                        MapObject mapObject = item.ToMapObject();
                        mapToSource.Add(mapObject, item.ID);
                        leaf.Children.Add(mapObject);
                        newLeaves.Add(mapObject);
                    }
                }
                leaves = newLeaves;
            }

            // Now we should have a nice neat hierarchy of objects
        }

        void LoadCameras(MapFile map, SerialisedObject so)
        {
            int activeCam = so.Get("activecamera", 0);

            List<SerialisedObject> cams = so.Children.Where(x => string.Equals(x.Name, "camera", StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (int i = 0; i < cams.Count; i++)
            {
                SerialisedObject cm = cams[i];
                map.Cameras.Add(new Camera
                {
                    EyePosition = cm.Get("position", Vector3.Zero),
                    LookPosition = cm.Get("look", Vector3.UnitX),
                    IsActive = activeCam == i
                });
            }
        }

        void LoadVisgroups(MapFile map, SerialisedObject so)
        {
            Visgroup vis = new Visgroup();
            LoadVisgroupsRecursive(so, vis);
            map.Visgroups.AddRange(vis.Children);
        }

        void LoadVisgroupsRecursive(SerialisedObject so, Visgroup parent)
        {
            foreach (SerialisedObject vg in so.Children.Where(x => string.Equals(x.Name, "visgroup", StringComparison.InvariantCultureIgnoreCase)))
            {
                Visgroup v = new Visgroup
                {
                    Name = vg.Get("name", ""),
                    ID = vg.Get("visgroupid", -1),
                    Color = vg.GetColor("color"),
                    Visible = true
                };
                LoadVisgroupsRecursive(vg, v);
                parent.Children.Add(v);
            }
        }

        #endregion

        public void Write(Stream stream, MapFile map, string styleHint)
        {
            List<SerialisedObject> list = new List<SerialisedObject>();

            list.AddRange(map.AdditionalObjects);

            SerialisedObject visObj = new SerialisedObject("visgroups");
            SaveVisgroups(map.Visgroups, visObj);
            list.Add(visObj);

            SaveWorld(map, list);
            SaveCameras(map, list);

            _formatter.Serialize(stream, list);
        }

        #region Write

        static string FormatVector3(Vector3 c)
        {
            return $"{FormatDecimal(c.X)} {FormatDecimal(c.Y)} {FormatDecimal(c.Z)}";
        }

        static string FormatDecimal(float d)
        {
            return d.ToString("0.00####", CultureInfo.InvariantCulture);
        }

        void SaveVisgroups(IEnumerable<Visgroup> visgroups, SerialisedObject parent)
        {
            foreach (Visgroup visgroup in visgroups)
            {
                SerialisedObject vgo = new SerialisedObject("visgroup");
                vgo.Set("visgroupid", visgroup.ID);
                vgo.SetColor("color", visgroup.Color);
                SaveVisgroups(visgroup.Children, vgo);
                parent.Children.Add(vgo);
            }
        }

        void SaveWorld(MapFile map, List<SerialisedObject> list)
        {
            // call the avengers

            int id = 1;
            Dictionary<MapObject, int> idMap = map.Worldspawn.FindAll().ToDictionary(x => x, x => id++);

            // Get the world, groups, and non-entity solids
            VmfWorld vmfWorld = new VmfWorld(map.Worldspawn);
            SerialisedObject worldObj = vmfWorld.ToSerialisedObject();
            SerialiseWorldspawnChildren(map.Worldspawn, worldObj, idMap, 0, map.Worldspawn.Children);
            list.Add(worldObj);

            // Entities are separate from the world
            List<SerialisedObject> entities = map.Worldspawn.FindAll().OfType<Entity>().Where(x => x != map.Worldspawn).Select(x => SerialiseEntity(x, idMap)).ToList();
            list.AddRange(entities);
        }

        void SerialiseWorldspawnChildren(Worldspawn worldspawn, SerialisedObject worldObj, Dictionary<MapObject, int> idMap, int groupId, List<MapObject> list)
        {
            foreach (MapObject c in list)
            {
                int cid = idMap[c];
                switch (c)
                {
                    case Entity _:
                        // Ignore everything underneath an entity
                        break;
                    case Group g:
                        VmfGroup sg = new VmfGroup(g, cid);
                        if (groupId != 0) sg.Editor.GroupID = groupId;
                        worldObj.Children.Add(sg.ToSerialisedObject());
                        SerialiseWorldspawnChildren(worldspawn, worldObj, idMap, cid, g.Children);
                        break;
                    case Solid s:
                        VmfSolid ss = new VmfSolid(s, cid);
                        if (groupId != 0) ss.Editor.GroupID = groupId;
                        worldObj.Children.Add(ss.ToSerialisedObject());
                        break;
                }
            }
        }

        SerialisedObject SerialiseEntity(MapObject obj, Dictionary<MapObject, int> idMap)
        {
            VmfObject self = VmfObject.Serialise(obj, idMap[obj]);
            if (self == null) return null;

            SerialisedObject so = self.ToSerialisedObject();

            foreach (Solid solid in obj.FindAll().OfType<Solid>())
            {
                VmfObject s = VmfObject.Serialise(solid, idMap[obj]);
                if (s != null) so.Children.Add(s.ToSerialisedObject());
            }

            return so;
        }

        void SaveCameras(MapFile map, List<SerialisedObject> list)
        {
            List<Camera> cams = map.Cameras;

            SerialisedObject so = new SerialisedObject("cameras");
            so.Set("activecamera", -1);

            for (int i = 0; i < cams.Count; i++)
            {
                Camera camera = cams[i];
                if (camera.IsActive) so.Set("activecamera", i);

                SerialisedObject vgo = new SerialisedObject("camera");
                vgo.Set("position", $"[{FormatVector3(camera.EyePosition)}]");
                vgo.Set("look", $"[{FormatVector3(camera.LookPosition)}]");
                so.Children.Add(vgo);
            }

            list.Add(so);
        }

        #endregion
    }
}
