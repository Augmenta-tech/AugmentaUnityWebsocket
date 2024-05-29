using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using log4net.Util;

namespace Augmenta
{
    public class AugmentaPZone : GenericPZone<Vector3>
    {
        AugmentaZone aZone;

        public AugmentaPZone(AugmentaZone aZone)
        {
            this.aZone = aZone;
        }

        override protected void updatePosition()
        {
            switch (posUpdateMode)
            {
                case PositionUpdateMode.None:
                    break;
                case PositionUpdateMode.Centroid:
                    aZone.transform.position = centroid;
                    break;
                case PositionUpdateMode.BoxCenter:
                    aZone.transform.position = (minBounds + maxBounds) / 2;
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
            else pointInArray = aZone.transform.parent.TransformPoint(point);
        }

        protected override void updateClusterPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute) pointInArray = point;
            else pointInArray = aZone.transform.parent.TransformPoint(point);
        }

        public override void kill()
        {
            aZone.kill();
        }
    }

    public class AugmentaZone : MonoBehaviour
    {

        AugmentaPZone nativeZone;

        public int objectID { get { return nativeZone.objectID; } }
        public Vector3[] points { get { return nativeZone.points.ToArray(); } }
        public AugmentaPZone.State state { get { return nativeZone.state; } }
        public Vector3 centroid { get { return nativeZone.centroid; } }
        public Vector3 velocity { get { return nativeZone.velocity; } }
        public Vector3 minBounds { get { return nativeZone.minBounds; } }
        public Vector3 maxBounds { get { return nativeZone.maxBounds; } }
        public float weight { get { return nativeZone.weight; } }

        [Header("Behaviour")]
        public float killDelayTime = 0;

        [Header("Debug")]
        public bool drawDebug;


        public delegate void OnRemoveEvent(AugmentaZone obj);
        public event OnRemoveEvent onRemove;

        void Start()
        {
            nativeZone = new AugmentaPZone(this);
        }

        // Update is called once per frame
        void Update()
        {
            nativeZone.update(Time.time);
        }

        public void updateData(byte[] data, int offset)
        {
            nativeZone.updateData(Time.time, data, offset);
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
                if (state == AugmentaPZone.State.Ghost) c = Color.gray / 2;

                Gizmos.color = c;
                foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);

                Gizmos.color = c + Color.white * .3f;
                Gizmos.DrawWireSphere(centroid, .03f);
                Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);
            }
        }
    }
}