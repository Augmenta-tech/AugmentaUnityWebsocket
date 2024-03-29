using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;
using UnityEngine.Events;

public class PleiadesClient : MonoBehaviour
{
	public string ipAddress
	{
		get => _ipAddress;
		set { _ipAddress = value;
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
        set {
	        _port = value;
	        if (websocket != null) {
		        websocket.Close();
		        websocket = null;
	        }
        }
	}

	public bool mirrorX = false;
	public bool mirrorY = false;
	public bool mirrorZ = false;

    WebSocket websocket;

    Dictionary<int, PObject> objects;
    public GameObject objectPrefab;

    public UnityEvent<PObject> OnPObjectCreated;

	public bool connected = false;
    public bool receivingData = false;

    [SerializeField] string _ipAddress = "127.0.0.1";
    [SerializeField] int _port = 6060;

    float lastConnectTime;
    float lastMessageTime;

	async void Start()
    {
        init();
        connect();
    }

    void init()
    {
        objects = new Dictionary<int, PObject>();

        websocket = new WebSocket("ws://" + ipAddress + ":" + port);
        websocket.OnOpen += () =>
        {
	        connected = true;
			Debug.Log("Connection "+ "ws://" + ipAddress + ":" + port + " opened !");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
	        connected = false;
			Debug.Log("Connection "+ "ws://" + ipAddress + ":" + port + " closed.");
        };

        websocket.OnMessage += (bytes) =>
        {
	        connected = true;
			lastMessageTime = Time.time;
			processData(bytes);
        };
    }


    async void connect()
    {
        Debug.Log("Connecting websocket...");
        lastConnectTime = Time.time;
        await websocket.Close();
        await websocket.Connect();
    }

    // Update is called once per frame
    /*async*/
    void Update()
    {
        if (websocket == null) init();

#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket.State == WebSocketState.Closed && Time.time - lastConnectTime > 1)
        {
            connect();
        }

        websocket.DispatchMessageQueue();
#endif

        List<int> objectsToRemove = new List<int>();
        foreach (var o in objects.Values) if (o.timeSinceGhost > 1) objectsToRemove.Add(o.objectID);
        foreach (var oid in objectsToRemove)
        {
            Destroy(objects[oid].gameObject);
            objects.Remove(oid);
        }

        receivingData = (Time.time - lastMessageTime) < 1;
    }


    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    void processData(byte[] data, int offset = 0)
    {
        var type = data[offset];

        if (type == 255) //bundle
        {
            int pos = offset + 1;
            while (pos < data.Length - 5)
            {
                int packetSize = BitConverter.ToInt32(data, pos + 1);
                processData(data, pos);
                pos += packetSize;
            }
        }


        //Debug.Log("Packet type : " + type);

        switch (type)
        {
            case 0: //Object
                {
                    processObject(data, offset);
                }
                break;

            case 1: //Zone
                {
                    processZone(data, offset);
                }
                break;
        }

    }

    void processObject(byte[] data, int offset)
    {
        int objectID = BitConverter.ToInt32(data, offset + 5);

        PObject o = null;
        if (objects.ContainsKey(objectID)) o = objects[objectID];
        if (o == null)
        {
            o = Instantiate(objectPrefab).GetComponent<PObject>();
            o.objectID = objectID;
            o.onRemove += onObjectRemove;
            objects.Add(objectID, o);
            o.transform.parent = transform;
            o.mirrorX = mirrorX;
            o.mirrorY = mirrorY;
            o.mirrorZ = mirrorZ;

            OnPObjectCreated?.Invoke(o);
        }

        o.updateData(data, offset);

    }

    void processZone(byte[] data, int offset)
    {

    }


    //events
    void onObjectRemove(PObject o)
    {
        objects.Remove(o.objectID);
        o.kill();
    }
}