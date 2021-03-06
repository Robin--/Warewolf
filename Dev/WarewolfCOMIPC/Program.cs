﻿using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using WarewolfCOMIPC.Client;
// ReSharper disable NonLocalizedString

namespace WarewolfCOMIPC
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) return;

            string token = args[0];

            // Create new named pipe with token from client
            Console.WriteLine("Starting Server Pipe Stream");
            using (var pipe = new NamedPipeServerStream(token, PipeDirection.InOut,253, PipeTransmissionMode.Message))
            {
                Console.WriteLine("Waiting Server Pipe Stream");
                pipe.WaitForConnection();
                AcceptMessagesFromPipe(pipe);
            }
        }

        private static void AcceptMessagesFromPipe(NamedPipeServerStream pipe)
        {
            

            // Receive CallData from client
            //var formatter = new BinaryFormatter();
            Console.WriteLine("Client Connected to Server Pipe Stream");
            var serializer = new JsonSerializer();
            var sr = new StreamReader(pipe);
            var jsonTextReader = new JsonTextReader(sr);
            var callData = serializer.Deserialize(jsonTextReader, typeof(CallData));
            Console.WriteLine("Client Data read and Deserialized to Server Pipe Stream");
            Console.WriteLine(callData.GetType());
            var data = (CallData)callData;

            while(data.Status != KeepAliveStatus.Close)
            {
                Console.WriteLine("Executing");
                LoadLibrary(data, serializer, pipe);
                AcceptMessagesFromPipe(pipe);
            }
        }

        private static void LoadLibrary(CallData data, JsonSerializer formatter, NamedPipeServerStream pipe)
        {
            var execute = data.Execute;
            if (!string.IsNullOrEmpty(data.ExecuteType))
            {
                Enum.TryParse(data.ExecuteType, true, out execute);
            }

            if (execute == Execute.GetType)
            {
                Console.WriteLine("Executing GetType for:"+data.CLSID);
                var type = Type.GetTypeFromCLSID(data.CLSID, true);
                Console.WriteLine("Got Type:" + type.FullName);
                var sw = new StreamWriter(pipe);
                Console.WriteLine("Serializing and sending:" + type.FullName);
                formatter.Serialize(sw, type);
                sw.Flush();
                Console.WriteLine("Sent:" + type.FullName);
            }
        }
        
    }    
}
