using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Sledge.Formats.Map.Objects;
using Path = Sledge.Formats.Map.Objects.Path;

namespace Sledge.Formats.Map.Formats
{
    public class WorldcraftRmfFormat : IMapFormat
    {
        public string Name => "Worldcraft RMF";
        public string Description => "The .rmf file format used by Worldcraft and Valve Hammer Editor 3.";
        public string ApplicationName => "Worldcraft";
        public string Extension => "rmf";
        public string[] AdditionalExtensions => new[] { "rmx" };
        public string[] SupportedStyleHints => new[] { "2.2" };

        const int MaxVariableStringLength = 127;

        public MapFile Read(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                // Only RMF version 2.2 is supported for the moment.
                double version = Math.Round(br.ReadSingle(), 1);
                Util.Assert(Math.Abs(version - 2.2) < 0.01, $"Unsupported RMF version number. Expected 2.2, got {version}.");

                // RMF header test
                string header = br.ReadFixedLengthString(Encoding.ASCII, 3);
                Util.Assert(header == "RMF", $"Incorrect RMF header. Expected 'RMF', got '{header}'.");

                MapFile map = new MapFile();

                ReadVisgroups(map, br);
                ReadWorldspawn(map, br);

                // Some RMF files might not have the DOCINFO block so we check if we're at the end of the stream
                if (stream.Position < stream.Length)
                {
                    // DOCINFO string check
                    string docinfo = br.ReadFixedLengthString(Encoding.ASCII, 8);
                    Util.Assert(docinfo == "DOCINFO", $"Incorrect RMF format. Expected 'DOCINFO', got '{docinfo}'.");

                    ReadCameras(map, br);
                }

                return map;
            }
        }

        #region Read

        static void ReadVisgroups(MapFile map, BinaryReader br)
        {
            int numVisgroups = br.ReadInt32();
            for (int i = 0; i < numVisgroups; i++)
            {
                Visgroup vis = new Visgroup
                {
                    Name = br.ReadFixedLengthString(Encoding.ASCII, 128),
                    Color = br.ReadRGBAColour(),
                    ID = br.ReadInt32(),
                    Visible = br.ReadBoolean()
                };
                br.ReadBytes(3);
                map.Visgroups.Add(vis);
            }
        }

        static void ReadWorldspawn(MapFile map, BinaryReader br)
        {
            Worldspawn e = (Worldspawn) ReadObject(map, br);

            map.Worldspawn.SpawnFlags = e.SpawnFlags;
            foreach (KeyValuePair<string, string> p in e.Properties) map.Worldspawn.Properties[p.Key] = p.Value;
            map.Worldspawn.Children.AddRange(e.Children);
        }

        static MapObject ReadObject(MapFile map, BinaryReader br)
        {
            string type = br.ReadCString();
            switch (type)
            {
                case "CMapWorld":
                    return ReadRoot(map, br);
                case "CMapGroup":
                    return ReadGroup(map, br);
                case "CMapSolid":
                    return ReadSolid(map, br);
                case "CMapEntity":
                    return ReadEntity(map, br);
                default:
                    throw new ArgumentOutOfRangeException("Unknown RMF map object: " + type);
            }
        }

        static void ReadMapBase(MapFile map, MapObject obj, BinaryReader br)
        {
            int visgroupId = br.ReadInt32();
            if (visgroupId > 0)
            {
                obj.Visgroups.Add(visgroupId);
            }

            obj.Color = br.ReadRGBColour();

            int numChildren = br.ReadInt32();
            for (int i = 0; i < numChildren; i++)
            {
                MapObject child = ReadObject(map, br);
                if (child != null) obj.Children.Add(child);
            }
        }

        static Worldspawn ReadRoot(MapFile map, BinaryReader br)
        {
            Worldspawn wld = new Worldspawn();
            ReadMapBase(map, wld, br);
            ReadEntityData(wld, br);
            int numPaths = br.ReadInt32();
            for (int i = 0; i < numPaths; i++)
            {
                map.Paths.Add(ReadPath(br));
            }
            return wld;
        }

        static Path ReadPath(BinaryReader br)
        {
            Path path = new Path
            {
                Name = br.ReadFixedLengthString(Encoding.ASCII, 128),
                Type = br.ReadFixedLengthString(Encoding.ASCII, 128),
                Direction = (PathDirection) br.ReadInt32()
            };
            int numNodes = br.ReadInt32();
            for (int i = 0; i < numNodes; i++)
            {
                PathNode node = new PathNode
                {
                    Position = br.ReadVector3(),
                    ID = br.ReadInt32(),
                    Name = br.ReadFixedLengthString(Encoding.ASCII, 128)
                };

                int numProps = br.ReadInt32();
                for (int j = 0; j < numProps; j++)
                {
                    string key = br.ReadCString();
                    string value = br.ReadCString();
                    node.Properties[key] = value;
                }
                path.Nodes.Add(node);
            }
            return path;
        }

