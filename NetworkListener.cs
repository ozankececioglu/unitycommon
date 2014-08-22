using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;


public class NetworkListener : MonoBehaviour
{
    TcpListener listener = null;
    Int32 port = 11000;
    void Awake()
    {
        this.listener = new TcpListener(IPAddress.Any, port);
        Debug.Log("network listener initialized at port "+port);
    }

    void Update()
    {
        Byte [] bytes = new Byte [256];
        String data = null;
        int counter = 0;
        try
        {
            Debug.Log("Waiting for a connection... ");

            // Perform a blocking call to accept requests.
            // You could also user server.AcceptSocket() here.
            TcpClient client = this.listener.AcceptTcpClient();
            counter++;
            Debug.Log("#" + counter + " Connected!");

            data = null;

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while ( ( i = stream.Read(bytes, 0, bytes.Length) ) != 0 )
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Debug.Log("Received: "+ data + counter);

                // Process the data sent by the client.
                data = data.ToUpper();

                byte [] msg = System.Text.Encoding.ASCII.GetBytes(data + "Client counter:" + counter);

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Debug.Log("Sent: "+ data + "Client counter:" + counter);
            }

            // Shutdown and end connection
            client.Close();
        }
        catch ( SocketException e )
        {
            Debug.Log("SocketException: "+ e.Message);
        }
        finally
        {
            // Stop listening for new clients.
            //this.listener.Stop();
        }
    }

    void Start()
    {
        this.listener.Start();
    }
    //public void StartListener()
    //{
    //    try
    //    {
    //        this.listener.Start();
    //        Byte [] bytes = new Byte [256];
    //        String data = null;
    //        int counter = 0;
    //        // Enter the listening loop.
    //        while ( true )
    //        {
    //            Console.Write("Waiting for a connection... ");

    //            // Perform a blocking call to accept requests.
    //            // You could also user server.AcceptSocket() here.
    //            TcpClient client = this.listener.AcceptTcpClient();
    //            counter++;
    //            Console.WriteLine("#" + counter + " Connected!");

    //            data = null;

    //            // Get a stream object for reading and writing
    //            NetworkStream stream = client.GetStream();

    //            int i;

    //            // Loop to receive all the data sent by the client.
    //            while ( ( i = stream.Read(bytes, 0, bytes.Length) ) != 0 )
    //            {
    //                // Translate data bytes to a ASCII string.
    //                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
    //                Console.WriteLine("Received: {0}", data + counter);

    //                // Process the data sent by the client.
    //                data = data.ToUpper();

    //                byte [] msg = System.Text.Encoding.ASCII.GetBytes(data + "Client counter:" + counter);

    //                // Send back a response.
    //                stream.Write(msg, 0, msg.Length);
    //                Console.WriteLine("Sent: {0}", data + "Client counter:" + counter);
    //            }

    //            // Shutdown and end connection
    //            client.Close();
    //        }
    //    }
    //    catch ( SocketException e )
    //    {
    //        Console.WriteLine("SocketException: {0}", e);
    //    }
    //    finally
    //    {
    //        // Stop listening for new clients.
    //        this.listener.Stop();
    //    }
    //}
}

