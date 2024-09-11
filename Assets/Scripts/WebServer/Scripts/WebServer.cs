using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
namespace WebServer
{

    public class WebServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    public List<RequestBase> Requests = new List<RequestBase>();

    public UnityEvent<HttpListenerContext, string> OnPost = new UnityEvent<HttpListenerContext, string>();
    public UnityEvent<HttpListenerContext, string> OnPut = new UnityEvent<HttpListenerContext, string>();
    public UnityEvent<HttpListenerContext, string> OnDelete = new UnityEvent<HttpListenerContext, string>();
    public UnityEvent<HttpListenerContext, string> OnGet = new UnityEvent<HttpListenerContext, string>();

    public int port = 197;
        public bool isRunning = false;
    public bool runOnStart = true;
    //void OnEnable()
    //{
    //    if (runOnStart && !isRunning)
    //    {
    //            RunStart();
    //    }
    //}
        void Start()
        {
            if (runOnStart && !isRunning)
            {
                RunStart();
            }
        }
        void RunStart()
    {
        if (runOnStart && !isRunning)
        {
            StartServer();
        }
    }
    public RequestBase GetRequest(HttpListenerContext context)
        {
            var rawRoute = context.Request.Url.AbsolutePath;
            string route = rawRoute.Length > 1 ? rawRoute.Substring(1) : rawRoute;

            foreach (var request in Requests)
            {
            if (request.route == route)
                {

                return request;
            }
        }
        return null;
    }
        bool responding = false;
    void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));  // Set your desired port
        listener.Start();

        listenerThread = new Thread(() =>
        {
            while (isRunning)
            {
                try
                {
                    var context = listener.GetContext();
                    HandleRequest(context);
                }
                catch (Exception e)
                {
                    Debug.LogError("Server Exception: " + e.Message);
                }
            }
        });

        listenerThread.Start();
            isRunning = true;
        Debug.Log(string.Format("Server started on http://localhost:{0}/", port));
    }
    public static void SendStream(HttpListenerContext context, byte[] buffer)
    {
        var request = context.Request;
        var response = context.Response;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
    public string GetQuery(HttpListenerRequest request)
    {
        var query = request.Url.Query;
        if (query.Length == 0)
        {
            return null;
        }
        return query.Substring(1);
    }
    public string GetRequestBody(HttpListenerRequest request)
    {
        var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return reader.ReadToEnd();
    }
    private void HandleRequest(HttpListenerContext context)
    {
            if (responding)
            {
                return;
            }
            responding = true;
       
        var request = context.Request;
        var response = context.Response;
            var requestBase = GetRequest(context);
            if(requestBase == null)
            {
                var rawRoute = context.Request.Url.AbsolutePath;

                string route = rawRoute.Length > 1 ? rawRoute.Substring(1) : rawRoute;
                var routeData = RequestBase.GetRouteData(route);
                //Debug.Log("Route not found");
                responding = false;
                SendStream(context, Encoding.UTF8.GetBytes(routeData));
                return;
                //return;
            }
            else
            {
                requestBase.Run(context);

            }
            responding = false;



            //    switch (request.HttpMethod)
            //{
            //    case "GET":
            //        OnGet.Invoke(context, GetQuery(request));
            //        break;
            //    case "POST":
            //        OnPost.Invoke(context, GetRequestBody(request));
            //        break;
            //    case "PUT":
            //        OnPut.Invoke(context, GetRequestBody(request));
            //        break;
            //    case "DELETE":
            //        OnDelete.Invoke(context, GetRequestBody(request));
            //        break;
            //        default:
            //        Debug.Log("Unsupported HTTP method");
            //            break;
            //}
        }
        private void OnDisable()
    {
        if (isRunning)
        {
            StopServer();
        }
    }
    void OnApplicationQuit()
    {
        if (isRunning)
        {
            StopServer();
        }
    }

    void StopServer()
    {
        isRunning = false;
        listener.Stop();
        listenerThread.Abort();
        Debug.Log("Server stopped.");
    }
}
}
