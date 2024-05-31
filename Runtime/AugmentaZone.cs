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

        public override void setup(AugmentaPContainer c, AugmentaClient client)
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
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = Color.red;
            //Gizmos.DrawWireCube(Vector3.zero, nativeZone.size);
        }
    }
}