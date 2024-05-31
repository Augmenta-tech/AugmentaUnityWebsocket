using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Augmenta
{
    using AugmentaPContainer = PContainer<Vector3>;
    public class AugmentaContainer : MonoBehaviour
    {
        AugmentaPContainer nativeObject;

        public virtual void setup(AugmentaPContainer nativeContainer, AugmentaClient client)
        {
            this.nativeObject = nativeContainer;
            this.nativeObject.wrapperObject = this;

            gameObject.name = nativeObject.name;

            foreach (var c in nativeObject.children)
            {
                AugmentaPContainer childC = c as AugmentaPContainer;
                GameObject oPrefab;

                switch (c.containerType)
                {

                    case ContainerType.Zone:
                        oPrefab = client.zonePrefab;
                        break;

                    case ContainerType.Scene:
                        oPrefab = client.scenePrefab;
                        break;

                    case ContainerType.Container:
                    default:
                        oPrefab = null;
                        break;

                }

                AugmentaContainer child = oPrefab != null ? Instantiate(oPrefab).GetComponent<AugmentaContainer>() : new GameObject().AddComponent<AugmentaContainer>();
                child.transform.parent = transform;
                child.setup(childC, client);

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