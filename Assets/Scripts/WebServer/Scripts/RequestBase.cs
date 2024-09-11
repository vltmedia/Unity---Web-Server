using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace WebServer
{
    public class RequestBase: ScriptableObject
    {
        public RequestType requestType = RequestType.GET;
        public HttpListenerContext context;
        public HttpListenerRequest request { get { return context.Request; } }
        public HttpListenerResponse response { get { return context.Response; } }
        public UnityEvent<RequestBase> onRequest = new UnityEvent<RequestBase>();
        public string HttpMethod { get { return requestType.ToString(); } }
        public string body { get {
                return ParseBody();
            } }
        public string query { get
            {
                return request.Url.Query;
            } }

        public string route;
        public string routePath
        {
            get
            {
                return Path.Combine(Application.streamingAssetsPath+"/www",  route);
            }
        }
        public static string FourOFour
        {
            get
            {
                  return File.ReadAllText(Path.Combine(Application.streamingAssetsPath + "/www", "404.html"));
            }
        }
        public string routeData
        {
            get
            {
                if(File.Exists(routePath))
                {
                    return File.ReadAllText(routePath);
                }
                else
                {
                    return FourOFour;
                }
            }
        }

        public static string GetRouteData(string route)
        {
            var path = Path.Combine(Application.streamingAssetsPath + "/www", route);
            if (File.Exists(path))
            {
                  return File.ReadAllText(path);
            }
            else
            {
                return FourOFour;
            }
        }
        public virtual void SendReponse(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            WebServer.SendStream(context, buffer);

        }

        public virtual void PreProcess()
        {

        }

        public virtual void Run(HttpListenerContext context)
        {
            this.context = context;
            PreProcess();
            onRequest.Invoke(this);

        }
        public string ParseBody()
        {
            var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return reader.ReadToEnd();
        }
    }




    [System.Serializable]
    public enum RequestType
    {
        GET,
        POST,
        PUT,
        DELETE
    }
}
