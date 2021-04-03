using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Sledge.Formats.Map.Objects;
using Path = Sledge.Formats.Map.Objects.Path;

namespace Sledge.Formats.Map.Formats
{
    public class JackhammerJmfFormat : IMapFormat
    {
        public string Name => "Jackhammer JMF";
        public string Description => "The .jmf file format used by Jackhammer and JACK.";
        public string ApplicationName => "JACK";
        public string Extension => "jmf";
        public string[] AdditionalExtensions => new[] { "jmf" };
        public string[] SupportedStyleHints => new[] { "" };

        public MapFile Read(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                // JMF header test
                string header = br.ReadFixedLengthString(Encoding.ASCII, 4);
                Util.Assert(header == "JHMF", $"Incorrect JMF header. Expected 'JHMF', got '{header}'.");

                // Only JHMF version 121 is supported for the moment.
                int version = br.ReadInt32();
                Util.Assert(version == 121, $"Unsupported JMF version number. Expected 121, got {version}.");

                // Appears to be an array of locations to export to .map
                int numExportStrings = br.ReadInt32();
                for (int i = 0; i < numExportStrings; i++)
                {
                    ReadString(br);
                }

                MapFile map = new MapFile();

                List<JmfGroup> groups = ReadGroups(map, br);
                ReadVisgroups(map, br);
                Vector3 cordonLow = br.ReadVector3();
                Vector3 cordonHigh = br.ReadVector3();
                ReadCameras(map, br);
                ReadPaths(map, br);
                List<JmfEntity> entities = ReadEntities(map, br);

                BuildTree(map, groups, entities);

                return map;
            }
        }

        #region Read

        void BuildTree(MapFile map, IEnumerable<JmfGroup> groups, IReadOnlyCollection<JmfEntity> entities)
        {
            Dictionary<int, int> groupIds = new Dictionary<int, int>(); // file group id -> actual id
            Dictionary<int, MapObject> objTree = new Dictionary<int, MapObject>(); // object id -> object
            
            int currentId = 2; // worldspawn is 1
            objTree[1] = map.Worldspawn;

            JmfEntity worldspawnEntity = entities.FirstOrDefault(x => x.Entity.ClassName == "worldspawn");
            if (worldspawnEntity != null)
            {
                map.Worldspawn.Properties = worldspawnEntity.Entity.Properties;
                map.Worldspawn.Color = worldspawnEntity.Entity.Color;
                map.Worldspawn.SpawnFlags = worldspawnEntity.Entity.SpawnFlags;
                map.Worldspawn.Visgroups = worldspawnEntity.Entity.Visgroups;
            }
            
            // Jackhammer doesn't allow a group within an entity, so groups
            // will only be children of worldspawn or another group. We can
            // build the group hierarchy immediately.
            List<JmfGroup> groupList = groups.ToList();
            int groupCount = groupList.Count;
            while (groupList.Any())
            {
                List<JmfGroup> pcs = groupList.Where(x => x.ID == x.ParentID || x.ParentID == 0 || groupIds.ContainsKey(x.ParentID)).ToList();
                foreach (JmfGroup g in pcs)
                {
                    int gid = currentId++;
                    groupIds[g.ID] = gid;
                    groupList.Remove(g);

                    Group group = new Group
                    {
                        Color = g.Color
                    };

                    int parentObjId = g.ID == g.ParentID || g.ParentID == 0 ? 1 : groupIds[g.ParentID];
                    objTree[parentObjId].Children.Add(group);
                    objTree[gid] = group;
                }

                if (groupList.Count == groupCount) break; // no groups processed, can't continue
                groupCount = groupList.Count;
            }

            // For non-worldspawn solids, they are direct children of their entity.
            // For non-worldspawn entities, they're either a child of a group or of the worldspawn.
            foreach (JmfEntity entity in entities.Where(x => x != worldspawnEntity))
            {
                int parentId = groupIds.ContainsKey(entity.GroupID) ? groupIds[entity.GroupID] : 1;
                objTree[parentId].Children.Add(entity.Entity);
                
                // Put all the entity's solids straight underneath this entity
                entity.Entity.Children.AddRange(entity.Solids.Select(x => x.Solid));
            }

            // For worldspawn solids, they're either a child of a group or of the worldspawn.
            if (worldspawnEntity != null)
            {
                foreach (JmfSolid solid in worldspawnEntity.Solids)
                {
                    int parentId = groupIds.ContainsKey(solid.GroupID) ? groupIds[solid.GroupID] : 1;
                    objTree[parentId].Children.Add(solid.Solid);
                }
            }
        }

