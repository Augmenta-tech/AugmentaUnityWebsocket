using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Augmenta
{
    using AugmentaPContainer = PContainer<Vector3>;
    using AugmentaPZone = PZone<Vector3>;
    public class AugmentaZone : AugmentaContainer
    {

        AugmentaPZone nativeZone;

        internal override void setup(AugmentaPContainer c, AugmentaClient client)
        {
            base.setup(c, client);
            this.nativeZone = c as AugmentaPZone;
        }

        public override void Update()
        {
            base.Update();
        }

        private void OnDrawGizmos()
        {
            if (nativeZone == null) return;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = Color.cyan;

            if(nativeZone.shape != null)
            {
                switch (nativeZone.shape.shapeType)
                {
                    case Shape<Vector3>.ShapeType.Box:
                        BoxShape<Vector3> box = nativeZone.shape as BoxShape<Vector3>;
                        Gizmos.DrawWireCube(box.size/2, box.size);
                        break;
                    case Shape<Vector3>.ShapeType.Sphere:
                        SphereShape<Vector3> sphere = nativeZone.shape as SphereShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
                        break;
                    case Shape<Vector3>.ShapeType.Cylinder:
                        CylinderShape<Vector3> cylinder = nativeZone.shape as CylinderShape<Vector3>;
                        Vector3 halfSize = new Vector3(cylinder.radius, cylinder.height / 2, cylinder.radius);
                        Gizmos.DrawWireCube(halfSize, halfSize*2);
                        break;
                    case Shape<Vector3>.ShapeType.Cone:
                        ConeShape<Vector3> cone = nativeZone.shape as ConeShape<Vector3>;
                        Gizmos.DrawWireSphere(Vector3.zero, cone.radius);
                        break;
                    case Shape<Vector3>.ShapeType.Mesh:
                        break;
                }
            }
            //Gizmos.DrawWireCube(Vector3.zero, nativeZone.size);
        }
    }
}