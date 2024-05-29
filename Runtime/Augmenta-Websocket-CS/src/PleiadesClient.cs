using System;
using System.Collections.Generic;
using System.Numerics;

namespace Augmenta
{

    public class PleaidesClient : GenericPleiadesClient
    {
        Matrix4x4 transform;

        public void update(float time, Matrix4x4 transform)
        {
            this.transform = transform;
            base.update(time);
        }

        override protected BasePObject createObject()
        {
            return new PObject();
        }

        override protected BasePZone createZone()
        {
            return new PZone();
        }

        protected override void processObjectInternal(BasePObject o)
        {
            var obj = (PObject)o;
            obj.parentTransform = transform;
        }
    }

    public abstract class GenericPleiadesClient
    {
        public Dictionary<int, BasePObject> objects = new Dictionary<int, BasePObject>();

        //Call once per frame
        virtual public void update(float time)
        {
            var objectsToRemove = new List<int>();
            foreach (var o in objects.Values)
            {
                o.update(time);
                if (o.timeSinceGhost > 1)
                    objectsToRemove.Add(o.objectID);
            }

            foreach (var oid in objectsToRemove)
            {
                var o = objects[oid];
                removeObject(o);
            }
        }

        public void processData(float time, ReadOnlySpan<byte> data, int offset = 0)
        {
            var type = data[offset];

            if (type == 255) //bundle
            {
                var pos = offset + 1; //offset + sizeof(packettype)
                while (pos < data.Length - 5) //-sizeof(packettype) - sizeof(packetsize)
                {
                    var packetSize = Utils.ReadInt(data, pos + 1);  //pos + sizeof(packettype)
                    processData(time, data.Slice(pos, packetSize), 0);
                    pos += packetSize;
                }
            }

            //Debug.Log("Packet type : " + type);

            switch (type)
            {
                case 0: //Object
                    {
                        processObject(time, data, offset);
                    }
                    break;

                case 1: //Zone
                    {
                        processZone(time, data, offset);
                    }
                    break;
            }
        }

        private void processObject(float time, ReadOnlySpan<byte> data, int offset)
        {
            var objectID = Utils.ReadInt(data, offset + 1 + sizeof(int)); //offset + sizeof(packettype) + sizeof(packetsize)

            BasePObject o = null;
            if (objects.ContainsKey(objectID)) o = objects[objectID];
            if (o == null)
            {
                o = createObject();
                o.objectID = objectID;
                o.onRemove += onObjectRemove;
                objects.Add(objectID, o);
            }

            processObjectInternal(o);

            o.updateData(time, data, offset);
        }

        void processZone(float time, ReadOnlySpan<byte> data, int offset)
        {
        }

        //events
        virtual protected void processObjectInternal(BasePObject o) { }
        abstract protected BasePObject createObject();
        abstract protected BasePZone createZone();

        protected void onObjectRemove(BasePObject o)
        {
            removeObject(o);
        }
        protected void removeObject(BasePObject o)
        {
            objects.Remove(o.objectID);
            o.kill();
        }
    }
}