        List<JmfGroup> ReadGroups(MapFile map, BinaryReader br)
        {
            List<JmfGroup> groups = new List<JmfGroup>();

            int numGroups = br.ReadInt32();
            for (int i = 0; i < numGroups; i++)
            {
                JmfGroup g = new JmfGroup
                {
                    ID = br.ReadInt32(),
                    ParentID = br.ReadInt32(),
                    Flags = br.ReadInt32(),
                    NumObjects = br.ReadInt32(),
                    Color = br.ReadRGBAColour()
                };
                groups.Add(g);
            }

            return groups;
        }

        static void ReadVisgroups(MapFile map, BinaryReader br)
        {
            int numVisgroups = br.ReadInt32();
            for (int i = 0; i < numVisgroups; i++)
            {
                Visgroup vis = new Visgroup
                {
                    Name = ReadString(br),
                    ID = br.ReadInt32(),
                    Color = br.ReadRGBAColour(),
                    Visible = br.ReadBoolean()
                };
                map.Visgroups.Add(vis);
            }
        }

        void ReadCameras(MapFile map, BinaryReader br)
        {
            int numCameras = br.ReadInt32();
            for (int i = 0; i < numCameras; i++)
            {
                Camera vis = new Camera
                {
                    EyePosition = br.ReadVector3(),
                    LookPosition = br.ReadVector3()
                };
                br.ReadInt32(); // something
                br.ReadInt32(); // something 2
                map.Cameras.Add(vis);
            }
        }

        void ReadPaths(MapFile map, BinaryReader br)
        {
            int numPaths = br.ReadInt32();
            for (int i = 0; i < numPaths; i++)
            {
                map.Paths.Add(ReadPath(br));
            }
        }

        static Path ReadPath(BinaryReader br)
        {
            Path path = new Path
            {
                Type = ReadString(br),
                Name = ReadString(br),
                Direction = (PathDirection) br.ReadInt32()
            };
            br.ReadInt32(); // flags
            br.ReadRGBAColour(); // colour

            int numNodes = br.ReadInt32();
            for (int i = 0; i < numNodes; i++)
            {
                string name = ReadString(br);
                string fire = ReadString(br); // fire on pass
                PathNode node = new PathNode
                {
                    Name = name,
                    Position = br.ReadVector3()
                };

                if (!string.IsNullOrWhiteSpace(fire)) node.Properties["message"] = fire;

                Vector3 angles = br.ReadVector3();
                node.Properties["angles"] = $"{angles.X} {angles.Y} {angles.Z}";

                node.Properties["spawnflags"] = br.ReadInt32().ToString();

                br.ReadRGBAColour(); // colour

                int numProps = br.ReadInt32();
                for (int j = 0; j < numProps; j++)
                {
                    string key = ReadString(br);
                    string value = ReadString(br);
                    if (key != null && value != null) node.Properties[key] = value;
                }

                path.Nodes.Add(node);
            }
            return path;
        }

        List<JmfEntity> ReadEntities(MapFile map, BinaryReader br)
        {
            List<JmfEntity> entities = new List<JmfEntity>();
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                JmfEntity ent = new JmfEntity
                {
                    Entity = new Entity
                    {
                        ClassName = ReadString(br)
                    }
                };

                Vector3 origin = br.ReadVector3();
                ent.Entity.Properties["origin"] = $"{origin.X} {origin.Y} {origin.Z}";

                ent.Flags = br.ReadInt32();
                ent.GroupID = br.ReadInt32();
                br.ReadInt32(); // group id again
                ent.Entity.Color = br.ReadRGBAColour();
                
                // useless (?) list of 13 strings
                for (int i = 0; i < 13; i++) ReadString(br);

                ent.Entity.SpawnFlags = br.ReadInt32();

                br.ReadBytes(76); // unknown (!)

                int numProps = br.ReadInt32();
                for (int i = 0; i < numProps; i++)
                {
                    string key = ReadString(br);
                    string value = ReadString(br);
                    if (key != null && value != null) ent.Entity.Properties[key] = value;
                }

                ent.Entity.Visgroups = new List<int>();

                int numVisgroups = br.ReadInt32();
                for (int i = 0; i < numVisgroups; i++)
                {
                    ent.Entity.Visgroups.Add(br.ReadInt32());
                }

                int numSolids = br.ReadInt32();
                for (int i = 0; i < numSolids; i++)
                {
                    ent.Solids.Add(ReadSolid(map, br));
                }

                entities.Add(ent);
            }

