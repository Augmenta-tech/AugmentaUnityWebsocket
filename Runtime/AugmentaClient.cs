using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;
using OSCQuery;

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


        [Header("Spawn")]
        public GameObject objectPrefab;
        public GameObject zonePrefab;

        [Header("Connection")]
        public string clientName = "Unity";
        public string clientID;
        [SerializeField] string _ipAddress = "127.0.0.1";
        [SerializeField] int _port = 6060;
        public bool connected = false;
        public bool receivingData = false;

        [Header("Streaming Options")]
        public int downSample = 1;
        public bool streamClouds = true;
        public bool streamClusters = true;
        public List<string> tags;

        int _lastDownSample = 1;
        bool _lastStreamClouds = true;
        bool _lastStreamClusters = true;
        List<string> _lastTags;


        [Header("Events")]
        public UnityEvent<AugmentaObject> OnObjectCreated;
        public UnityEvent<AugmentaObject> OnObjectRemoved;


        bool isProcessing;
        List<MessageEventArgs> wsMessages;


        private void Awake()
        {
            wsMessages = new List<MessageEventArgs>();
            pClient = new AugmentaPleiadesClient(this);
            tags = new List<string>();
            _lastTags = new List<string>();
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
                sendRegister();
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
                connected = true;
                lastMessageTime = lastUpdateTime;

                while (isProcessing) { }

                wsMessages.Add(e);

            };

        }


        public void sendRegister()
        {
            JSONObject o = JSONObject.Create();
            o.AddField("id", clientID);
            o.AddField("name", clientName);
            JSONObject options = JSONObject.Create();
            options.AddField("downSample", downSample);
            options.AddField("streamClouds", streamClouds);
            options.AddField("streamClusters", streamClusters);
            JSONObject tagsO = JSONObject.Create();
            foreach(var t in tags) tagsO.Add(t);
            options.AddField("tags", tagsO);
            JSONObject coords = JSONObject.Create();
            coords.AddField("axis", "y_up_left");
            coords.AddField("origin", "bottom_left");
            options.AddField("coordinateMapping", coords);
            o.AddField("options", options);

            JSONObject ro = JSONObject.Create();
            ro.AddField("register", o);

            websocket.Send(ro.ToString());
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


            if(connected)
            {
                bool tagsChanged = false;
                if (tags.Count != _lastTags.Count) tagsChanged = true;
                else
                {
                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (tags[i] != _lastTags[i])
                        {
                            tagsChanged = true;
                            break;
                        } 
                    }
                }
                if(streamClouds != _lastStreamClouds || streamClusters != _lastStreamClusters || downSample != _lastDownSample || tagsChanged)
                {
                    sendRegister();
                    _lastStreamClouds = streamClouds;
                    _lastStreamClusters = streamClusters;
                    _lastTags = new List<string>(tags);
                }
            }


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