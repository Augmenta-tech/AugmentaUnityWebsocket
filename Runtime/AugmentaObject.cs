using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using log4net.Util;

namespace Augmenta
{
    public class AugmentaPObject : GenericPObject<Vector3> //this is Vector3 from UnityEngine, not System.Numerics (this is why we need to create a new class)
    {
        AugmentaObject aObject;

        public AugmentaPObject() { }
        public AugmentaPObject(AugmentaObject ao = null)
        {
            aObject = ao;
            aObject.setNativeObject(this);
        }

        override protected void updateTransform()
        {
            switch (posUpdateMode)
            {
                case PositionUpdateMode.None:
                    break;
                case PositionUpdateMode.Centroid:
                    aObject.transform.localPosition = centroid;
                    break;
                case PositionUpdateMode.BoxCenter:
                    aObject.transform.localPosition = (minBounds + maxBounds) / 2;
                    break;
            }


            Quaternion rot = Quaternion.AngleAxis(rotation.x, Vector3.right) *
   Quaternion.AngleAxis(rotation.z, Vector3.forward) *
   Quaternion.AngleAxis(rotation.y, Vector3.up);

            aObject.transform.localRotation = rot;
        }

        protected override void updateCloudPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute) pointInArray = point;
            else pointInArray = aObject.transform.parent.TransformPoint(point);
        }

        protected override void updateClusterPoint(ref Vector3 pointInArray, Vector3 point)
        {
            if (pointMode == CoordMode.Absolute) pointInArray = aObject.transform.parent.InverseTransformPoint(point);
            else pointInArray = point;
            //Debug.Log("update cluster point : " +pointInArray);
        }

        public override void kill(bool immediate = false)
        {
            aObject.kill(immediate);
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

        public void setNativeObject(AugmentaPObject nativeObject)
        {
            this.nativeObject = nativeObject;
        }

        // Update is called once per frame
        void Update()
        {
            if (nativeObject == null) return;
            nativeObject.update(Time.time);
        }

        public void updateData(byte[] data, int offset)
        {
            if (nativeObject == null) return;
            nativeObject.updateData(Time.time, data, offset);
        }

        public void kill(bool immediate)
        {
            onRemove?.Invoke(this);
            if (immediate || killDelayTime == 0)
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
            if (nativeObject == null) return;
            if (drawDebug)
            {
                Color c = Color.HSVToRGB((objectID * .1f) % 1, 1, 1); //Color.red;// getColor();
                if (state == AugmentaPObject.State.Ghost) c = Color.gray / 2;

                Gizmos.color = c;
                foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);

                Gizmos.color = c + Color.white * .3f;

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.parent.position, transform.rotation, transform.parent.lossyScale);
                Gizmos.matrix = rotationMatrix;

                Gizmos.DrawWireSphere(centroid, .03f);

                Vector3 boxCenter = (minBounds + maxBounds) / 2;
                Gizmos.DrawWireCube(boxCenter, maxBounds - minBounds);
            }
        }
    }
}