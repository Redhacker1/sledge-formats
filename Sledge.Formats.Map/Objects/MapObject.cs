using System;
using System.Collections.Generic;
using System.Drawing;

namespace Sledge.Formats.Map.Objects
{
    public abstract class MapObject
    {
        public List<MapObject> Children { get; set; }
        public List<int> Visgroups { get; set; }
        public Color Color { get; set; }

        protected MapObject()
        {
            Children = new List<MapObject>();
            Visgroups = new List<int>();
            Color = Color.White;
        }

        public IEnumerable<MapObject> FindAll()
        {
            return Find(x => true);
        }

        public IEnumerable<MapObject> Find(Predicate<MapObject> matcher)
        {
            List<MapObject> list = new List<MapObject>();
            FindRecursive(list, matcher);
            return list;
        }

        void FindRecursive(ICollection<MapObject> items, Predicate<MapObject> matcher)
        {
            bool thisMatch = matcher(this);
            if (thisMatch)
            {
                items.Add(this);
            }
            foreach (MapObject mo in Children)
            {
                mo.FindRecursive(items, matcher);
            }
        }
    }
}