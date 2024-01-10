using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

public class PCLToGraphicsBuffer : MonoBehaviour  
{
	public PObject pObject;
	public VisualEffect[] vfxToBind;

	[Tooltip("Unlimited if <= 0")]public int capacity = 0;


	GraphicsBuffer pointsBuffer;

	void Start()
    {
	    //Bind vfx
	    if (vfxToBind.Length == 0)
	    {
		    vfxToBind = GetComponentsInChildren<VisualEffect>();
	    }

	    foreach (var vfx in vfxToBind)
	    {
		    if (!vfx.HasGraphicsBuffer("Positions"))
		    {
			    Debug.LogWarning("VFX Graph "+vfx.name+" should have a GraphicsBuffer property named Positions.");
		    }

		    if (!vfx.HasInt("PositionsCount")) {
			    Debug.LogWarning("VFX Graph " + vfx.name + " should have an int property named PositionsCount.");
		    }

		    vfx.SetInt("PositionsCount", 0);
		}

	    if (capacity > 0)
	    {
		    CreateAndBindPositionsBuffer(capacity);
			BindBufferCount(0);
	    }
    }

	void Update()
	{
		if (!pObject)
		{
			BindBufferCount(0);
			return;
		}

		int bufferCount = pObject.points.Length;

		if (capacity > 0)
	    {
			//Fixed capacity
			if (bufferCount > capacity)
			{
				Debug.LogWarning("Current points count is over capacity, aborting conversion.");
			}
			else
			{
				pointsBuffer.SetData(pObject.points);
				BindBufferCount(bufferCount);
			}
	    }
	    else
	    {
			//Dynamic capacity
			if (pointsBuffer == null && bufferCount > 0)
			{
				CreateAndBindPositionsBuffer(bufferCount);
				BindBufferCount(bufferCount);
			}
			else
			{
				if (bufferCount > pointsBuffer.count)
				{
					CreateAndBindPositionsBuffer(bufferCount);
					BindBufferCount(bufferCount);
				}
				else
				{
					pointsBuffer.SetData(pObject.points);
					BindBufferCount(bufferCount);
				}
			}
	    }
    }

    void OnDestroy()
    {
	    pointsBuffer?.Dispose();
	    pointsBuffer = null;
	}

    void CreateAndBindPositionsBuffer(int bufferSize)
    {
	    if (pointsBuffer != null)
	    {
		    pointsBuffer?.Dispose();
		    pointsBuffer = null;
		}

	    pointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 3 * sizeof(float));
	    foreach (var vfx in vfxToBind) {
		    vfx.SetGraphicsBuffer("Positions", pointsBuffer);
	    }
    }

    void BindBufferCount(int bufferCount)
    {
	    foreach (var vfx in vfxToBind) {
		    vfx.SetInt("PositionsCount", bufferCount);
	    }
	}

    public void BindPObject(PObject pObjectToBind)
    {
	    pObject = pObjectToBind;
    }
}
