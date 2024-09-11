using System;
using System.IO;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
#endif

namespace UnityWebServer
{
    /// <summary>
    /// Implementation of a simple HTTP server.
    /// How to use: Inherit from this class and override the abstract HandleRequest() method.
    /// </summary>
    public abstract class HttpServer : IDisposable
    {
#if UNITY_WSA && !UNITY_EDITOR
        private StreamSocketListener socketListener;
#else
		Queue<HttpRequest> mainThreadRequests;
		ThreadedTaskQueue taskq;
		TcpListener listener;
#endif
        private readonly int port;
		private readonly  int workerThreads = 2;

        protected HttpServer(int port)
        {
            this.port = port;
            IgnoreExceptions = true;
#if UNITY_WSA && !UNITY_EDITOR
#else
			this.workerThreads = workerThreads + 1;
			mainThreadRequests = new Queue<HttpRequest> ();
#endif
        }

        public bool IgnoreExceptions { get; set; }


        public string GetLocalIPAddress() {
#if UNITY_WSA && !UNITY_EDITOR
            foreach (Windows.Networking.HostName hostName in Windows.Networking.Connectivity.NetworkInformation.GetHostNames())
            {
                if (hostName.DisplayName.Split(".".ToCharArray()).Length == 4)
                {
                    return hostName.DisplayName + ":" + port;
                }
            }
            return "";
#else
            string hostName = System.Net.Dns.GetHostName();
            System.Net.IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(hostName);
            foreach (System.Net.IPAddress ip in hostEntry.AddressList) {
                if (ip.ToString().Split(".".ToCharArray()).Length == 4) {
                    return ip + ":" + port;
                }
            }
#endif
            return null;
        }


        public
#if UNITY_WSA && !UNITY_EDITOR
            async 
#endif
        void Start()
        {
#if UNITY_WSA && !UNITY_EDITOR
            socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += OnConnectionReceived;

            await socketListener.BindServiceNameAsync(port.ToString());
#else
            listener = new TcpListener (System.Net.IPAddress.Any, port);
			listener.Start (8);
			taskq = new ThreadedTaskQueue (workerThreads + 1);
			taskq.PushTask (AcceptConnections);
#endif
            Debug.LogFormat("UnityWebServer: Listening on http://{0}", GetLocalIPAddress());
        }

        public void Stop()
        {
			Dispose();
            Debug.Log("UnityWebServer: Stopped");
        }

#if UNITY_WSA && !UNITY_EDITOR
		public void Update ()
		{
		}
#else
        public void Update ()
		{
			lock (mainThreadRequests) {
				while (mainThreadRequests.Count > 0) {
					HttpRequest request = mainThreadRequests.Dequeue ();
					ProcessRequest (request);
				}
			}
		}

		void ProcessRequest (HttpRequest request)
		{
			HttpResponse response = HttpResponse.CreateDefaultHttpResponse(request); //Create the default response object for this request

			try
			{
				if(request.IsValid) {
					HandleRequest(request, response);     //call the abstract method if the request is valid
				}
				response.Send(request.stream);
			}
			catch (Exception)
			{
				Debug.LogError("ex");
				if (!IgnoreExceptions)  //Only throw exceptions if they are enabled
					throw;
				HttpResponse responseInternalError = HttpResponse.CreateDefaultHttpResponse (request);
				responseInternalError.StatusCode = HttpStatusCode.InternalServerError;
				responseInternalError.Send(request.stream);
			}
			finally
			{
				request.Close ();   //This call is important: This closes the connection to the client.
			}
		}

#endif

#if UNITY_WSA && !UNITY_EDITOR
        private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            ProcessRequestAsync(args.Socket);
        }

        /// <summary>
        /// Process an incomming rquest.
        /// </summary>
        /// <param name="socket">the socket associated with the request</param>
        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                using (var inputStream = socket.InputStream.AsStreamForRead())
                using (var outputStream = socket.OutputStream.AsStreamForWrite())
                {
                    var request = await HttpRequest.ParseFromStream(inputStream);   //Read the http request

                    var response = HttpResponse.CreateDefaultHttpResponse(request); //Create the default response object for this request

                    if(request.IsValid)
                        await HandleRequest(request, response);     //call the abstract method if the request is valid
                    
                    await response.Send(outputStream);
                }
            }
            catch (Exception)
            {
                if (!IgnoreExceptions)  //Only throw exceptions if they are enabled
                    throw;
            }
            finally
            {
                socket.Dispose();   //This call is important: This closes the connection to the client.
            }
        }

#else
		void AcceptConnections ()
		{
			while (true) {
				try {
					var tc = listener.AcceptTcpClient ();
					taskq.PushTask (() => ServeHTTP (tc));
				} catch (SocketException) {
					break;
				}
			}
		}

		void ServeHTTP (TcpClient tc)
		{
			NetworkStream inputStream = tc.GetStream ();

			HttpRequest request = HttpRequest.ParseFromStream(inputStream);   //Read the http request

			lock (mainThreadRequests) {
				mainThreadRequests.Enqueue (request);
			}

		}
#endif

#if UNITY_WSA && !UNITY_EDITOR
        /// <summary>
        /// Abstract method that gets called for every incomming request.
        /// Override this method with the specific behavior of the http server.
        /// This method should be implemented asynchronously.
        /// </summary>
        /// <param name="request">the request object for this request</param>
        /// <param name="response">the response object for this request</param>
        public abstract Task HandleRequest(HttpRequest request, HttpResponse response);
#else
		public abstract void HandleRequest(HttpRequest request, HttpResponse response);

#endif




        public void Dispose()
        {
#if UNITY_WSA && !UNITY_EDITOR
            if (socketListener != null)
            {
                socketListener.ConnectionReceived -= OnConnectionReceived;
                socketListener.Dispose();
                socketListener = null;
            }
#else
			if (taskq != null)
				taskq.Dispose ();
			if (listener != null)
				listener.Stop ();
			mainThreadRequests.Clear ();
			taskq = null;
			listener = null;
#endif
        }
    }
}
