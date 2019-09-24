using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{

    public class ClientData<T> : IData
    {
        public enum ClientState
        {
            FAULTED = -99,
            UNINIT = -1,
            CONNECTED,
            DISCONNECTED,
            EXITING
        }

        public byte this[int i]
        {
            get { return data[i]; }
            set { data[i] = value; }
        }

        private T value;

        private byte[] data = null;

        public ClientData(int size)
        {
            DataSize = size;
            this.data = new byte[DataSize];
            State = ClientState.UNINIT;
        }

        //public ClientData(T[] data)
        //{
        //    this.data = data;
        //    State = ClientState.UNINIT;
        //}

        public ClientData(ClientData<T> cIn)
        {
            data = cIn.Data;
            if (State == ClientState.CONNECTED)
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
            }
            State = ClientState.DISCONNECTED;
        }

        //public T[] Data
        //{
        //    get { return data; }
        //    set { data = value; }
        //}

        public ClientData(T item)
        {
            this.value = item;
            //SerializeValueToData();
        }

        public T Value
        {
            get { return this.value; }
            private set { this.value = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public void SerializeValueToData()
        {
            data = SerializeToByteArray(this.value);
        }

        public T DeserializeData()
        {
            return DeserializeFromByteArray<T>(data);
        }

        public Socket ClientSocket
        {
            get;
            set;
        }

        public void ClearSocket()
        {
            if (ClientSocket.Connected)
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();
            }
        }

        public void ClearData()
        {
            if (data != null)
            {
                Array.Clear(data, 0, data.Length);
            }
        }

        public ClientState State
        {
            get;
            set;
        }

        public int DataSize
        {
            get;
            set;
        }

        public static byte[] SerializeToByteArray<U>(U obj)
        {
            if (obj == null)
            {
                return null;
            }
            using (var ms = new MemoryStream())
            {
                try
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, obj);
                    return ms.ToArray();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static U DeserializeFromByteArray<U>(byte[] byteArr)
        {
            if (byteArr == null)
            {
                return default(U);
            }
            U obj = default(U);
            using (var ms = new MemoryStream(byteArr))
            {
                try
                {
                    var bf = new BinaryFormatter();
                    //ms.Write(byteArr, 0, byteArr.Length);
                    //ms.Seek(0, SeekOrigin.Begin);
                    obj = (U)bf.Deserialize(ms);
                }
                catch (Exception)
                {

                }
            }
            return obj;
        }
    }
}
