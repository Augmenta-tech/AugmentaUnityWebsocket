using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Augmenta
{

    [RequireComponent(typeof(AugmentaObject))]
    public class PCLToGameObjects : MonoBehaviour
    {
        public int objectsCount;
        public GameObject objectPrefab;

        AugmentaObject pObject;

        private GameObject[] instantiatedObjects;

        void Start()
        {
            //Get PObject
            pObject = GetComponent<AugmentaObject>();

            //Create game objects
            instantiatedObjects = new GameObject[objectsCount];

            for (int i = 0; i < objectsCount; i++)
            {
                instantiatedObjects[i] = Instantiate(objectPrefab, transform);
            }

            PlaceObjectsOnPointCloud();
        }

        void Update()
        {
            PlaceObjectsOnPointCloud();
        }

        void PlaceObjectsOnPointCloud()
        {
            int activeCount = pObject.points.Length;

            if (activeCount <= instantiatedObjects.Length)
            {
                //If less points than objects, place an object on each point and disable the rest
                for (int i = 0; i < instantiatedObjects.Length; i++)
                {
                    if (i < activeCount)
                    {
                        instantiatedObjects[i].SetActive(true);
                        instantiatedObjects[i].transform.position = pObject.points[i];
                    }
                    else
                    {
                        instantiatedObjects[i].SetActive(false);
                    }
                }
            }
            else
            {
                //If more points than objects, place each object on a random point
                for (int i = 0; i < instantiatedObjects.Length; i++)
                {
                    instantiatedObjects[i].SetActive(true);
                    instantiatedObjects[i].transform.position = pObject.points[Random.Range(0, activeCount)];
                }
            }

        }
    }
}