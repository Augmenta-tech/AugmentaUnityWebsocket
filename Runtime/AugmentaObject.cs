using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using log4net.Util;

namespace Augmenta
{
    public class AugmentaPObject : GenericPObject<Vector3>
    {
        AugmentaObject aObject;

        public AugmentaPObject(AugmentaObject aObject)
        {
            this.aObject = aObject;
        }

        override protected void updatePosition()
        {
            switch (posUpdateMode)
            {
                case PositionUpdateMode.None:
                    break;
                case PositionUpdateMode.Centroid:
                    aObject.transform.position = centroid;
                    break;
                case PositionUpdateMode.BoxCenter:
                    aObject.transform.position = (minBounds + maxBounds) / 2;
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
            if (pointMode == CoordMode.Absolute) pointInArray = point;
            else pointInArray = aObject.transform.parent.TransformPoint(point);
        }

        protected override void updateClusterPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute) pointInArray = point;
            else pointInArray = aObject.transform.parent.TransformPoint(point);
        }

        public override void kill()
        {
            aObject.kill();
        }
    }

    public class AugmentaObject : MonoBehaviour 
    {

        AugmentaPObject nativeObject;

        public int objectID { get { return nativeObject.objectID; } }
        public Vector3[] points { get { return nativeObject.points.ToArray(); } }
        public AugmentaPObject.State state { get { return nativeObject.state; } }
        public Vector3 centroid { get { return nativeObject.centroid; } }
        public Vector3 velocity { get { return nativeObject.velocity; } }
        public Vector3 minBounds { get { return nativeObject.minBounds; } }
        public Vector3 maxBounds { get { return nativeObject.maxBounds; } }
        public float weight { get { return nativeObject.weight; } }

        [Header("Behaviour")]
        public float killDelayTime = 0;

        [Header("Debug")]
        public bool drawDebug;


        public delegate void OnRemoveEvent(AugmentaObject obj);
        public event OnRemoveEvent onRemove;

        void Start()
        {
            nativeObject = new AugmentaPObject(this);
        }

        // Update is called once per frame
        void Update()
        {
            nativeObject.update(Time.time);
        }

        public void updateData(byte[] data, int offset)
        {
            nativeObject.updateData(Time.time, data, offset);
        }

        public void kill()
        {
            onRemove?.Invoke(this);
            if (killDelayTime == 0)
            {
                Destroy(gameObject);
                return;
            }


            StartCoroutine(killForReal(killDelayTime));
        }

        IEnumerator killForReal(float timeBeforeKill)
        {
            yield return new WaitForSeconds(timeBeforeKill);
            Destroy(gameObject);
        }

        void OnDrawGizmos()
        {
            if (drawDebug)
            {
                Color c = Color.HSVToRGB((objectID * .1f) % 1, 1, 1); //Color.red;// getColor();
                if (state == AugmentaPObject.State.Ghost) c = Color.gray / 2;

                Gizmos.color = c;
                foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);

                Gizmos.color = c + Color.white * .3f;
                Gizmos.DrawWireSphere(centroid, .03f);
                Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);
            }
        }
    }
}