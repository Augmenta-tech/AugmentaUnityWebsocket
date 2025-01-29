using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Events;

namespace Augmenta
{
    using static Augmenta.BasePObject;
    using AugmentaPContainer = PContainer<Vector3>;
    using AugmentaPZone = PZone<Vector3>;

    public class AugmentaZone : AugmentaContainer
    {
        AugmentaPZone nativeZone;

        public int presence { get { return nativeZone.presence; } }
        public float density { get { return nativeZone.density; } }
        public float sliderValue { get { return nativeZone.sliderValue; } }
        public Vector2 padXY { get { return new Vector2(nativeZone.padX, nativeZone.padY); } }
        public Vector3[] points { get { return nativeZone.points.ToArray(); } }

        public UnityEvent<int> objectsEnteredEvent;
        public UnityEvent<int> objectsExitedEvent;

        internal override void setup(AugmentaPContainer c, AugmentaClient client)
        {
            base.setup(c, client);
            this.nativeZone = c as AugmentaPZone;
            this.nativeZone.wrapperObject = this;
            this.nativeZone.enterEvent += onObjectsEntered;
            this.nativeZone.exitEvent += onObjectsExited;
        }

        private void onObjectsEntered(int count)
        {
            Debug.Log("Objects entered: " + count);
            objectsEnteredEvent.Invoke(count);
        }

        private void onObjectsExited(int count)
        {
            Debug.Log("Objects exited: " + count);
            objectsExitedEvent.Invoke(count);
        }

        private void OnDrawGizmos()
        {
            if (nativeZone == null) return;

            //before matrix transofmation

            Color col = new Color(nativeZone.color.R / 255f, nativeZone.color.G / 255f, nativeZone.color.B / 255f, nativeZone.color.A / 255f);
            Color brighter = new Color(.1f, .1f, .1f);

            Gizmos.color = col;

            //transform for local space
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            if (points.Length > 0)
            {
                foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);
            }

            if (nativeZone.shape != null)
            {
                switch (nativeZone.shape.shapeType)
                {
                    case Shape<Vector3>.ShapeType.Box:
                        BoxShape<Vector3> box = nativeZone.shape as BoxShape<Vector3>;
                        Gizmos.DrawWireCube(box.size / 2, box.size);

                        Gizmos.color = col + brighter;

                        if (drawDebug)
                        {
                            //using pad xy, relative 0-1 to draw x and z lines
                            Vector3 tp = new Vector3(padXY.x * box.size.x, 0, padXY.y * box.size.z);
                            Gizmos.DrawLine(new Vector3(tp.x, 0, 0), new Vector3(tp.x, 0, box.size.z));
                            Gizmos.DrawLine(new Vector3(0, 0, tp.z), new Vector3(box.size.x, 0, tp.z));

                            Gizmos.color = col * new Color(1, 1, 1, .2f);
                            Vector3 tSlider = box.size;

                            if (nativeZone != null)
                            {
                                switch (nativeZone.sliderAxis)
                                {
                                    case 0:
                                        tSlider.x = sliderValue * box.size.x;
                                        break;

                                    case 1:
                                        tSlider.y = sliderValue * box.size.y;

                                        break;

                                    case 2:
                                        tSlider.z = sliderValue * box.size.z;
                                        break;
                                }
                            }

                            //draw box in axis
                            Gizmos.DrawCube(tSlider / 2, tSlider);
                        }
                        break;

                    case Shape<Vector3>.ShapeType.Sphere:
                        SphereShape<Vector3> sphere = nativeZone.shape as SphereShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
                        break;
                    case Shape<Vector3>.ShapeType.Cylinder:
                        CylinderShape<Vector3> cylinder = nativeZone.shape as CylinderShape<Vector3>;
                        Vector3 halfSize = new Vector3(cylinder.radius, cylinder.height / 2, cylinder.radius);
                        Gizmos.DrawWireCube(halfSize, halfSize * 2);
                        break;
                    case Shape<Vector3>.ShapeType.Cone:
                        ConeShape<Vector3> cone = nativeZone.shape as ConeShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, cone.radius);
                        break;
                    case Shape<Vector3>.ShapeType.Mesh:
                        break;
                }
            }
        }
    }
}