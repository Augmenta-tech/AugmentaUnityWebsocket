using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AugmentaWebsocketClient
{
    public class AugmentaContainer : MonoBehaviour
    {
        Augmenta.Container<Vector3> nativeObject;

        [Header("Debug")]
        public bool drawDebug;
        internal virtual void Setup(Augmenta.Container<Vector3> nativeContainer, AugmentaClient client)
        {
            this.nativeObject = nativeContainer;
            this.nativeObject.wrapperObject = this;

            gameObject.name = nativeObject.name;

            foreach (var c in nativeObject.children)
            {
                Augmenta.Container<Vector3> childC = c as Augmenta.Container<Vector3>;
                GameObject oPrefab;

                switch (c.containerType)
                {

                    case Augmenta.ContainerType.Zone:
                        oPrefab = client.zonePrefab;
                        break;

                    case Augmenta.ContainerType.Scene:
                        oPrefab = client.scenePrefab;
                        break;

                    case Augmenta.ContainerType.Container:
                    default:
                        oPrefab = null;
                        break;

                }

                AugmentaContainer child = oPrefab != null ? Instantiate(oPrefab).GetComponent<AugmentaContainer>() : new GameObject().AddComponent<AugmentaContainer>();
                child.transform.parent = transform;
                child.Setup(childC, client);

            }

        }

        public virtual void Update()
        {
            if (nativeObject == null) return;
            transform.localPosition = nativeObject.position;
            transform.localRotation = Quaternion.Euler(nativeObject.rotation);
        }
    }
}