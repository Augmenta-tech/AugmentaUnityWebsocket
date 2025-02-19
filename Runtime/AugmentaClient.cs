using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;
using ZstdNet;

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
                if (websocketClient != null)
                {
                    websocketClient.Close();
                    websocketClient = null;
                }
            }
        }

        public int port
        {
            get => _port;
            set
            {
                _port = value;
                if (websocketClient != null)
                {
                    websocketClient.Close();
                    websocketClient = null;
                }
            }
        }

        WebSocket websocketClient;
        AugmentaUnityClient augmentaClient;

        float lastUpdateTime;
        float lastConnectTime;
        float lastMessageTime;

        public enum ProtocolVersion { Latest = 0, V2 = 2 };

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
        public ProtocolVersion version = ProtocolVersion.Latest;
        public int downSample = 1;
        public bool streamClouds = true;
        public bool streamClusters = true;
        public bool streamClusterPoints = true;
        public bool streamZonePoints = false;
        public bool useCompression = false;
        public bool usePolling = false;
        public List<string> tags;

        int _lastDownSample = 1;
        bool _lastStreamClouds = true;
        bool _lastStreamClusters = true;
        bool _lastStreamClusterPoints = true;
        bool _lastStreamZonePoints = false;
        bool _lastUseCompression = false;
        bool _lastUsePolling = false;
        List<string> _lastTags;

        bool hasReceivedSincePolling = false;


        [Header("Events")]
        public UnityEvent<AugmentaObject> OnObjectCreated;
        public UnityEvent<AugmentaObject> OnObjectRemoved;


        bool isProcessing;
        List<MessageEventArgs> wsMessages;


        private void Awake()
        {
            wsMessages = new List<MessageEventArgs>();
            augmentaClient = new AugmentaUnityClient(this);
            _lastTags = new List<string>();

        }


        void OnDisable()
        {
            websocketClient.Close();
            augmentaClient.Clear();
            augmentaClient = null;
            wsMessages.Clear();
        }

        private void OnEnable()
        {
            wsMessages = new List<MessageEventArgs>();
            augmentaClient = new AugmentaUnityClient(this);
        }

        void Start()
        {
            Init();
            Connect();
        }

        void Init()
        {
            websocketClient = new WebSocket("ws://" + ipAddress + ":" + port);
            websocketClient.OnOpen += (sender, e) =>
            {
                connected = true;
                Debug.Log("Connection " + "ws://" + ipAddress + ":" + port + " opened !");
                SendRegister();
            };

            websocketClient.OnError += (sender, e) =>
            {
                Debug.Log("Error! " + e.Message);
            };

            websocketClient.OnClose += (sender, e) =>
            {
                connected = false;
                Debug.Log("Connection " + "ws://" + ipAddress + ":" + port + " closed.");
            };

            websocketClient.OnMessage += (sender, e) =>
            {
                connected = true;
                lastMessageTime = lastUpdateTime;

                while (isProcessing) { }

                wsMessages.Add(e);

            };
        }


        public void SendRegister()
        {
            Augmenta.ProtocolOptions options = new Augmenta.ProtocolOptions();
            options.downSample = downSample;
            options.streamClouds = streamClouds;
            options.streamClusters = streamClusters;
            options.streamClusterPoints = streamClusterPoints;
            // ?
            options.streamZonePoints = streamZonePoints;
            options.useCompression = useCompression;
            options.usePolling = usePolling;
            options.boxRotationMode = Augmenta.ProtocolOptions.RotationMode.Degrees;
            options.tags = tags;
            options.axisTransform.axis = Augmenta.AxisTransform.AxisMode.YUpLeftHanded;
            options.axisTransform.origin = Augmenta.AxisTransform.OriginMode.BottomLeft;

            var message = augmentaClient.GetRegisterMessage(clientName, options);
            websocketClient.Send(message);
        }

        void ProcessMessage(MessageEventArgs e)
        {
#if !UNITY_EDITOR
            try
            {
#endif
            if (e.IsText)
            {
                augmentaClient.ProcessMessage(e.Data);
            }
            else if (e.IsBinary)
            {
                try
                {
                    augmentaClient.ProcessData(Time.time, e.RawData, 0, useCompression);

                }
                catch(Exception ex)
                {
                    Debug.LogWarning("Error in processing " + ex.Message);
                }

                hasReceivedSincePolling = true;

            }
#if !UNITY_EDITOR
            }
            catch (Exception err)
            {
                Debug.LogError("Error processing message : " + err);
            }

#endif
        }



        void Connect()
        {
            Debug.Log("Connecting websocket...");
            StartCoroutine(ConnectCoroutine());
        }

        void SendPoll()
        {
            var message = augmentaClient.GetPollMessage();
            websocketClient.Send(message);
        }

        IEnumerator ConnectCoroutine()
        {
            lastConnectTime = Time.time;
            websocketClient.Close();
            websocketClient.Connect();
            yield return null;
        }

        // Update is called once per frame
        /*async*/
        void Update()
        {
            if (websocketClient == null) Init();


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


                if (streamClouds != _lastStreamClouds 
                    || streamClusters != _lastStreamClusters 
                    || streamClusterPoints != _lastStreamClusterPoints
                    || streamZonePoints != _lastStreamZonePoints 
                    || downSample != _lastDownSample
                    || useCompression != _lastUseCompression
                    || usePolling != _lastUsePolling
                    || tagsChanged)
                {

                    SendRegister();

                    _lastStreamClouds = streamClouds;
                    _lastStreamClusters = streamClusters;
                    _lastStreamClusterPoints = streamClusterPoints;
                    _lastStreamZonePoints = streamZonePoints;
                    _lastDownSample = downSample;
                    _lastUseCompression = useCompression;
                    _lastUsePolling = usePolling;

                    _lastTags = new List<string>(tags);
                }


                if (connected && usePolling && hasReceivedSincePolling) SendPoll(); //we can do it here since update will be shared with the engine runtime, so it will be called once per frame
            }


