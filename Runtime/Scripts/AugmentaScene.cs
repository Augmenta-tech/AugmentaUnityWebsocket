using UnityEngine;

namespace AugmentaWebsocketClient
{
    public class AugmentaScene : AugmentaContainer
    {
        Augmenta.Scene<Vector3> nativeScene;
        public GameObject objectsContainer;

        internal override void Setup(Augmenta.Container<Vector3> c, AugmentaClient client)
        {
            base.Setup(c, client);
            this.nativeScene = c as Augmenta.Scene<Vector3>;

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
            Gizmos.color = Color.white * .5f;
            Gizmos.DrawWireCube(nativeScene.size / 2, nativeScene.size);
        }
    }
}