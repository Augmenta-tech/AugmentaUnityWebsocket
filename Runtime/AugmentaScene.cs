using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Augmenta
{
    using AugmentaPContainer = PContainer<Vector3>;
    using AugmentaPScene = PScene<Vector3>;
    public class AugmentaScene : AugmentaContainer
    {

        AugmentaPScene nativeScene;
        public GameObject objectsContainer;

        internal override void setup(AugmentaPContainer c, AugmentaClient client)
        {
            base.setup(c, client);
            this.nativeScene = c as AugmentaPScene;

            objectsContainer = new GameObject("Objects");
            objectsContainer.transform.parent = transform;

        }

        public override void Update()
        {
            base.Update();
        }

        private void OnDrawGizmos()
        {
            if(nativeScene == null) return;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(nativeScene.size / 2, nativeScene.size);
        }
    }
}