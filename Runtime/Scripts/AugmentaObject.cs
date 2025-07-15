using System.Collections;
using UnityEngine;

namespace AugmentaWebsocketClient
{
    public class AugmentaUnityObject : Augmenta.GenericObject<Vector3> //this is Vector3 from UnityEngine, not System.Numerics (this is why we need to create a new class)
    {
        AugmentaObject aObject;

        public AugmentaUnityObject() { }
        
        public AugmentaUnityObject(AugmentaObject ao = null)
        {
            centroid = Vector3.zero;
            boxCenter = Vector3.zero;
            boxSize = Vector3.one;
            rotation = Vector3.zero;

            aObject = ao;
            aObject.SetNativeObject(this);
        }

        override protected void UpdateTransform()
        {
            if (!isCluster)
            {
                return;
            }

            switch (posUpdateMode)
            {
                case PositionUpdateMode.None:
                    break;

                case PositionUpdateMode.Centroid:
                    aObject.transform.localPosition = centroid;
                    break;
                case PositionUpdateMode.BoxCenter:
                    aObject.transform.localPosition = boxCenter;
                    break;
            }


            Quaternion rot = Quaternion.AngleAxis(rotation.x, Vector3.right) *
            Quaternion.AngleAxis(rotation.z, Vector3.forward) *
            Quaternion.AngleAxis(rotation.y, Vector3.up);

            aObject.transform.localRotation = rot;
        }

        //We're deciding that any transformation will be done by the client, this avoids doing a point-by-point callback
        //protected override void updateCloudPoint(ref Vector3 pointInArray, Vector3 point)
        //{
        //    if (pointMode == CoordMode.Absolute) pointInArray = point;
        //    else pointInArray = aObject.transform.parent.TransformPoint(point);
        //}

        //protected override void updateClusterPoint(ref Vector3 pointInArray, Vector3 point)
        //{
        //    if (pointMode == CoordMode.Absolute) pointInArray = aObject.transform.parent.InverseTransformPoint(point);
        //    else pointInArray = point;
        //    //Debug.Log("update cluster point : " +pointInArray);
        //}

        public override void Kill(bool immediate = false)
        {
            aObject.Kill(immediate);
        }
    }

    public class AugmentaObject : MonoBehaviour
    {

        AugmentaUnityObject nativeObject;

        public int objectID { get { return nativeObject.objectID; } }
        public Vector3[] points { get { return nativeObject.points.ToArray(); } }
        public AugmentaUnityObject.State state { get { return nativeObject.state; } }
        public Vector3 centroid { get { return nativeObject.centroid; } }
        public Vector3 velocity { get { return nativeObject.velocity; } }
        public Vector3 boxCenter { get { return nativeObject.boxCenter; } }
        public Vector3 boxSize { get { return nativeObject.boxSize; } }
        public float weight { get { return nativeObject.weight; } }

        [Header("Behaviour")]
        public float killDelayTime = 0;

        [Header("Debug")]
        public bool drawDebug;


        public delegate void OnRemoveEvent(AugmentaObject obj);
        public event OnRemoveEvent onRemove;

        public void SetNativeObject(AugmentaUnityObject nativeObject)
        {
            this.nativeObject = nativeObject;
        }

        private void Awake()
        {
            //transform.localPosition = Vector3.zero;
        }

        // Update is called once per frame
        void Update()
        {
            if (nativeObject == null) return;
            nativeObject.Update(Time.time);
        }

        public void UpdateData(byte[] data, int offset)
        {
            if (nativeObject == null) return;
            nativeObject.UpdateData(Time.time, data, offset);
        }

        public void Kill(bool immediate)
        {
            onRemove?.Invoke(this);
            if (immediate || killDelayTime == 0)
            {
                Destroy(gameObject);
                return;
            }


            StartCoroutine(KillForReal(killDelayTime));
        }

        IEnumerator KillForReal(float timeBeforeKill)
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
                if (state == AugmentaUnityObject.State.Ghost) c = Color.gray / 2;

                Matrix4x4 mat = Matrix4x4.TRS(transform.parent.position, Quaternion.identity, Vector3.one);
                Matrix4x4 centroidMat = Matrix4x4.TRS(transform.parent.position + centroid, transform.rotation, transform.parent.lossyScale);
                Matrix4x4 boxCenterMat = Matrix4x4.TRS(transform.parent.position + boxCenter, transform.rotation, transform.parent.lossyScale);

                Gizmos.matrix = mat;

                Gizmos.color = c;
                foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);

                if (nativeObject.isCluster)
                {
                    Gizmos.color = c + Color.white * .3f;

                    Gizmos.matrix = centroidMat;
                    Gizmos.DrawWireSphere(Vector3.zero, .03f);

                    Gizmos.matrix = boxCenterMat;
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                }
            }
        }
    }
}