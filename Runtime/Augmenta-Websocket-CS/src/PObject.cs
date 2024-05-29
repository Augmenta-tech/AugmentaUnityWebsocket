using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Augmenta
{
    public abstract class BasePObject
    {
        public int objectID;
        public float lastUpdateTime;

        public float killDelayTime = 0;
        public float timeSinceGhost;
        public bool drawDebug;

        public delegate void OnRemoveEvent(BasePObject obj);
        public event OnRemoveEvent onRemove;

        public enum State { Enter = 0, Update = 1, Leave = 2, Ghost = 3 };
        public State state;
        public void update(float time)
        {
            if (time - lastUpdateTime > .5f)
                timeSinceGhost = time;
            else
                timeSinceGhost = -1;
        }

        virtual public void updateData(float time, ReadOnlySpan<byte> data, int offset)
        {
            lastUpdateTime = time;
        }

        virtual protected void updateClusterData(ReadOnlySpan<byte> data, int offset)
        {
            {
                state = (State)Utils.ReadInt(data, offset);
                if (state == State.Leave) //Will leave
                {
                    onRemove?.Invoke(this);
                    return;
                }
            }
        }

        virtual public void kill() { }
        virtual public void clear()
        {
            state = State.Ghost;
        }
    }

    public abstract class GenericPObject<T> : BasePObject
    {
        private T[] pointsA = new T[0];
        private int pointCount;
        public ArraySegment<T> points => new ArraySegment<T>(pointsA, 0, pointCount);

        //cluster


        public T centroid;
        public T velocity;
        public T minBounds;
        public T maxBounds;
        //[Range(0, 1)]
        public float weight;

        public enum PositionUpdateMode { None, Centroid, BoxCenter }
        public PositionUpdateMode posUpdateMode = PositionUpdateMode.Centroid;

        public enum CoordMode { Absolute, Relative }
        public CoordMode pointMode = CoordMode.Relative;




        // Update is called once per frame

        public override void updateData(float time, ReadOnlySpan<byte> data, int offset)
        {
            var pos = offset + 1 + 2 * sizeof(int); //packet type (1) + packet size (4) + objectID (4)
            while (pos < data.Length)
            {
                var propertyID = Utils.ReadInt(data, pos);
                var propertySize = Utils.ReadInt(data, pos + sizeof(int));

                if (propertySize < 0)
                {
                    //Debug.LogWarning("Error : property size < 0");
                    break;
                }

                switch (propertyID)
                {
                    case 0: updatePointsData(data, pos + 2 * sizeof(int)); break;
                    case 1: updateClusterData(data, pos + 2 * sizeof(int)); break;
                }

                pos += propertySize;
            }

            base.updateData(time, data, offset);
        }

        void updatePointsData(ReadOnlySpan<byte> data, int offset)
        {
            pointCount = Utils.ReadInt(data, offset);
            var vectors = ReadVectors(data, offset + sizeof(int), pointCount * 12);

            if (pointsA.Length < pointCount)
                pointsA = new T[(int)(pointCount * 1.5)];

            if (pointMode == CoordMode.Absolute)
                vectors.CopyTo(pointsA.AsSpan());
            else
            {
                for (int i = 0; i < vectors.Length; i++)
                    updateCloudPoint(ref pointsA[i], vectors[i]);

            }
        }

        override protected void updateClusterData(ReadOnlySpan<byte> data, int offset)
        {
            base.updateClusterData(data, offset);

            var clusterData = new T[4];
            for (int i = 0; i < 4; i++)
            {
                var si = offset + sizeof(int) + i * 12;

                var p = ReadVector(data, si);
                if (i == 1) clusterData[i] = p;
                else updateClusterPoint(ref clusterData[i], p);

            }

            centroid = clusterData[0];
            velocity = clusterData[1];
            minBounds = clusterData[2];
            maxBounds = clusterData[3];
            weight = Utils.ReadFloat(data, offset + 4 + 4 * 12);


            updatePosition();
        }

        public override void clear()
        {
            base.clear();
            pointCount = 0;
            pointsA = new T[0];

            centroid = default;
            velocity = default;
        }

        abstract protected void updateCloudPoint(ref T pointInArray, T point);
        abstract protected void updateClusterPoint(ref T pointInArray, T point);
        abstract protected void updatePosition();

        abstract protected T ReadVector(ReadOnlySpan<byte> data, int offset);
        abstract protected ReadOnlySpan<T> ReadVectors(ReadOnlySpan<byte> data, int offset, int length);
    }

    public class PObject : GenericPObject<Vector3>
    {
        public Matrix4x4 transform;
        public Matrix4x4 parentTransform;

        override protected void updatePosition()
        {
            switch (posUpdateMode)
            {
                case PositionUpdateMode.None:
                    break;
                case PositionUpdateMode.Centroid:
                    transform.Translation = centroid;
                    break;
                case PositionUpdateMode.BoxCenter:
                    transform.Translation = (minBounds + maxBounds) / 2;
                    break;
            }
        }

        override protected Vector3 ReadVector(ReadOnlySpan<byte> data, int offset)
        {
            return MemoryMarshal.Cast<byte, Vector3>(data.Slice(offset))[0];
        }
        override protected ReadOnlySpan<Vector3> ReadVectors(ReadOnlySpan<byte> data, int offset, int length)
        {
            return MemoryMarshal.Cast<byte, Vector3>(data.Slice(offset, length));
        }

        protected override void updateCloudPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute)
                pointInArray = point;
            else
                pointInArray = Vector3.Transform(point, parentTransform);
        }

        protected override void updateClusterPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute)
                pointInArray = point;
            else
                pointInArray = Vector3.Transform(point, parentTransform);
        }

    }
}