        static Group ReadGroup(MapFile map, BinaryReader br)
        {
            Group grp = new Group();
            ReadMapBase(map, grp, br);
            return grp;
        }

        static Solid ReadSolid(MapFile map, BinaryReader br)
        {
            Solid sol = new Solid();
            ReadMapBase(map, sol, br);
            int numFaces = br.ReadInt32();
            for (int i = 0; i < numFaces; i++)
            {
                Face face = ReadFace(br);
                sol.Faces.Add(face);
            }
            return sol;
        }

        static Entity ReadEntity(MapFile map, BinaryReader br)
        {
            Entity ent = new Entity();
            ReadMapBase(map, ent, br);
            ReadEntityData(ent, br);
            br.ReadBytes(2); // Unused
            Vector3 origin = br.ReadVector3();
            ent.Properties["origin"] = $"{origin.X.ToString("0.000", CultureInfo.InvariantCulture)} {origin.Y.ToString("0.000", CultureInfo.InvariantCulture)} {origin.Z.ToString("0.000", CultureInfo.InvariantCulture)}";
            br.ReadBytes(4); // Unused
            return ent;
        }

        static void ReadEntityData(Entity e, BinaryReader br)
        {
            e.ClassName = br.ReadCString();
            br.ReadBytes(4); // Unused bytes
            e.SpawnFlags = br.ReadInt32();

            int numProperties = br.ReadInt32();
            for (int i = 0; i < numProperties; i++)
            {
                string key = br.ReadCString();
                string value = br.ReadCString();
                if (key == null) continue;
                e.Properties[key] = value;
            }

            br.ReadBytes(12); // More unused bytes
        }

        static Face ReadFace(BinaryReader br)
        {
            Face face = new Face();
            string textureName = br.ReadFixedLengthString(Encoding.ASCII, 256);
            br.ReadBytes(4); // Unused
            face.TextureName = textureName;
            face.UAxis = br.ReadVector3();
            face.XShift = br.ReadSingle();
            face.VAxis = br.ReadVector3();
            face.YShift = br.ReadSingle();
            face.Rotation = br.ReadSingle();
            face.XScale = br.ReadSingle();
            face.YScale = br.ReadSingle();
            br.ReadBytes(16); // Unused
            int numVerts = br.ReadInt32();
            for (int i = 0; i < numVerts; i++)
            {
                face.Vertices.Add(br.ReadVector3());
            }
            face.Plane = br.ReadPlane();
            return face;
        }