            return entities;
        }

        JmfSolid ReadSolid(MapFile map, BinaryReader br)
        {
            JmfSolid solid = new JmfSolid
            {
                Solid = new Solid()
            };

            int numPatches = br.ReadInt32();
            solid.Flags = br.ReadInt32();
            solid.GroupID = br.ReadInt32();
            br.ReadInt32(); // group id again
            solid.Solid.Color = br.ReadRGBAColour();
            
            int numVisgroups = br.ReadInt32();
            for (int i = 0; i < numVisgroups; i++)
            {
                solid.Solid.Visgroups.Add(br.ReadInt32());
            }

            int numFaces = br.ReadInt32();
            for (int i = 0; i < numFaces; i++)
            {
                solid.Solid.Faces.Add(ReadFace(br));
            }

            for (int i = 0; i < numPatches; i++)
            {
                solid.Solid.Meshes.Add(ReadPatch(br));
            }

            return solid;
        }

        Face ReadFace(BinaryReader br)
        {
            Face face = new Face();

            br.ReadInt32(); // something

            int numVertices = br.ReadInt32();
            ReadSurfaceProperties(face, br);

            Vector3 norm = br.ReadVector3();
            float distance = br.ReadSingle();
            face.Plane = new Plane(norm, distance);
            
            br.ReadInt32(); // something 2

            for (int i = 0; i < numVertices; i++)
            {
                br.ReadVector3(); // texture coordinate
                face.Vertices.Add(br.ReadVector3());
            }

            return face;
        }

        Mesh ReadPatch(BinaryReader br)
        {
            Mesh mesh = new Mesh
            {
                Width = br.ReadInt32(),
                Height = br.ReadInt32(),
                
            };

            ReadSurfaceProperties(mesh, br);

            br.ReadInt32(); // something

            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    MeshPoint point = new MeshPoint
                    {
                        X = i,
                        Y = j,
                        Position = br.ReadVector3(),
                        Normal = br.ReadVector3(),
                        Texture = br.ReadVector3()
                    };

                    if (i < mesh.Width && j < mesh.Height)
                    {
                        mesh.Points.Add(point);
                    }
                }
            }

            return mesh;
        }

        void ReadSurfaceProperties(Surface surface, BinaryReader br)
        {
            surface.UAxis = br.ReadVector3();
            surface.XShift = br.ReadSingle();
            surface.VAxis = br.ReadVector3();
            surface.YShift = br.ReadSingle();
            surface.XScale = br.ReadSingle();
            surface.YScale = br.ReadSingle();
            surface.Rotation = br.ReadSingle();

            br.ReadInt32(); // something 1
            br.ReadInt32(); // something 2
            br.ReadInt32(); // something 3
            br.ReadInt32(); // something 4

            surface.SurfaceFlags = br.ReadInt32(); // or content flags?
            surface.TextureName = br.ReadFixedLengthString(Encoding.ASCII, 64);
        }

        #endregion

        public void Write(Stream stream, MapFile map, string styleHint)
        {
            throw new NotImplementedException();
        }

        static string ReadString(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len < 0) return null;
            char[] chars = br.ReadChars(len);
            return new string(chars).Trim('\0');
        }

        class JmfGroup
        {
            public int ID { get; set; }
            public int ParentID { get; set; }
            public int Flags { get; set; }
            public int NumObjects { get; set; }
            public Color Color { get; set; }
        }

        class JmfEntity
        {
            public int Flags { get; set; }
            public int GroupID { get; set; }
            public Entity Entity { get; set; }
            public List<JmfSolid> Solids { get; set; }

            public JmfEntity()
            {
                Solids = new List<JmfSolid>();
            }
        }

        class JmfSolid
        {
            public int Flags { get; set; }
            public int GroupID { get; set; }
            public Solid Solid { get; set; }
        }
    }
}