#if !UNITY_WEBGL || UNITY_EDITOR
            if ((!connected || !websocketClient.IsAlive) && Time.time - lastConnectTime > 1)
            {
                Connect();
            }

#endif
            lastUpdateTime = Time.time;

            isProcessing = true;
            foreach (var e in wsMessages) ProcessMessage(e);
            wsMessages.Clear();
            isProcessing = false;


            augmentaClient.Update(Time.time);
            receivingData = (Time.time - lastMessageTime) < 1;
        }


        private void OnApplicationQuit()
        {
            websocketClient.Close();
        }

    }

    internal class AugmentaUnityClient : Augmenta.Client<AugmentaUnityObject, Vector3>
    {
        public AugmentaClient client;
        public AugmentaContainer aWorldContainer;

        public AugmentaUnityClient(AugmentaClient client)
        {
            this.client = client;
        }

        //The following overrides are necessary in Unity because we need to create MonoBehaviour objects aside the "native" PObject (can't inherit more than one class)
        //In a regular C# project, you may not need to override these methods
        override protected Augmenta.BaseObject CreateObject()
        {
            AugmentaObject ao = GameObject.Instantiate(client.objectPrefab).GetComponent<AugmentaObject>();
            if (workingScene != null)
                ao.transform.parent = (workingScene.wrapperObject as AugmentaScene).objectsContainer.transform;
            else
                ao.transform.parent = client.transform;

            return new AugmentaUnityObject(ao);
        }

        override protected void OnContainerCreated(ref Augmenta.Container<Vector3> newContainer)
        {
            AugmentaContainer c = new GameObject().AddComponent<AugmentaContainer>();
            c.Setup(newContainer, client);
            c.transform.SetParent(client.transform, false);
            aWorldContainer = c;
        }

        public override void Clear()
        {
            base.Clear();
            if (aWorldContainer != null) GameObject.Destroy(aWorldContainer.gameObject);
            aWorldContainer = null;
        }
    }
}