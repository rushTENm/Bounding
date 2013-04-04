using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Bounding
{
    /// 

    /// Represents an octree spatial partioning system.
    /// 

    public class Octree
    {
        /// 

        /// The number of children in an octree.
        /// 

        private const int ChildCount = 8;

        /// 

        /// The octree's looseness value.
        /// 

        private float looseness = 0;

        /// 

        /// The octree's depth.
        /// 

        private int depth = 0;

        /// 

        /// The octree's center coordinates.
        /// 

        private Vector3 center = Vector3.Zero;

        /// 

        /// The octree's length.
        /// 

        private float length = 0f;

        /// 

        /// The bounding box that represents the octree.
        /// 

        private BoundingBox bounds = default(BoundingBox);

        /// 

        /// The objects in the octree.
        /// 

        private List objects = new List();

        /// 

        /// The octree's child nodes.
        /// 

        private Octree[] children = null;

        /// 

        /// The octree's world size.
        /// 

        private float worldSize = 0f;

        /// 

        /// Creates a new octree.
        /// 

        /// The octree's world size.
        /// The octree's looseness value.
        /// The octree recursion depth.
        public Octree(float worldSize, float looseness, int depth)
            : this(worldSize, looseness, depth, 0, Vector3.Zero)
        {
        }

        public Octree(float worldSize, float looseness, int depth, Vector3 center)
            : this(worldSize, looseness, depth, 0, center)
        {
        }

        /// 

        /// Creates a new octree.
        /// 

        /// The octree's world size.
        /// The octree's looseness value.
        /// The maximum depth to recurse to.
        /// The octree recursion depth.
        /// The octree's center coordinates.
        private Octree(float worldSize, float looseness,
            int maxDepth, int depth, Vector3 center)
        {
            this.worldSize = worldSize;
            this.looseness = looseness;
            this.depth = depth;
            this.center = center;
            this.length = this.looseness * this.worldSize / (float)Math.Pow(2, this.depth);
            float radius = this.length / 2f;

            // Create the bounding box.
            Vector3 min = this.center + new Vector3(-radius);
            Vector3 max = this.center + new Vector3(radius);
            this.bounds = new BoundingBox(min, max);

            // Split the octree if the depth hasn't been reached.
            if (this.depth < maxDepth)
            {
                this.Split(maxDepth);
            }
        }

        /// 

        /// Removes the specified obj.
        /// 

        /// The obj.
        public void Remove(T obj)
        {
            objects.Remove(obj);
        }

        /// 

        /// Determines whether the specified obj has changed.
        /// 

        /// The obj.
        /// The transformebbox.
        /// 
        ///   true if the specified obj has changed; otherwise, false.
        /// 
        public bool HasChanged(T obj, BoundingBox transformebbox)
        {
            return this.bounds.Contains(transformebbox) == ContainmentType.Contains;
        }

        /// 

        /// Stills inside ?
        /// 

        /// The o.
        /// The center.
        /// The radius.
        /// 
        public bool StillInside(T o, Vector3 center, float radius)
        {
            Vector3 min = center - new Vector3(radius);
            Vector3 max = center + new Vector3(radius);
            BoundingBox bounds = new BoundingBox(min, max);

            if (this.children != null)
                return false;

            if (this.bounds.Contains(bounds) == ContainmentType.Contains)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// 

        /// Stills inside ?.
        /// 

        /// The obj.
        /// Its bounds.
        /// 
        public bool StillInside(T o, BoundingBox bounds)
        {
            if (this.children != null)
                return false;

            if (this.bounds.Contains(bounds) == ContainmentType.Contains)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// 

        /// Adds the given object to the octree.
        /// 

        /// The object to add.
        /// The object's center coordinates.
        /// The object's radius.
        public Octree Add(T o, Vector3 center, float radius)
        {
            Vector3 min = center - new Vector3(radius);
            Vector3 max = center + new Vector3(radius);
            BoundingBox bounds = new BoundingBox(min, max);

            if (this.bounds.Contains(bounds) == ContainmentType.Contains)
            {
                return this.Add(o, bounds, center, radius);
            }
            return null;
        }


        /// 

        /// Adds the given object to the octree.
        /// 

        public Octree Add(T o, BoundingBox transformebbox)
        {
            float radius = (transformebbox.Max - transformebbox.Min).Length() / 2;
            Vector3 center = (transformebbox.Max + transformebbox.Min) / 2;

            if (this.bounds.Contains(transformebbox) == ContainmentType.Contains)
            {
                return this.Add(o, transformebbox, center, radius);
            }
            return null;
        }


        /// 

        /// Adds the given object to the octree.
        /// 

        /// The object to add.
        /// The object's bounds.
        /// The object's center coordinates.
        /// The object's radius.
        private Octree Add(T o, BoundingBox bounds, Vector3 center, float radius)
        {
            if (this.children != null)
            {
                // Find which child the object is closest to based on where the
                // object's center is located in relation to the octree's center.
                int index = (center.X <= this.center.X ? 0 : 1) +
                    (center.Y >= this.center.Y ? 0 : 4) +
                    (center.Z <= this.center.Z ? 0 : 2);

                // Add the object to the child if it is fully contained within
                // it.
                if (this.children[index].bounds.Contains(bounds) == ContainmentType.Contains)
                {
                    return this.children[index].Add(o, bounds, center, radius);

                }
            }
            this.objects.Add(o);
            return this;
        }

        /// 

        /// Draws the octree.
        /// 

        /// The viewing matrix.
        /// The projection matrix.
        /// The objects in the octree.
        /// The number of octrees drawn.
        public int Draw(Matrix view, Matrix projection, List objects)
        {
            BoundingFrustum frustum = new BoundingFrustum(view * projection);
            ContainmentType containment = frustum.Contains(this.bounds);

            return this.Draw(frustum, view, projection, containment, objects);
        }

        /// 

        /// Draws the octree.
        /// 

        /// The viewing frustum used to determine if the octree is in view.
        /// The viewing matrix.
        /// The projection matrix.
        /// Determines how much of the octree is visible.
        /// The objects in the octree.
        /// The number of octrees drawn.
        private int Draw(BoundingFrustum frustum, Matrix view, Matrix projection,
            ContainmentType containment, List objects)
        {
            int count = 0;

            if (containment != ContainmentType.Contains)
            {
                containment = frustum.Contains(this.bounds);
            }

            // Draw the octree only if it is atleast partially in view.
            if (containment != ContainmentType.Disjoint)
            {
                // Draw the octree's bounds if there are objects in the octree.
                if (this.objects.Count > 0)
                {
                    if (DebugDraw != null)
                        DebugDraw.AddShape(new DebugBox(this.bounds, Color.White));
                    objects.AddRange(this.objects);
                    count++;
                }

                // Draw the octree's children.
                if (this.children != null)
                {
                    foreach (Octree child in this.children)
                    {
                        count += child.Draw(frustum, view, projection, containment, objects);
                    }
                }
            }

            return count;
        }

        /// 

        /// Splits the octree into eight children.
        /// 

        /// The maximum depth to recurse to.
        private void Split(int maxDepth)
        {
            this.children = new Octree[Octree.ChildCount];
            int depth = this.depth + 1;
            float quarter = this.length / this.looseness / 4f;

            this.children[0] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(-quarter, quarter, -quarter));
            this.children[1] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(quarter, quarter, -quarter));
            this.children[2] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(-quarter, quarter, quarter));
            this.children[3] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(quarter, quarter, quarter));
            this.children[4] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(-quarter, -quarter, -quarter));
            this.children[5] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(quarter, -quarter, -quarter));
            this.children[6] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(-quarter, -quarter, quarter));
            this.children[7] = new Octree(this.worldSize, this.looseness,
                maxDepth, depth, this.center + new Vector3(quarter, -quarter, quarter));
        }

    }
}
