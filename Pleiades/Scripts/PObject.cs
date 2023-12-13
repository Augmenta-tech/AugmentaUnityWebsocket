using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PObject : MonoBehaviour
{
    public int objectID;
    public Vector3[] points;

    //cluster
    public enum State { Enter = 0, Update = 1, Leave = 2, Ghost = 3 };
    public State state;

    public Vector3 centroid;
    public Vector3 velocity;
    public Vector3 minBounds;
    public Vector3 maxBounds;
    [Range(0, 1)]
    public float weight;

    float lastUpdateTime;

    public float killDelayTime = 0;

    public float timeSinceGhost;
    public bool drawDebug;

    public enum PositionUpdateMode { None, Centroid, BoxCenter }
    public PositionUpdateMode posUpdateMode = PositionUpdateMode.Centroid;

    public enum CoordMode { Absolute, Relative }
    public CoordMode pointMode = CoordMode.Relative;

    public delegate void OnRemoveEvent(PObject obj);
    public event OnRemoveEvent onRemove;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastUpdateTime > .5f) timeSinceGhost = Time.time;
        else timeSinceGhost = -1;
    }

    public void updateData(byte[] data, int offset)
    {

        int pos = offset + 9; //packet type (1) + packet size (4) + objectID (4)
        while (pos < data.Length)
        {
            int propertyID = BitConverter.ToInt32(data, pos);
            int propertySize = BitConverter.ToInt32(data, pos + 4);

            if(propertySize < 0)
            {
                Debug.LogWarning("Error : property size < 0");
                break;
            }

            switch (propertyID)
            {
                case 0: updatePointsData(data, pos + 8); break;
                case 1: updateClusterData(data, pos + 8); break;
            }

            pos += propertySize;
        }

        lastUpdateTime = Time.time;

    }


    void updatePointsData(byte[] data, int offset)
    {
        int numPoints = BitConverter.ToInt32(data, offset);

        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            int si = offset + 4 + (i * 12);
            Vector3 p = new Vector3(BitConverter.ToSingle(data, si), BitConverter.ToSingle(data, si + 4), BitConverter.ToSingle(data, si + 8));
            if (pointMode == CoordMode.Absolute) points[i] = p;
            else points[i] = transform.parent.TransformPoint(p);
        }
    }

    void updateClusterData(byte[] data, int offset)
    {
        State state = (State)BitConverter.ToInt32(data, offset);
        if (state == State.Leave) //Will leave
        {
            onRemove(this);
            return;
        }

        Vector3[] clusterData = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            int si = offset + 4 + i * 12;

            Vector3 p = new Vector3(BitConverter.ToSingle(data, si), BitConverter.ToSingle(data, si + 4), BitConverter.ToSingle(data, si + 8));
            if (pointMode == CoordMode.Absolute) clusterData[i] = transform.parent.InverseTransformPoint(p);
            else clusterData[i] = p;
        }

        centroid = clusterData[0];
        velocity = clusterData[1];
        minBounds = clusterData[2];
        maxBounds = clusterData[3];
        weight = BitConverter.ToSingle(data, offset + 4 + 4 * 12);

        switch (posUpdateMode)
        {
            case PositionUpdateMode.None:
                break;
            case PositionUpdateMode.Centroid:
                transform.localPosition = centroid;
                break;
            case PositionUpdateMode.BoxCenter:
                transform.localPosition = (minBounds + maxBounds) / 2;
                break;
        }
    }

    public void kill()
    {
        if (killDelayTime == 0)
        {
            Destroy(gameObject);
            return;
        }

        points = new Vector3[0];
        StartCoroutine(killForReal(killDelayTime));
    }

    IEnumerator killForReal(float timeBeforeKill)
    {
        yield return new WaitForSeconds(timeBeforeKill);
        Destroy(gameObject);
    }

    void OnDrawGizmos() 
    { 
        if (drawDebug)
        {
            Color c = Color.HSVToRGB((objectID * .1f) % 1, 1, 1); //Color.red;// getColor();
            if (state == State.Ghost) c = Color.gray / 2;

            Gizmos.color = c;
            foreach (var p in points) Gizmos.DrawLine(p, p + Vector3.forward * .01f);

            Gizmos.color = c + Color.white * .3f;
            Gizmos.DrawWireSphere(centroid, .03f);
            Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);
        }
    }
}
