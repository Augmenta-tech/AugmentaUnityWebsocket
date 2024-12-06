using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;

namespace Augmenta
{
    using AugmentaPContainer = PContainer<Vector3>;
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
        public GameObject scenePrefab;
        public GameObject zonePrefab;
        public GameObject objectPrefab;

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
        public bool streamZonePoints = false;
        public List<string> tags;

        int _lastDownSample = 1;
        bool _lastStreamClouds = true;
        bool _lastStreamClusters = true;
        bool _lastStreamZonePoints = false;
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
            foreach (var t in tags) tagsO.Add(t);
            options.AddField("tags", tagsO);
            JSONObject coords = JSONObject.Create();
            coords.AddField("axis", "y_up_left");
            coords.AddField("origin", "bottom_left");
            options.AddField("coordinateMapping", coords);
            options.AddField("boxRotationMode", "Degrees");
            o.AddField("options", options);

            JSONObject ro = JSONObject.Create();
            ro.AddField("register", o);

            websocket.Send(ro.ToString());
        }

        void processMessage(MessageEventArgs e)
        {
#if !UNITY_EDITOR
            try
            {
#endif
            if (e.IsText)
            {
                pClient.processMessage(e.Data);
            }
            else if (e.IsBinary)
            {
                pClient.processData(Time.time, e.RawData);

            }
#if !UNITY_EDITOR
            }
            catch (Exception err)
            {
                Debug.LogError("Error processing message : " + err);
            }

#endif
        }



        void connect()
        {
            Debug.Log("Connecting websocket...");
            StartCoroutine(connectCoroutine());
        }

        IEnumerator connectCoroutine()
        {
            lastConnectTime = Time.time;
            websocket.Close();
            websocket.Connect();
            yield return null;
        }

        // Update is called once per frame
        /*async*/
        void Update()
        {
            if (websocket == null) init();


            if (connected)
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
                if (streamClouds != _lastStreamClouds || streamClusters != _lastStreamClusters || downSample != _lastDownSample || streamZonePoints != _lastStreamZonePoints || tagsChanged)
                {
                    sendRegister();
                    _lastStreamClouds = streamClouds;
                    _lastStreamClusters = streamClusters;
                    _lastStreamZonePoints = streamZonePoints;
                    _lastTags = new List<string>(tags);
                }
            }


#if !UNITY_WEBGL || UNITY_EDITOR
            if ((!connected || !websocket.IsAlive) && Time.time - lastConnectTime > 1)
            {
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

    internal class AugmentaPleiadesClient : PleiadesClient<AugmentaPObject, Vector3>
    {
        public AugmentaClient client;
        public AugmentaContainer aWorldContainer;

        public AugmentaPleiadesClient(AugmentaClient client)
        {
            this.client = client;
        }

        //The following overrides are necessary in Unity because we need to create MonoBehaviour objects aside the "native" PObject (can't inherit more than one class)
        //In a regular C# project, you may not need to override these methods
        override protected BasePObject createObject()
        {
            AugmentaObject ao = GameObject.Instantiate(client.objectPrefab).GetComponent<AugmentaObject>();
            if (workingScene != null)
                ao.transform.parent = (workingScene.wrapperObject as AugmentaScene).objectsContainer.transform;
            else
                ao.transform.parent = client.transform;

            return new AugmentaPObject(ao);
        }

        override protected AugmentaPContainer createContainerInternal(JSONObject o)
        {
            AugmentaPContainer p = base.createContainerInternal(o);
            AugmentaContainer c = new GameObject().AddComponent<AugmentaContainer>();
            c.setup(p, client);
            c.transform.parent = client.transform;
            aWorldContainer = c;
            return p;
        }

        public override void clear()
        {
            base.clear();
            if (aWorldContainer != null) GameObject.Destroy(aWorldContainer.gameObject);
            aWorldContainer = null;
        }
    }
}