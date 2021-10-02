using EricsLib.Geometries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricsLib
{
    /// <summary>
    /// The Oct Tree is a 3 Dimensional version of the Quad Tree. It breaks up a 3D space into enclosing boxes.
    /// Partitioning logic: If a node contains two or more objects, partition the node by subdividing it into 8 quadrants. 
    /// Then, try to place all objects into the subdivided regions of space. If the objects cannot be fully contained, leave them in their current node.
    /// This is implemented recursively.
    /// </summary>
    public class OctTree
    {
        BoundingBox m_region;

        List<Physical> m_objects;

        /// <summary>
        /// These are items which we're waiting to insert into the datastructure. 
        /// We want to accrue as many objects in here as possible before we inject them into the tree. This is slightly more cache friendly.
        /// </summary>
        static Queue<Physical> m_pendingInsertion = new Queue<Physical>();

        /// <summary>
        /// These are all of the possible child octants for this node in the tree.
        /// </summary>
        OctTree[] m_childNode = new OctTree[8];

        /// <summary>
        /// This is a bitmask indicating which child nodes are actively being used.
        /// It adds slightly more complexity, but is faster for performance since there is only one comparison instead of 8.
        /// </summary>
        byte m_activeNodes = 0;

        /// <summary>
        /// The minumum size for enclosing region is a 1x1x1 cube.
        /// </summary>
        const int MIN_SIZE = 1;

        /// <summary>
        /// this is how many frames we'll wait before deleting an empty tree branch. Note that this is not a constant. The maximum lifespan doubles
        /// every time a node is reused, until it hits a hard coded constant of 64
        /// </summary>
        int m_maxLifespan = 8;          //
        int m_curLife = -1;             //this is how much time we have left

        /// <summary>
        /// A reference to the parent node is sometimes required. If we are a node and we realize that we no longer have items contained within ourselves,
        /// we need to let our parent know that we're empty so that it can delete us.
        /// </summary>
        OctTree _parent;

        static bool m_treeReady = false;       //the tree has a few objects which need to be inserted before it is complete
        static bool m_treeBuilt = false;       //there is no pre-existing tree yet.


        /*Note: we want to avoid allocating memory for as long as possible since there can be lots of nodes.*/
        /// <summary>
        /// Creates an oct tree which encloses the given region and contains the provided objects.
        /// </summary>
        /// <param name="region">The bounding region for the oct tree.</param>
        /// <param name="objList">The list of objects contained within the bounding region</param>
        private OctTree(BoundingBox region, List<Physical> objList)
        {
            m_region = region;
            m_objects = objList;
            m_curLife = -1;
        }

        public OctTree()
        {
            m_objects = new List<Physical>();
            m_region = new BoundingBox(Vector3.Zero, Vector3.Zero);
            m_curLife = -1;
        }

        /// <summary>
        /// Creates an octTree with a suggestion for the bounding region containing the items.
        /// </summary>
        /// <param name="region">The suggested dimensions for the bounding region. 
        /// Note: if items are outside this region, the region will be automatically resized.</param>
        public OctTree(BoundingBox region)
        {
            m_region = region;
            m_objects = new List<Physical>();
            m_curLife = -1;
        }

        /// <summary>
        /// Renders the current state of the octTree by drawing the outlines of each bounding region.
        /// </summary>
        /// <param name="pb">The primitive batch being used to draw the OctTree.</param>
        public void Render(PrimitiveBatch pb)
        {
            Vector3[] verts = new Vector3[8];
            verts[0] = m_region.Min;
            verts[1] = new Vector3(m_region.Min.X, m_region.Min.Y, m_region.Max.Z); //Z
            verts[2] = new Vector3(m_region.Min.X, m_region.Max.Y, m_region.Min.Z); //Y
            verts[3] = new Vector3(m_region.Max.X, m_region.Min.Y, m_region.Min.Z); //X

            verts[7] = m_region.Max;
            verts[4] = new Vector3(m_region.Max.X, m_region.Max.Y, m_region.Min.Z); //Z
            verts[5] = new Vector3(m_region.Max.X, m_region.Min.Y, m_region.Max.Z); //Y
            verts[6] = new Vector3(m_region.Min.X, m_region.Max.Y, m_region.Max.Z); //X

            pb.AddLine(new VertexPositionColor(verts[0], Color.Lime), new VertexPositionColor(verts[1], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[0], Color.Lime), new VertexPositionColor(verts[2], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[0], Color.Lime), new VertexPositionColor(verts[3], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[7], Color.Lime), new VertexPositionColor(verts[4], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[7], Color.Lime), new VertexPositionColor(verts[5], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[7], Color.Lime), new VertexPositionColor(verts[6], Color.Lime));

            pb.AddLine(new VertexPositionColor(verts[1], Color.Lime), new VertexPositionColor(verts[6], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[1], Color.Lime), new VertexPositionColor(verts[5], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[4], Color.Lime), new VertexPositionColor(verts[2], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[4], Color.Lime), new VertexPositionColor(verts[3], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[2], Color.Lime), new VertexPositionColor(verts[6], Color.Lime));
            pb.AddLine(new VertexPositionColor(verts[3], Color.Lime), new VertexPositionColor(verts[5], Color.Lime));

            for (int a = 0; a < 8; a++)
            {
                if (m_childNode[a] != null)
                    m_childNode[a].Render(pb);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (m_treeBuilt == true)
            {
                //Start a count down death timer for any leaf nodes which don't have objects or children.
                //when the timer reaches zero, we delete the leaf. If the node is reused before death, we double its lifespan.
                //this gives us a "frequency" usage score and lets us avoid allocating and deallocating memory unnecessarily
                if (m_objects.Count == 0)
                {
                    if (HasChildren == false)
                    {
                        if (m_curLife == -1)
                            m_curLife = m_maxLifespan;
                        else if (m_curLife > 0)
                        {
                            m_curLife--;
                        }
                    }
                }
                else
                {
                    if (m_curLife != -1)
                    {
                        if (m_maxLifespan <= 64)
                            m_maxLifespan *= 2;
                        m_curLife = -1;
                    }
                }
                List<Physical> movedObjects = new List<Physical>(m_objects.Count);

                //go through and update every object in the current tree node
                foreach (Physical gameObj in m_objects)
                {
                    //we should figure out if an object actually moved so that we know whether we need to update this node in the tree.
                    if (gameObj.Update(gameTime))
                    {
                        movedObjects.Add(gameObj);
                    }
                }

                //prune any dead objects from the tree.
                int listSize = m_objects.Count;
                for (int a = 0; a < listSize; a++)
                {
                    if (!m_objects[a].Alive)
                    {
                        if (movedObjects.Contains(m_objects[a]))
                            movedObjects.Remove(m_objects[a]);
                        m_objects.RemoveAt(a--);
                        listSize--;
                    }
                }

                //recursively update any child nodes.
                for (int flags = m_activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                    if ((flags & 1) == 1) m_childNode[index].Update(gameTime);


                //If an object moved, we can insert it into the parent and that will insert it into the correct tree node.
                //note that we have to do this last so that we don't accidentally update the same object more than once per frame.
                foreach (Physical movedObj in movedObjects)
                {
                    OctTree current = this;

                    //figure out how far up the tree we need to go to reinsert our moved object
                    //we are either using a bounding rect or a bounding sphere
                    //try to move the object into an enclosing parent node until we've got full containment
                    if (movedObj.BoundingBox.Max != movedObj.BoundingBox.Min)
                    {
                        while (current.m_region.Contains(movedObj.BoundingBox) != ContainmentType.Contains)
                            if (current._parent != null) current = current._parent;
                            else break; //prevent infinite loops when we go out of bounds of the root node region
                    }
                    else
                    {
                        while (current.m_region.Contains(movedObj.BoundingSphere) != ContainmentType.Contains)//we must be using a bounding sphere, so check for its containment.
                            if (current._parent != null) current = current._parent;
                            else break;
                    }

                    //now, remove the object from the current node and insert it into the current containing node.
                    m_objects.Remove(movedObj);
                    current.Insert(movedObj);   //this will try to insert the object as deep into the tree as we can go.
                }

                //prune out any dead branches in the tree
                for (int flags = m_activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                    if ((flags & 1) == 1 && m_childNode[index].m_curLife == 0)
                    {
                        m_childNode[index] = null;
                        m_activeNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                    }

                //now that all objects have moved and they've been placed into their correct nodes in the octree, we can look for collisions.
                if (IsRoot == true)
                {
                    //This will recursively gather up all collisions and create a list of them.
                    //this is simply a matter of comparing all objects in the current root node with all objects in all child nodes.
                    //note: we can assume that every collision will only be between objects which have moved.
                    //note 2: An explosion can be centered on a point but grow in size over time. In this case, you'll have to override the update method for the explosion.
                    List<IntersectionRecord> irList = GetIntersection(new List<Physical>());

                    foreach (IntersectionRecord ir in irList)
                    {
                        if (ir.PhysicalObject != null)
                            ir.PhysicalObject.HandleIntersection(ir);
                        if (ir.OtherPhysicalObject != null)
                            ir.OtherPhysicalObject.HandleIntersection(ir);
                    }
                }

            }
            else
            {

            }
        }

        /// <summary>
        /// Inserts a bunch of items into the oct tree.
        /// </summary>
        /// <param name="ItemList">A list of physical objects to add</param>
        /// <remarks>The OctTree will be rebuilt JIT</remarks>
        public void Add<T>(List<T> ItemList) where T : Physical
        {
            foreach (Physical Item in ItemList)
            {
                if (Item.HasBounds)
                {
                    m_pendingInsertion.Enqueue(Item);
                    m_treeReady = false;
                }
                else
                {
                    throw new Exception("Every object being inserted into the octTree must have a bounding region!");
                }
            }

        }

        public void Add<T>(T Item) where T : Physical
        {
            if (Item.HasBounds) //sanity check
            {
                m_pendingInsertion.Enqueue(Item);

                m_treeReady = false;    //mark the tree as needing an update
            }
            else
            {
                throw new Exception("Every object being inserted into the octTree must have a bounding region!");
            }
        }

        //I don't think I actually use this. The octree removes items on its own when they die.
        public void Remove<T>(T Item) where T : Physical
        {
            //not recursive
            if (m_objects.Contains(Item))
                m_objects.Remove(Item);
        }

        /// <summary>
        /// A tree has already been created, so we're going to try to insert an item into the tree without rebuilding the whole thing
        /// </summary>
        /// <typeparam name="T">A physical object</typeparam>
        /// <param name="Item">The physical object to insert into the tree</param>
        private void Insert<T>(T Item) where T : Physical
        {
            /*make sure we're not inserting an object any deeper into the tree than we have to.
	            -if the current node is an empty leaf node, just insert and leave it.*/
            if (m_objects.Count <= 1 && m_activeNodes == 0)
            {
                m_objects.Add(Item);
                return;
            }

            Vector3 dimensions = m_region.Max - m_region.Min;
            //Check to see if the dimensions of the box are greater than the minimum dimensions
            if (dimensions.X <= MIN_SIZE && dimensions.Y <= MIN_SIZE && dimensions.Z <= MIN_SIZE)
            {
                m_objects.Add(Item);
                return;
            }
            Vector3 half = dimensions / 2.0f;
            Vector3 center = m_region.Min + half;

            //Find or create subdivided regions for each octant in the current region
            BoundingBox[] childOctant = new BoundingBox[8];
            childOctant[0] = (m_childNode[0] != null) ? m_childNode[0].m_region : new BoundingBox(m_region.Min, center);
            childOctant[1] = (m_childNode[1] != null) ? m_childNode[1].m_region : new BoundingBox(new Vector3(center.X, m_region.Min.Y, m_region.Min.Z), new Vector3(m_region.Max.X, center.Y, center.Z));
            childOctant[2] = (m_childNode[2] != null) ? m_childNode[2].m_region : new BoundingBox(new Vector3(center.X, m_region.Min.Y, center.Z), new Vector3(m_region.Max.X, center.Y, m_region.Max.Z));
            childOctant[3] = (m_childNode[3] != null) ? m_childNode[3].m_region : new BoundingBox(new Vector3(m_region.Min.X, m_region.Min.Y, center.Z), new Vector3(center.X, center.Y, m_region.Max.Z));
            childOctant[4] = (m_childNode[4] != null) ? m_childNode[4].m_region : new BoundingBox(new Vector3(m_region.Min.X, center.Y, m_region.Min.Z), new Vector3(center.X, m_region.Max.Y, center.Z));
            childOctant[5] = (m_childNode[5] != null) ? m_childNode[5].m_region : new BoundingBox(new Vector3(center.X, center.Y, m_region.Min.Z), new Vector3(m_region.Max.X, m_region.Max.Y, center.Z));
            childOctant[6] = (m_childNode[6] != null) ? m_childNode[6].m_region : new BoundingBox(center, m_region.Max);
            childOctant[7] = (m_childNode[7] != null) ? m_childNode[7].m_region : new BoundingBox(new Vector3(m_region.Min.X, center.Y, center.Z), new Vector3(center.X, m_region.Max.Y, m_region.Max.Z));

            //First, is the item completely contained within the root bounding box?
            //note2: I shouldn't actually have to compensate for this. If an object is out of our predefined bounds, then we have a problem/error.
            //          Wrong. Our initial bounding box for the terrain is constricting its height to the highest peak. Flying units will be above that.
            //             Fix: I resized the enclosing box to 256x256x256. This should be sufficient.
            if (Item.BoundingBox.Max != Item.BoundingBox.Min && m_region.Contains(Item.BoundingBox) == ContainmentType.Contains)
            {
                bool found = false;
                //we will try to place the object into a child node. If we can't fit it in a child node, then we insert it into the current node object list.
                for (int a = 0; a < 8; a++)
                {
                    //is the object fully contained within a quadrant?
                    if (childOctant[a].Contains(Item.BoundingBox) == ContainmentType.Contains)
                    {
                        if (m_childNode[a] != null)
                            m_childNode[a].Insert(Item);   //Add the item into that tree and let the child tree figure out what to do with it
                        else
                        {
                            m_childNode[a] = CreateNode(childOctant[a], Item);   //create a new tree node with the item
                            m_activeNodes |= (byte)(1 << a);
                        }
                        found = true;
                    }
                }
                if (!found) m_objects.Add(Item);

            }
            else if (Item.BoundingSphere.Radius != 0 && m_region.Contains(Item.BoundingSphere) == ContainmentType.Contains)
            {
                bool found = false;
                //we will try to place the object into a child node. If we can't fit it in a child node, then we insert it into the current node object list.
                for (int a = 0; a < 8; a++)
                {
                    //is the object contained within a child quadrant?
                    if (childOctant[a].Contains(Item.BoundingSphere) == ContainmentType.Contains)
                    {
                        if (m_childNode[a] != null)
                            m_childNode[a].Insert(Item);   //Add the item into that tree and let the child tree figure out what to do with it
                        else
                        {
                            m_childNode[a] = CreateNode(childOctant[a], Item);   //create a new tree node with the item
                            m_activeNodes |= (byte)(1 << a);
                        }
                        found = true;
                    }
                }
                if (!found) m_objects.Add(Item);
            }
            else
            {
                //either the item lies outside of the enclosed bounding box or it is intersecting it. Either way, we need to rebuild
                //the entire tree by enlarging the containing bounding box
                //BoundingBox enclosingArea = FindBox();
                BuildTree();
            }
        }

        /// <summary>
        /// Naively builds an oct tree from scratch.
        /// </summary>
        private void BuildTree()    //complete & tested
        {
            //terminate the recursion if we're a leaf node
            if (m_objects.Count <= 1)   //doubt: is this really right? needs testing.
                return;

            Vector3 dimensions = m_region.Max - m_region.Min;

            if (dimensions == Vector3.Zero)
            {
                FindEnclosingCube();
                dimensions = m_region.Max - m_region.Min;
            }

            //Check to see if the dimensions of the box are greater than the minimum dimensions
            if (dimensions.X <= MIN_SIZE && dimensions.Y <= MIN_SIZE && dimensions.Z <= MIN_SIZE)
            {
                return;
            }

            Vector3 half = dimensions / 2.0f;
            Vector3 center = m_region.Min + half;

            //Create subdivided regions for each octant
            BoundingBox[] octant = new BoundingBox[8];
            octant[0] = new BoundingBox(m_region.Min, center);
            octant[1] = new BoundingBox(new Vector3(center.X, m_region.Min.Y, m_region.Min.Z), new Vector3(m_region.Max.X, center.Y, center.Z));
            octant[2] = new BoundingBox(new Vector3(center.X, m_region.Min.Y, center.Z), new Vector3(m_region.Max.X, center.Y, m_region.Max.Z));
            octant[3] = new BoundingBox(new Vector3(m_region.Min.X, m_region.Min.Y, center.Z), new Vector3(center.X, center.Y, m_region.Max.Z));
            octant[4] = new BoundingBox(new Vector3(m_region.Min.X, center.Y, m_region.Min.Z), new Vector3(center.X, m_region.Max.Y, center.Z));
            octant[5] = new BoundingBox(new Vector3(center.X, center.Y, m_region.Min.Z), new Vector3(m_region.Max.X, m_region.Max.Y, center.Z));
            octant[6] = new BoundingBox(center, m_region.Max);
            octant[7] = new BoundingBox(new Vector3(m_region.Min.X, center.Y, center.Z), new Vector3(center.X, m_region.Max.Y, m_region.Max.Z));

            //This will contain all of our objects which fit within each respective octant.
            List<Physical>[] octList = new List<Physical>[8];
            for (int i = 0; i < 8; i++) octList[i] = new List<Physical>();

            //this list contains all of the objects which got moved down the tree and can be delisted from this node.
            List<Physical> delist = new List<Physical>();

            foreach (Physical obj in m_objects)
            {
                if (obj.BoundingBox.Min != obj.BoundingBox.Max)
                {
                    for (int a = 0; a < 8; a++)
                    {
                        if (octant[a].Contains(obj.BoundingBox) == ContainmentType.Contains)
                        {
                            octList[a].Add(obj);
                            delist.Add(obj);
                            break;
                        }
                    }
                }
                else if (obj.BoundingSphere.Radius != 0)
                {
                    for (int a = 0; a < 8; a++)
                    {
                        if (octant[a].Contains(obj.BoundingSphere) == ContainmentType.Contains)
                        {
                            octList[a].Add(obj);
                            delist.Add(obj);
                            break;
                        }
                    }
                }
            }

            //delist every moved object from this node.
            foreach (Physical obj in delist)
                m_objects.Remove(obj);

            //Create child nodes where there are items contained in the bounding region
            for (int a = 0; a < 8; a++)
            {
                if (octList[a].Count != 0)
                {
                    m_childNode[a] = CreateNode(octant[a], octList[a]);
                    m_activeNodes |= (byte)(1 << a);
                    m_childNode[a].BuildTree();
                }
            }

            m_treeBuilt = true;
            m_treeReady = true;
        }

        private OctTree CreateNode(BoundingBox region, List<Physical> objList)  //complete & tested
        {
            if (objList.Count == 0)
                return null;

            OctTree ret = new OctTree(region, objList);
            ret._parent = this;

            return ret;
        }

        private OctTree CreateNode(BoundingBox region, Physical Item)
        {
            List<Physical> objList = new List<Physical>(1); //sacrifice potential CPU time for a smaller memory footprint
            objList.Add(Item);
            OctTree ret = new OctTree(region, objList);
            ret._parent = this;
            return ret;
        }

        /// <summary>
        /// Processes all pending insertions by inserting them into the tree.
        /// </summary>
        /// <remarks>Consider deprecating this?</remarks>
        private void UpdateTree()   //complete & tested
        {
            /*I think I can just directly insert items into the tree instead of using a queue.*/
            if (!m_treeBuilt)
            {
                while (m_pendingInsertion.Count != 0)
                    m_objects.Add(m_pendingInsertion.Dequeue());

                //trim out any objects which have the exact same bounding areas

                BuildTree();
            }
            else
            {
                while (m_pendingInsertion.Count != 0)
                    Insert(m_pendingInsertion.Dequeue());
            }

            m_treeReady = true;
        }

        /// <summary>
        /// This finds the dimensions of the bounding box necessary to tightly enclose all items in the object list.
        /// </summary>
        private void FindEnclosingBox()
        {
            Vector3 global_min = m_region.Min, global_max = m_region.Max;



            //go through all the objects in the list and find the extremes for their bounding areas.
            foreach (Physical obj in m_objects)
            {
                Vector3 local_min = Vector3.Zero, local_max = Vector3.Zero;

                if (!obj.HasBounds)
                {
                    //the object doesn't have any bounding regions associated with it, so we're going to skip it.
                    //otherwise, we'll get stack overflow exceptions since we'd be creating an infinite number of nodes approaching zero.
                    //continue;
                    throw new Exception("Every object in the octTree must have a bounding region!");
                }

                if (obj.BoundingBox != null && obj.BoundingBox.Max != obj.BoundingBox.Min)
                {
                    local_min = obj.BoundingBox.Min;
                    local_max = obj.BoundingBox.Max;
                }

                if (obj.BoundingSphere != null && obj.BoundingSphere.Radius != 0.0f)
                {

                    local_min = new Vector3(obj.BoundingSphere.Center.X - obj.BoundingSphere.Radius,
                        obj.BoundingSphere.Center.Y - obj.BoundingSphere.Radius,
                        obj.BoundingSphere.Center.Z - obj.BoundingSphere.Radius);
                    local_max = new Vector3(obj.BoundingSphere.Center.X + obj.BoundingSphere.Radius,
                        obj.BoundingSphere.Center.Y + obj.BoundingSphere.Radius,
                        obj.BoundingSphere.Center.Z + obj.BoundingSphere.Radius);
                }

                if (local_min.X < global_min.X) global_min.X = local_min.X;
                if (local_min.Y < global_min.Y) global_min.Y = local_min.Y;
                if (local_min.Z < global_min.Z) global_min.Z = local_min.Z;

                if (local_max.X > global_max.X) global_max.X = local_max.X;
                if (local_max.Y > global_max.Y) global_max.Y = local_max.Y;
                if (local_max.Z > global_max.Z) global_max.Z = local_max.Z;
            }

            m_region.Min = global_min;
            m_region.Max = global_max;
        }

        /// <summary>
        /// This finds the smallest enclosing cube which is a power of 2, for all objects in the list.
        /// </summary>
        private void FindEnclosingCube()
        {
            FindEnclosingBox();

            //find the min offset from (0,0,0) and translate by it for a short while
            Vector3 offset = m_region.Min - Vector3.Zero;
            m_region.Min += offset;
            m_region.Max += offset;

            //find the nearest power of two for the max values
            int highX = (int)Math.Floor(Math.Max(Math.Max(m_region.Max.X, m_region.Max.Y), m_region.Max.Z));

            //see if we're already at a power of 2
            for (int bit = 0; bit < 32; bit++)
            {
                if (highX == 1 << bit)
                {
                    m_region.Max = new Vector3(highX, highX, highX);

                    m_region.Min -= offset;
                    m_region.Max -= offset;
                    return;
                }
            }

            //gets the most significant bit value, so that we essentially do a Ceiling(X) with the 
            //ceiling result being to the nearest power of 2 rather than the nearest integer.
            int x = Calc.SigBit(highX);

            m_region.Max = new Vector3(x, x, x);

            m_region.Min -= offset;
            m_region.Max -= offset;
        }

        /// <summary>
        /// Gives you a list of all intersection records which intersect or are contained within the given frustum area
        /// </summary>
        /// <param name="frustum">The containing frustum to check for intersection/containment with</param>
        /// <returns>A list of intersection records with collisions</returns>
        private List<IntersectionRecord> GetIntersection(BoundingFrustum frustum, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            if (m_objects.Count == 0 && HasChildren == false)   //terminator for any recursion
                return null;

            List<IntersectionRecord> ret = new List<IntersectionRecord>();

            //test each object in the list for intersection
            foreach (Physical obj in m_objects)
            {

                //skip any objects which don't meet our type criteria
                if ((int)((int)type & (int)obj.Type) == 0)
                    continue;

                //test for intersection
                IntersectionRecord ir = obj.Intersects(frustum);
                if (ir != null) ret.Add(ir);
            }

            //test each object in the list for intersection
            for (int a = 0; a < 8; a++)
            {
                if (m_childNode[a] != null && (frustum.Contains(m_childNode[a].m_region) == ContainmentType.Intersects || frustum.Contains(m_childNode[a].m_region) == ContainmentType.Contains))
                {
                    List<IntersectionRecord> hitList = m_childNode[a].GetIntersection(frustum);
                    if (hitList != null)
                    {
                        foreach (IntersectionRecord ir in hitList)
                            ret.Add(ir);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Gives you a list of intersection records for all objects which intersect with the given ray
        /// </summary>
        /// <param name="intersectRay">The ray to intersect objects against</param>
        /// <returns>A list of all intersections</returns>
        private List<IntersectionRecord> GetIntersection(Ray intersectRay, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            if (m_objects.Count == 0 && HasChildren == false)   //terminator for any recursion
                return null;

            List<IntersectionRecord> ret = new List<IntersectionRecord>();

            //the ray is intersecting this region, so we have to check for intersection with all of our contained objects and child regions.

            //test each object in the list for intersection
            foreach (Physical obj in m_objects)
            {
                //skip any objects which don't meet our type criteria
                if ((int)((int)type & (int)obj.Type) == 0)
                    continue;

                if (obj.BoundingBox.Intersects(intersectRay) != null)
                {
                    IntersectionRecord ir = obj.Intersects(intersectRay);
                    if (ir.HasHit)
                        ret.Add(ir);
                }
            }

            // test each child octant for intersection
            for (int a = 0; a < 8; a++)
            {
                if (m_childNode[a] != null && m_childNode[a].m_region.Intersects(intersectRay) != null)
                {
                    List<IntersectionRecord> hits = m_childNode[a].GetIntersection(intersectRay, type);
                    if (hits != null)
                    {
                        foreach (IntersectionRecord ir in hits)
                            ret.Add(ir);
                    }
                }
            }

            return ret;
        }

        private List<IntersectionRecord> GetIntersection(List<Physical> parentObjs, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            List<IntersectionRecord> intersections = new List<IntersectionRecord>();
            //assume all parent objects have already been processed for collisions against each other.
            //check all parent objects against all objects in our local node
            foreach (Physical pObj in parentObjs)
            {
                foreach (Physical lObj in m_objects)
                {
                    //We let the two objects check for collision against each other. They can figure out how to do the coarse and granular checks.
                    //all we're concerned about is whether or not a collision actually happened.
                    IntersectionRecord ir = pObj.Intersects(lObj);
                    if (ir != null)
                    {
                        if (intersections.Contains(ir))
                        {
                            int a = 0;
                            a++;
                        }
                        intersections.Add(ir);
                    }
                }
            }

            //now, check all our local objects against all other local objects in the node
            if (m_objects.Count > 1)
            {
                #region self-congratulation
                /*
                 * This is a rather brilliant section of code. Normally, you'd just have two foreach loops, like so:
                 * foreach(Physical lObj1 in m_objects)
                 * {
                 *      foreach(Physical lObj2 in m_objects)
                 *      {
                 *           //intersection check code
                 *      }
                 * }
                 * 
                 * The problem is that this runs in O(N*N) time and that we're checking for collisions with objects which have already been checked.
                 * Imagine you have a set of four items: {1,2,3,4}
                 * You'd first check: {1} vs {1,2,3,4}
                 * Next, you'd check {2} vs {1,2,3,4}
                 * but we already checked {1} vs {2}, so it's a waste to check {2} vs. {1}. What if we could skip this check by removing {1}?
                 * We'd have a total of 4+3+2+1 collision checks, which equates to O(N(N+1)/2) time. If N is 10, we are already doing half as many collision checks as necessary.
                 * Now, we can't just remove an item at the end of the 2nd for loop since that would break the iterator in the first foreach loop, so we'd have to use a
                 * regular for(int i=0;i<size;i++) style loop for the first loop and reduce size each iteration. This works...but look at the for loop: we're allocating memory for
                 * two additional variables: i and size. What if we could figure out some way to eliminate those variables?
                 * So, who says that we have to start from the front of a list? We can start from the back end and still get the same end results. With this in mind,
                 * we can completely get rid of a for loop and use a while loop which has a conditional on the capacity of a temporary list being greater than 0.
                 * since we can poll the list capacity for free, we can use the capacity as an indexer into the list items. Now we don't have to increment an indexer either!
                 * The result is below.
                 */
                #endregion

                List<Physical> tmp = new List<Physical>(m_objects.Count);
                tmp.AddRange(m_objects);
                while (tmp.Count > 0)
                {
                    foreach (Physical lObj2 in tmp)
                    {
                        if (tmp[tmp.Count - 1] == lObj2 || (tmp[tmp.Count - 1].IsStatic && lObj2.IsStatic))
                            continue;
                        IntersectionRecord ir = tmp[tmp.Count - 1].Intersects(lObj2);
                        if (ir != null)
                            intersections.Add(ir);
                    }

                    //remove this object from the temp list so that we can run in O(N(N+1)/2) time instead of O(N*N)
                    tmp.RemoveAt(tmp.Count - 1);
                }
            }

            //now, merge our local objects list with the parent objects list, then pass it down to all children.
            foreach (Physical lObj in m_objects)
                if (lObj.IsStatic == false)
                    parentObjs.Add(lObj);
            //parentObjs.AddRange(m_objects);

            //each child node will give us a list of intersection records, which we then merge with our own intersection records.
            for (int flags = m_activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                if ((flags & 1) == 1) intersections.AddRange(m_childNode[index].GetIntersection(parentObjs, type));

            return intersections;
        }

        #region Colliders

        /// <summary>
        /// This gives you a list of every intersection record created with the intersection ray
        /// </summary>
        /// <param name="intersectionRay">The ray to use for intersection</param>
        /// <returns></returns>
        public List<IntersectionRecord> AllIntersections(Ray intersectionRay)
        {
            if (!m_treeReady)
                UpdateTree();

            return GetIntersection(intersectionRay);
        }

        /// <summary>
        /// This gives you the first object encountered by the intersection ray
        /// </summary>
        /// <param name="intersectionRay">The ray being used to intersect with</param>
        /// <param name="type">The type of the physical object to filter for</param>
        /// <returns></returns>
        public IntersectionRecord NearestIntersection(Ray intersectionRay, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            if (!m_treeReady)
                UpdateTree();

            List<IntersectionRecord> intersections = GetIntersection(intersectionRay, type);

            IntersectionRecord nearest = new IntersectionRecord();

            foreach (IntersectionRecord ir in intersections)
            {
                if (nearest.HasHit == false)
                {
                    nearest = ir;
                    continue;
                }

                if (ir.Distance < nearest.Distance)
                {
                    nearest = ir;
                }
            }

            return nearest;
        }

        /// <summary>
        /// This gives you a list of all intersections, filtered by a specific type of object
        /// </summary>
        /// <param name="intersectionRay">The ray to intersect with all objects</param>
        /// <param name="type">The type of physical object we're interested in intersecting with</param>
        /// <returns>A list of intersections of the specified type of geometry</returns>
        public List<IntersectionRecord> AllIntersections(Ray intersectionRay, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            if (!m_treeReady)
                UpdateTree();

            return null;
        }

        /// <summary>
        /// This gives you a list of all objects which [intersect or are contained within] the given frustum and meet the given object type
        /// </summary>
        /// <param name="region">The frustum to intersect with</param>
        /// <param name="type">The type of objects you want to filter</param>
        /// <returns>A list of intersection records for all objects intersecting with the frustum</returns>
        public List<IntersectionRecord> AllIntersections(BoundingFrustum region, Physical.PhysicalType type = Physical.PhysicalType.ALL)
        {
            if (!m_treeReady)
                UpdateTree();

            return GetIntersection(region, type);
        }

        #endregion

        #region Accessors
        private bool IsRoot
        {
            //The root node is the only node without a parent.
            get { return _parent == null; }
        }

        private bool HasChildren
        {
            get
            {
                //if (m_childNode[0] != null ||
                //    m_childNode[1] != null ||
                //    m_childNode[2] != null ||
                //    m_childNode[3] != null ||
                //    m_childNode[4] != null ||
                //    m_childNode[5] != null ||
                //    m_childNode[6] != null ||
                //    m_childNode[7] != null)
                //    return true;
                //return false;
                return m_activeNodes != 0;
            }
        }

        /// <summary>
        /// Returns true if this node tree and all children have no content
        /// </summary>
        private bool IsEmpty    //untested
        {
            get
            {
                if (m_objects.Count != 0)
                    return false;
                else
                {
                    for (int a = 0; a < 8; a++)
                    {
                        //note that we have to do this recursively. 
                        //Just checking child nodes for the current node doesn't mean that their children won't have objects.
                        if (m_childNode[a] != null && !m_childNode[a].IsEmpty)
                            return false;
                    }

                    return true;
                }
            }
        }
        #endregion
    }
}
