using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;

namespace Augmenta
{
    public class AugmentaClient : MonoBehaviour
    {
        public string ipAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                if (websocket != null)
                {
                    websocket.Close();
                    websocket = null;
                }
            }
        }

        public int port
        {
            get => _port;
            set
            {
                _port = value;
                if (websocket != null)
                {
                    websocket.Close();
                    websocket = null;
                }
            }
        }

        WebSocket websocket;
        AugmentaPleiadesClient pClient;

        float lastUpdateTime;
        float lastConnectTime;
        float lastMessageTime;


        [Header("Setup")]
        public GameObject objectPrefab;
        public GameObject zonePrefab;


        [Header("Connection")]
        [SerializeField] string _ipAddress = "127.0.0.1";
        [SerializeField] int _port = 6060;
        public bool connected = false;
        public bool receivingData = false;


        [Header("Events")]
        public UnityEvent<AugmentaObject> OnObjectCreated;
        public UnityEvent<AugmentaObject> OnObjectRemoved;


        bool isProcessing;
        List<MessageEventArgs> wsMessages;


        private void Awake()
        {
            wsMessages = new List<MessageEventArgs>();
            pClient = new AugmentaPleiadesClient(this);
        }

         
        void OnDisable()
        {
            websocket.Close();
            pClient.clear();
            pClient = null;
            wsMessages.Clear();
        }

        private void OnEnable()
        {
            wsMessages = new List<MessageEventArgs>();
            pClient = new AugmentaPleiadesClient(this);
        }

        void Start()
        {
            init();
            connect();
        }

        void init()
        {
            websocket = new WebSocket("ws://" + ipAddress + ":" + port);
            websocket.OnOpen += (sender, e) =>
            {
                connected = true;
                Debug.Log("Connection " + "ws://" + ipAddress + ":" + port + " opened !");
            };

            websocket.OnError += (sender, e) =>
            {
                Debug.Log("Error! " + e.Message);
            };

            websocket.OnClose += (sender, e) =>
            {
                connected = false;
                Debug.Log("Connection " + "ws://" + ipAddress + ":" + port + " closed.");
            };

            websocket.OnMessage += (sender, e) =>
            {
                Debug.Log("Receive message : " + e.IsText);
                connected = true;
                lastMessageTime = lastUpdateTime;

                while (isProcessing) { }

                wsMessages.Add(e);

            };

        }

        void processMessage(MessageEventArgs e)
        {
            try
            {
                if (e.IsText)
                {
                    //pClient.processMessage(Time.time, e.RawData);
                }
                else if (e.IsBinary)
                {
                    pClient.processData(Time.time, e.RawData);

                }
            }
            catch (Exception err)
            {
                Debug.LogError("Error processing message : " + err);
            }
        }


        void connect()
        {
            Debug.Log("Connecting websocket...");
            lastConnectTime = Time.time;
            websocket.Close();
            websocket.Connect();
        }

        // Update is called once per frame
        /*async*/
        void Update()
        {
            if (websocket == null) init();

#if !UNITY_WEBGL || UNITY_EDITOR
            if ((!connected || !websocket.IsAlive) && Time.time - lastConnectTime > 1)
            {
                Debug.Log("Connecting websocket...");
                connect();
            }

#endif
            lastUpdateTime = Time.time;

            isProcessing = true;
            foreach (var e in wsMessages) processMessage(e);
            wsMessages.Clear();
            isProcessing = false;


            pClient.update(Time.time);
            receivingData = (Time.time - lastMessageTime) < 1;
        }


        private void OnApplicationQuit()
        {
            websocket.Close();
        }

    }

    public class AugmentaPleiadesClient : GenericPleiadesClient
    {
        AugmentaClient client;

        public AugmentaPleiadesClient(AugmentaClient client)
        {
            this.client = client;
        }

        protected override BasePObject addObject(int objectID)
        {
            Debug.Log("Add Object : " + objectID);
            return base.addObject(objectID);
        }

        protected override void removeObject(BasePObject o)
        {
            Debug.Log("Remove Object : " + o.objectID);
            base.removeObject(o);
        }


        override protected BasePObject createObject()
        {
            AugmentaObject ao = GameObject.Instantiate(client.objectPrefab).GetComponent<AugmentaObject>();
            ao.transform.parent = client.transform;
            return new AugmentaPObject(ao);
        }

        override protected BasePZone createZone()
        {
            AugmentaZone az = GameObject.Instantiate(client.zonePrefab).AddComponent<AugmentaZone>();
            az.transform.parent = client.transform;
            return new AugmentaPZone(az);
        }
    }
}