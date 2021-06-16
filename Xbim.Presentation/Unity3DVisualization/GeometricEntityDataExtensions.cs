using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.Common.XbimExtensions;

using DamageModelingTools.UnityCommunication.Messaging;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Unity3DVisualization
{
    /// <summary>
    /// Extensions for the geometric entities
    /// </summary>
    internal static class GeometricEntityDataExtensions
    {
        internal static void Read(this GeometricEntityData m3D, byte[] mesh, XbimMatrix3D? transform = null)
        {
            var qrd = new RotateTransform3D();
            Matrix3D? matrix3D = null;
            if (transform.HasValue)
            {
                var xq = transform.Value.GetRotationQuaternion();
                var quaternion = new Quaternion(xq.X, xq.Y, xq.Z, xq.W);
                if (!quaternion.IsIdentity)
                    qrd.Rotation = new QuaternionRotation3D(quaternion);
                else
                    qrd = null;
                matrix3D = transform.Value.ToMatrix3D();
            }

            int indexBase = m3D.Vertices.Count();
            using (MemoryStream ms = new MemoryStream(mesh))
            {
                using (var br = new BinaryReader(ms))
                {
                    byte version = br.ReadByte(); //stream format version
                    int numVertices = br.ReadInt32();
                    int numTriangles = br.ReadInt32();

                    List<MessagingVector3D> uniqueVertices = new List<MessagingVector3D>(numVertices);
                    List<MessagingVector3D> vertices = new List<MessagingVector3D>(numVertices * 4); //approx the size
                    List<int> triangleIndices = new List<Int32>(numTriangles * 3);
                    List<MessagingVector3D> normals = new List<MessagingVector3D>(numVertices * 4);

                    for (int i = 0; i < numVertices; i++)
                    {
                        double x = br.ReadSingle();
                        double y = br.ReadSingle();
                        double z = br.ReadSingle();
                        MessagingVector3D p = new MessagingVector3D(x, y, z);
                        if (matrix3D.HasValue)
                        {
                            p = matrix3D.Value.Transform(p.AsPoint3D()).AsMessagingVector3D();
                        }
                        uniqueVertices.Add(p);
                    }

                    var numFaces = br.ReadInt32();

                    for (var i = 0; i < numFaces; i++)
                    {
                        var numTrianglesInFace = br.ReadInt32();
                        if (numTrianglesInFace == 0) continue;
                        var isPlanar = numTrianglesInFace > 0;
                        numTrianglesInFace = Math.Abs(numTrianglesInFace);
                        if (isPlanar)
                        {
                            XbimVector3D normal = br.ReadPackedNormal().Normal;
                            MessagingVector3D wpfNormal = new MessagingVector3D(normal.X, normal.Y, normal.Z);
                            var uniqueIndices = new Dictionary<int, int>();
                            for (var j = 0; j < numTrianglesInFace; j++)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    int idx = ReadIndex(br, numVertices);
                                    int writtenIdx;
                                    if (!uniqueIndices.TryGetValue(idx, out writtenIdx)) //we haven't got it, so add it
                                    {
                                        writtenIdx = vertices.Count;
                                        vertices.Add(uniqueVertices[idx]);
                                        uniqueIndices.Add(idx, writtenIdx);
                                        //add a matching normal
                                        if (qrd != null)
                                        {
                                            wpfNormal = qrd.Transform(wpfNormal.AsPoint3D()).AsMessagingVector3D();
                                        }
                                        normals.Add(wpfNormal);
                                    }
                                    triangleIndices.Add(indexBase + writtenIdx);
                                }
                            }
                        }
                        else
                        {
                            var uniqueIndices = new Dictionary<int, int>();
                            for (var j = 0; j < numTrianglesInFace; j++)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    int idx = ReadIndex(br, numVertices);
                                    var normal = br.ReadPackedNormal().Normal;
                                    int writtenIdx;
                                    MessagingVector3D wpfNormal = new MessagingVector3D(normal.X, normal.Y, normal.Z);
                                    if (!uniqueIndices.TryGetValue(idx, out writtenIdx)) //we haven't got it, so add it
                                    {
                                        writtenIdx = vertices.Count;
                                        vertices.Add(uniqueVertices[idx]);
                                        uniqueIndices.Add(idx, writtenIdx);
                                        normals.Add(wpfNormal);
                                    }
                                    else
                                    {
                                        if (normals[writtenIdx] != wpfNormal) //deal with normals that vary at a node
                                        {
                                            writtenIdx = vertices.Count;
                                            vertices.Add(uniqueVertices[idx]);
                                            normals.Add(wpfNormal);
                                        }
                                    }

                                    triangleIndices.Add(indexBase + writtenIdx);
                                }
                            }
                        }
                    }
                    m3D.Vertices.AddRange(vertices);
                    m3D.Indices.AddRange(triangleIndices);
                    m3D.Normals.AddRange(normals);
                }
            }
        }

        /// <summary>
        /// Reads a packed Xbim Triangle index from a stream
        /// </summary>
        /// <param name="br"></param>
        /// <param name="maxVertexCount">The size of the maximum number of vertices in the stream, i.e. the biggest index value</param>
        /// <returns></returns>
        private static int ReadIndex(BinaryReader br, int maxVertexCount)
        {
            if (maxVertexCount <= 0xFF)
                return br.ReadByte();
            if (maxVertexCount <= 0xFFFF)
                return br.ReadUInt16();
            return (int)br.ReadUInt32(); //this should never go over int32
        }

        /// <summary>
        /// retrieve a point 3D equal to the messaging vector
        /// </summary>
        /// <param name="messagingVector3D"></param>
        /// <returns></returns>
        public static Point3D AsPoint3D (this MessagingVector3D messagingVector3D)
        {
            return new Point3D(messagingVector3D.X, messagingVector3D.Y, messagingVector3D.Z);
        }

        /// <summary>
        /// retrieve a messaging vector equal to the given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static MessagingVector3D AsMessagingVector3D (this Point3D point)
        {
            return new MessagingVector3D(point.X, point.Y, point.Z);
        }
    }
}
