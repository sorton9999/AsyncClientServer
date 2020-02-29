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
        private const int HDR_SIZE = 28;

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

            try
            {
                HandleFileDataAsync(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("File Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        private async void HandleFileDataAsync(Client client, MessageData messageData)
        {
            await Task.Factory.StartNew( () =>
           {

               if (receivingFile)
               {
                   // The receive block.
                   // Keep putting the received data together until all received, then write it out.
                   //
                   // TODO -- Add the ability to send data to another client
                   //
                   byte[] fData = new byte[messageData.length];
                   Buffer.BlockCopy((byte[])messageData.message, offset, fData, 0, (int)messageData.length);
                   totalRcv += fData.Length;
                   binWriter.Write(fData);
                   if (totalRcv >= fileSize)
                   {
                       receivingFile = false;
                       if (fileSize == (fileData.Length - HDR_SIZE))
                       {
                           Console.WriteLine("Received File: " + fileName);
                       }
                       else
                       {
                           Console.WriteLine("Send File Mismatch - Expect: [{0}], Actual: [{1}]", fileSize, totalRcv);
                       }
                       try
                       {
                           // Create the file and write out the data
                           //
                           // TODO -- Write out this data to another client
                           //
                           string path = filesPath;
                           using (FileStream fsStream = new FileStream(path + fileName, FileMode.Create))
                           using (BinaryWriter writer = new BinaryWriter(fsStream, Encoding.UTF8))
                           {
                               // Had to deal with the 28 byte header
                               writer.Write(fileData, (HDR_SIZE - 1), fileData.Length - HDR_SIZE);
                           }
                       }
                       catch (Exception ex)
                       {
                           Console.WriteLine("Exception caught in process: {0}", ex);
                       }

                   }
                   else
                   {
                       Console.WriteLine("Total Received [{0}] out of [{1}]", totalRcv, fileSize);
                   }
               }
               else
               {
                   // First message gets the file info, including the name and file size.  This will set the
                   // flag to true so the upper block will take over to receive the actual file data.
                   Console.WriteLine("Receiving File: " + messageData.message);
                   Console.WriteLine("Size: " + messageData.length);
                   receivingFile = true;
                   fileName = (string)messageData.message;
                   fileSize = messageData.length;
                   totalRcv = 0;
                   fileData = new byte[fileSize + HDR_SIZE];
                   memStream = new MemoryStream(fileData);
                   binWriter = new BinaryWriter(memStream);
                   memStream.Position = 0;
               }


           }).ConfigureAwait(false);
        }

        public void SetActionData(object data)
        {
            _server = data as TaskServer;
        }

    }
}
