using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public enum NetworkState
{
    STATE_READY = 0,
    STATE_NOTREADY,
    STATE_CONNECTED,
    STATE_DISCONNECTED,
    STATE_ERROR
};

public class NetworkTCPListener
{    
	public bool socketReady = false;
	public String host;
    public Int32 port;
    public Action<string> networkMessageReceived;
    public Action<NetworkState> networkStatusChanged;
	
	TcpClient theSocket;
    NetworkStream theStream;
    StreamWriter theWriter;
    StreamReader theReader;

	public NetworkTCPListener(string thost, int tport) {
		
		host = thost;
		port = tport;
		
		if (networkStatusChanged != null)
			networkStatusChanged(NetworkState.STATE_NOTREADY);
	}

    public void SetupSocket()
    {
        try
        {
            theSocket = new TcpClient(host, port);
            theStream = theSocket.GetStream();
            theWriter = new StreamWriter(theStream);
            theReader = new StreamReader(theStream);
            socketReady = true;
			
			if (networkStatusChanged != null)
            	networkStatusChanged(NetworkState.STATE_READY);
        }
        catch ( Exception e )
        {
            Common.Log("Socket error:" + e);
			
			if (networkStatusChanged != null)
            	networkStatusChanged(NetworkState.STATE_ERROR);
        }
    }

    public void WriteSocket(string theLine)
    {
        if ( !socketReady )
            return;
		
        theWriter.Write(theLine + "\r\n");
        theWriter.Flush();
    }

    public string ReadSocket()
    {
        if ( !socketReady )
            return "socket is not ready";
        if ( !theStream.DataAvailable )
            return "none";
		
        try
        {
            byte [] buffer = new byte [theSocket.ReceiveBufferSize];
            int sizeRead = theStream.Read(buffer, 0, buffer.Length);
            String message = Encoding.UTF8.GetString(buffer);
            Common.Log("Size read = " + sizeRead + " Message = " + message);
			
			if (networkMessageReceived != null)
				networkMessageReceived(message);
			
			return message;
        }
        catch ( Exception e )
        {
            Common.Log(e.Message);
			
			if (networkStatusChanged != null)
            	networkStatusChanged(NetworkState.STATE_ERROR);
			
			return null;
        }
    }

    public void CloseSocket()
    {
        if ( !socketReady )
            return;
		
        theWriter.Close();
        theReader.Close();
        theSocket.Close();
        socketReady = false;
		
		if (networkStatusChanged != null)
        	networkStatusChanged(NetworkState.STATE_NOTREADY);
    }

    public void MaintainConnection()
    {
        if ( !theStream.CanRead )
        {
            SetupSocket();
        }
        else
        {
			if (networkStatusChanged != null)
            	networkStatusChanged(NetworkState.STATE_CONNECTED);
        }
    }
}