        static void ReadCameras(MapFile map, BinaryReader br)
        {
            br.ReadSingle(); // Appears to be a version number for camera data. Unused.
            int activeCamera = br.ReadInt32();

            int num = br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                map.Cameras.Add(new Camera
                {
                    EyePosition = br.ReadVector3(),
                    LookPosition = br.ReadVector3(),
                    IsActive = activeCamera == i
                });
            }
        }

        #endregion

        public void Write(Stream stream, MapFile map, string styleHint)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                // RMF 2.2 header
                bw.Write(2.2f);
                bw.WriteFixedLengthString(Encoding.ASCII, 3, "RMF");

                // Body
                WriteVisgroups(map, bw);
                WriteWorldspawn(map, bw);

                // Only write docinfo if there's cameras in the document
                if (map.Cameras.Any())
                {
                    // Docinfo footer
                    bw.WriteFixedLengthString(Encoding.ASCII, 8, "DOCINFO");
                    WriteCameras(map, bw);
                }
            }
        }

        #region Write

        static void WriteVisgroups(MapFile map, BinaryWriter bw)
        {
            List<Visgroup> vis = map.Visgroups;
            bw.Write(vis.Count);
            foreach (Visgroup visgroup in vis)
            {
                bw.WriteFixedLengthString(Encoding.ASCII, 128, visgroup.Name);
                bw.WriteRGBAColour(visgroup.Color);
                bw.Write(visgroup.ID);
                bw.Write(visgroup.Visible);
                bw.Write(new byte[3]); // Unused
            }
        }

        static void WriteWorldspawn(MapFile map, BinaryWriter bw)
        {
            WriteObject(map.Worldspawn, bw);
            List<Path> paths = map.Paths;
            bw.Write(paths.Count);
            foreach (Path path in paths)
            {
                WritePath(bw, path);
            }
        }

        static void WriteObject(MapObject o, BinaryWriter bw)
        {
            switch (o)
            {
                case Worldspawn r:
                    WriteRoot(r, bw);
                    break;
                case Group g:
                    WriteGroup(g, bw);
                    break;
                case Solid s:
                    WriteSolid(s, bw);
                    break;
                case Entity e:
                    WriteEntity(e, bw);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unsupported RMF map object: " + o.GetType());
            }
        }

        static void WriteMapBase(MapObject obj, BinaryWriter bw)
        {
            bw.Write(obj.Visgroups.Any() ? obj.Visgroups[0] : 0);
            bw.WriteRGBColour(obj.Color);
            bw.Write(obj.Children.Count);
            foreach (MapObject child in obj.Children)
            {
                WriteObject(child, bw);
            }
        }

        static void WriteRoot(Worldspawn root, BinaryWriter bw)
        {
            bw.WriteCString("CMapWorld", MaxVariableStringLength);
            WriteMapBase(root, bw);
            WriteEntityData(root, bw);
        }

        static void WritePath(BinaryWriter bw, Path path)
        {
            bw.WriteFixedLengthString(Encoding.ASCII, 128, path.Name);
            bw.WriteFixedLengthString(Encoding.ASCII, 128, path.Type);
            bw.Write((int)path.Direction);
            bw.Write(path.Nodes.Count);
            foreach (PathNode node in path.Nodes)
            {
                bw.WriteVector3(node.Position);
                bw.Write(node.ID);
                bw.WriteFixedLengthString(Encoding.ASCII, 128, node.Name);
                bw.Write(node.Properties.Count);
                foreach (KeyValuePair<string, string> property in node.Properties)
                {
                    bw.WriteCString(property.Key, MaxVariableStringLength);
                    bw.WriteCString(property.Value, MaxVariableStringLength);
                }
            }
        }

        static void WriteGroup(Group group, BinaryWriter bw)
        {
            bw.WriteCString("CMapGroup", MaxVariableStringLength);
            WriteMapBase(group, bw);
        }

        static void WriteSolid(Solid solid, BinaryWriter bw)
        {
            bw.WriteCString("CMapSolid", MaxVariableStringLength);
            WriteMapBase(solid, bw);
            List<Face> faces = solid.Faces.ToList();
            bw.Write(faces.Count);
            foreach (Face face in faces)
            {
                WriteFace(face, bw);
            }
        }

        static void WriteEntity(Entity entity, BinaryWriter bw)
        {
            bw.WriteCString("CMapEntity", MaxVariableStringLength);
            WriteMapBase(entity, bw);
            WriteEntityData(entity, bw);
            bw.Write(new byte[2]); // Unused

            Vector3 origin = new Vector3();
            if (entity.Properties.ContainsKey("origin"))
            {
                string o = entity.Properties["origin"];
                if (!string.IsNullOrWhiteSpace(o))
                {
                    string[] parts = o.Split(' ');
                    if (parts.Length == 3)
                    {
                        NumericsExtensions.TryParse(parts[0], parts[1], parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out origin);
                    }
                }
            }
            bw.WriteVector3(origin);
            bw.Write(new byte[4]); // Unused
        }

        static void WriteEntityData(Entity data, BinaryWriter bw)
        {
            bw.WriteCString(data.ClassName, MaxVariableStringLength);
            bw.Write(new byte[4]); // Unused
            bw.Write(data.SpawnFlags);

            List<KeyValuePair<string, string>> props = data.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToList();
            bw.Write(props.Count);
            foreach (KeyValuePair<string, string> p in props)
            {
                bw.WriteCString(p.Key, MaxVariableStringLength);
                bw.WriteCString(p.Value, MaxVariableStringLength);
            }
            bw.Write(new byte[12]); // Unused
        }

        static void WriteFace(Face face, BinaryWriter bw)
        {
            bw.WriteFixedLengthString(Encoding.ASCII, 256, face.TextureName);
            bw.Write(new byte[4]);
            bw.WriteVector3(face.UAxis);
            bw.Write(face.XShift);
            bw.WriteVector3(face.VAxis);
            bw.Write(face.YShift);
            bw.Write(face.Rotation);
            bw.Write(face.XScale);
            bw.Write(face.YScale);
            bw.Write(new byte[16]);
            bw.Write(face.Vertices.Count);
            foreach (Vector3 vertex in face.Vertices)
            {
                bw.WriteVector3(vertex);
            }
            bw.WritePlane(face.Vertices.ToArray());
        }

        static void WriteCameras(MapFile map, BinaryWriter bw)
        {
            bw.Write(0.2f); // Unused

            List<Camera> cams = map.Cameras;
            int active = Math.Max(0, cams.FindIndex(x => x.IsActive));

            bw.Write(active);
            bw.Write(cams.Count);
            foreach (Camera cam in cams)
            {
                bw.WriteVector3(cam.EyePosition);
                bw.WriteVector3(cam.LookPosition);
            }
        }

        #endregion
    }
}
