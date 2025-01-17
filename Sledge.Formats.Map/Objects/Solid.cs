﻿using System.Collections.Generic;
using System.Linq;
using Sledge.Formats.Precision;

namespace Sledge.Formats.Map.Objects
{
    public class Solid : MapObject
    {
        public List<Face> Faces { get; set; }
        public List<Mesh> Meshes { get; set; }

        public Solid()
        {
            Faces = new List<Face>();
            Meshes = new List<Mesh>();
        }

        public void ComputeVertices()
        {
            if (Faces.Count < 4) return;

            Polyhedron poly = new Polyhedron(Faces.Select(x => new Plane(x.Plane.Normal.ToPrecisionVector3(), x.Plane.D)));

            foreach (Face face in Faces)
            {
                Polygon pg = poly.Polygons.FirstOrDefault(x => x.Plane.Normal.EquivalentTo(face.Plane.Normal.ToPrecisionVector3(), 0.0075f)); // Magic number that seems to match VHE
                if (pg != null)
                {
                    face.Vertices.AddRange(pg.Vertices.Select(x => x.ToStandardVector3()));
                }
            }
        }
    }
}