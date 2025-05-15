using UnityEngine;
using UnityEngine.VFX;

namespace Augmenta
{
	public class PCLToGraphicsBuffer : MonoBehaviour
	{
		public AugmentaObject pObject;
		public VisualEffect[] vfxToBind;

		public string vfxPositionProperty = "Positions";
        public string vfxCountProperty = "PositionsCount";

		[Tooltip("Unlimited if <= 0")] public int capacity = 0;


		GraphicsBuffer pointsBuffer;

		void Start()
		{
			if (!pObject) pObject = GetComponent<AugmentaObject>();

			//Bind vfx
			if (vfxToBind.Length == 0)
			{
				vfxToBind = GetComponentsInChildren<VisualEffect>();
			}

			foreach (var vfx in vfxToBind)
			{
				if (!vfx.HasGraphicsBuffer(vfxPositionProperty))
				{
					Debug.LogWarning("VFX Graph " + vfx.name + " should have a GraphicsBuffer property named Positions.");
				}

				if (!vfx.HasInt(vfxCountProperty))
				{
					Debug.LogWarning("VFX Graph " + vfx.name + " should have an int property named PositionsCount.");
				}

				vfx.SetInt(vfxCountProperty, 0);
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
			foreach (var vfx in vfxToBind)
			{
				vfx.SetGraphicsBuffer(vfxPositionProperty, pointsBuffer);
			}
		}

		void BindBufferCount(int bufferCount)
		{
			foreach (var vfx in vfxToBind)
			{
				vfx.SetInt(vfxCountProperty, bufferCount);
			}
		}

		public void BindAugmentaObject(AugmentaObject pObjectToBind)
		{
			pObject = pObjectToBind;
		}
	}
}