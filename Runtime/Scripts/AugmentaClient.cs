using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;
using Augmenta;
using static Augmenta.ProtocolOptions;
using static Augmenta.AxisTransform;
using Unity.VisualScripting.YamlDotNet.Core;

namespace AugmentaWebsocketClient
{
    // Duplicate protocol options classes to make them serializable
    // If you know of a better way to do this, please ping us :-)
    [Serializable]
    public class SerializableAxisTransform
    {
        public Augmenta.AxisTransform.AxisMode axis = Augmenta.AxisTransform.AxisMode.ZUpRightHanded;
        public Augmenta.AxisTransform.OriginMode origin = Augmenta.AxisTransform.OriginMode.BottomLeft;
        public bool flipX = false;
        public bool flipY = false;
        public bool flipZ = false;
        public Augmenta.AxisTransform.CoordinateSpace coordinateSpace = Augmenta.AxisTransform.CoordinateSpace.Absolute;
    }

    [Serializable]
    public class SerializableProtocolOptions
    {
        public ProtocolVersion version = ProtocolVersion.Latest;
        public List<string> tags = new();
        public int downSample = 1;
        public bool streamClouds = true;
        public bool streamClusters = true;
        public bool streamClusterPoints = true;
        public bool streamZonePoints = false;
        public RotationMode boxRotationMode = RotationMode.Quaternions;
        public SerializableAxisTransform axisTransform = new();
        public bool useCompression = true;
        public bool usePolling = false;
    }

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

        public SerializableProtocolOptions protocolOptions = new SerializableProtocolOptions();

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
                augmentaClient.options = GetProtocolOptions();
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
            var message = augmentaClient.GetRegisterMessage(clientName);
            Debug.Log(message);
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
                Debug.Log(e.Data);
                augmentaClient.ProcessMessage(e.Data);
            }
            else if (e.IsBinary)
            {
                try
                {
                    augmentaClient.ProcessData(Time.time, e.RawData);
                }
                catch (Exception ex)
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
                var options = GetProtocolOptions();
                if (!options.Equals(augmentaClient.options))
                {
                    augmentaClient.options = options;
                    SendRegister();
                }

                if (augmentaClient.options.usePolling && hasReceivedSincePolling)
                {
                    SendPoll(); //we can do it here since update will be shared with the engine runtime, so it will be called once per frame
                }
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

        private Augmenta.ProtocolOptions GetProtocolOptions()
        {
            Augmenta.ProtocolOptions options = new();
            options.version = protocolOptions.version;
            options.tags = new List<string>(protocolOptions.tags);
            options.downSample = protocolOptions.downSample;
            options.streamClouds = protocolOptions.streamClouds;
            options.streamClusters = protocolOptions.streamClusters;
            options.streamClusterPoints = protocolOptions.streamClusterPoints;
            options.streamZonePoints = protocolOptions.streamZonePoints;
            options.boxRotationMode = protocolOptions.boxRotationMode;
            options.useCompression = protocolOptions.useCompression;
            options.usePolling = protocolOptions.usePolling;
            options.axisTransform.axis = protocolOptions.axisTransform.axis;
            options.axisTransform.origin = protocolOptions.axisTransform.origin;
            options.axisTransform.flipX = protocolOptions.axisTransform.flipX;
            options.axisTransform.flipY = protocolOptions.axisTransform.flipY;
            options.axisTransform.flipZ = protocolOptions.axisTransform.flipZ;
            options.axisTransform.coordinateSpace = protocolOptions.axisTransform.coordinateSpace;
            return options;
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
            {
                ao.transform.parent = (workingScene.wrapperObject as AugmentaScene).objectsContainer.transform;
            }
            else
            {
                ao.transform.parent = client.transform;
            }

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