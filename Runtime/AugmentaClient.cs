using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using WebSocketSharp;
using ZstdNet;

namespace AugmentaWebsocketClient
{
    // Inherit from Options classes to mark them as Serializable
    // TODO: Copy and equality methods could be part of the SDK
    [Serializable]
    public class AxisTransformUnity : Augmenta.AxisTransform { }

    [Serializable]
    public class ProtocolOptionsUnity : Augmenta.ProtocolOptions
    {
        new public AxisTransformUnity axisTransform;

        public bool Equals(ProtocolOptionsUnity other)
        {
            if (other is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return version == other.version &&
                   tags.Equals(other.tags) &&
                   downSample == other.downSample &&
                   streamClouds == other.streamClouds &&
                   streamClusters == other.streamClusters &&
                   streamClusterPoints == other.streamClusterPoints &&
                   streamZonePoints == other.streamZonePoints &&
                   boxRotationMode == other.boxRotationMode &&
                   axisTransform.Equals(other.axisTransform) &&
                   useCompression == other.useCompression &&
                   usePolling == other.usePolling;
        }

        public ProtocolOptionsUnity DeepCopy()
        {
            ProtocolOptionsUnity other = new ProtocolOptionsUnity();
            other.version = version;
            other.downSample = downSample;
            other.streamClouds = streamClouds;
            other.streamClusters = streamClusters;
            other.streamClusterPoints = streamClusterPoints;
            other.streamZonePoints = streamZonePoints;
            other.boxRotationMode = boxRotationMode;
            other.axisTransform = axisTransform;
            other.useCompression = useCompression;
            other.usePolling = usePolling;
            other.tags = new List<string>(tags);
            other.axisTransform.axis = axisTransform.axis;
            other.axisTransform.origin = axisTransform.origin;
            other.axisTransform.flipX = axisTransform.flipX;
            other.axisTransform.flipY = axisTransform.flipY;
            other.axisTransform.flipZ = axisTransform.flipZ;
            other.axisTransform.coordinateSpace = axisTransform.coordinateSpace;
            // TODO: originOffset, customMatrix

            return other;
        }
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

        public ProtocolOptionsUnity protocolOptions = new ProtocolOptionsUnity();
        private ProtocolOptionsUnity currentProtocolOptions;

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
            var message = augmentaClient.GetRegisterMessage(clientName, protocolOptions);
            websocketClient.Send(message);
            currentProtocolOptions = protocolOptions.DeepCopy();
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
                        augmentaClient.ProcessData(Time.time, e.RawData, 0, currentProtocolOptions.useCompression);
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
                if (!protocolOptions.Equals(currentProtocolOptions))
                {
                    SendRegister();
                }

                if (currentProtocolOptions.usePolling && hasReceivedSincePolling)
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