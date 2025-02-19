using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Events;

namespace AugmentaWebsocketClient
{
    public class AugmentaZone : AugmentaContainer
    {
        Augmenta.Zone<Vector3> nativeZone;

        public int presence { get { return nativeZone.presence; } }
        public float density { get { return nativeZone.density; } }
        public float sliderValue { get { return nativeZone.sliderValue; } }
        public Vector2 padXY { get { return new Vector2(nativeZone.padX, nativeZone.padY); } }
        public Vector3[] points { get { return nativeZone.points.ToArray(); } }

        public UnityEvent<int> objectsEnteredEvent;
        public UnityEvent<int> objectsExitedEvent;

        internal override void Setup(Augmenta.Container<Vector3> c, AugmentaClient client)
        {
            base.Setup(c, client);
            this.nativeZone = c as Augmenta.Zone<Vector3>;
            this.nativeZone.wrapperObject = this;
            this.nativeZone.enterEvent += OnObjectsEntered;
            this.nativeZone.exitEvent += OnObjectsExited;
        }

        private void OnObjectsEntered(int count)
        {
            Debug.Log("Objects entered: " + count);
            objectsEnteredEvent.Invoke(count);
        }

        private void OnObjectsExited(int count)
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
                    case Augmenta.Shape<Vector3>.ShapeType.Box:
                        Augmenta.BoxShape<Vector3> box = nativeZone.shape as Augmenta.BoxShape<Vector3>;
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

                    case Augmenta.Shape<Vector3>.ShapeType.Sphere:
                        Augmenta.SphereShape<Vector3> sphere = nativeZone.shape as Augmenta.SphereShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
                        break;
                    case Augmenta.Shape<Vector3>.ShapeType.Cylinder:
                        Augmenta.CylinderShape<Vector3> cylinder = nativeZone.shape as Augmenta.CylinderShape<Vector3>;
                        Vector3 halfSize = new Vector3(cylinder.radius, cylinder.height / 2, cylinder.radius);
                        Gizmos.DrawWireCube(halfSize, halfSize * 2);
                        break;
                    case Augmenta.Shape<Vector3>.ShapeType.Cone:
                        Augmenta.ConeShape<Vector3> cone = nativeZone.shape as Augmenta.ConeShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, cone.radius);
                        break;
                    case Augmenta.Shape<Vector3>.ShapeType.Mesh:
                        break;
                }
            }
        }
    }
}