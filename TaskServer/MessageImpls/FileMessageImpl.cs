using CliServLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskCommon;
using TcpLib;

namespace TaskServer
{
    public class FileMessageImpl : IMessageImpl
    {
        private TaskServer _server = null;

        private bool receivingFile = false;

        private long totalRcv = 0;
        private int offset = 0;

        // The size of the received file
        long fileSize = 0;

        // Receive file data as a byte stream
        private MemoryStream memStream = null;
        private BinaryWriter binWriter = null;

        // The byte stream of file data
        private byte[] fileData = null;

        // The name of the received file
        private string fileName = String.Empty;

        private string filesPath = String.Empty;

        private const string FILE_DIR = "TempFiles";


        public FileMessageImpl()
                : base()
        {
            // For any file transfers, put them in a known default location
            try
            {
                filesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                filesPath += "\\" + FILE_DIR + "\\";
                System.IO.Directory.CreateDirectory(filesPath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;
            if (receivingFile)
            {
                byte[] fData = new byte[messageData.length];
                Buffer.BlockCopy((byte[])messageData.message, offset, fData, 0, (int)messageData.length);
                totalRcv += fData.Length;
                binWriter.Write(fData);
                if (totalRcv >= fileSize)
                {
                    receivingFile = false;
                    if (fileSize == (fileData.Length - 28))
                    {
                        Console.WriteLine("Received File: " + fileName);
                    }
                    else
                    {
                        Console.WriteLine("Send File Mismatch - Expect: [{0}], Actual: [{1}]", fileSize, totalRcv);
                    }
                    try
                    {
                        //string path = "C:\\Users\\steve\\test\\";
                        string path = filesPath;
                        using (FileStream fsStream = new FileStream(path + fileName, FileMode.Create))
                        using (BinaryWriter writer = new BinaryWriter(fsStream, Encoding.UTF8))
                        {
                            writer.Write(fileData, 27, fileData.Length - 28);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception caught in process: {0}", ex);
                        retVal = false;
                    }

                }
                else
                {
                    Console.WriteLine("Total Received [{0}] out of [{1}]", totalRcv, fileSize);
                }
            }
            else
            {
                Console.WriteLine("Receiving File: " + messageData.message);
                Console.WriteLine("Size: " + messageData.length);
                receivingFile = true;
                fileName = (string)messageData.message;
                fileSize = messageData.length;
                totalRcv = 0;
                fileData = new byte[fileSize + 28];
                memStream = new MemoryStream(fileData);
                binWriter = new BinaryWriter(memStream);
                memStream.Position = 0;
            }
            return retVal;
        }

        public void SetActionData(object data)
        {
            //if (data != null)
            //{
            //    receivingFile = (bool)data;
            //}
            _server = data as TaskServer;
        }

    }
}
