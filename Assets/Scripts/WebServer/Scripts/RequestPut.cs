

using System.Net;
using UnityEngine;
namespace WebServer
{



    [CreateAssetMenu(fileName = "Request_Put_", menuName = "WebServer/Request: Put", order = 3)]

    public class RequestPut: RequestBase {
        public string outputData = "";

        public override void Run(HttpListenerContext context)
        {
            this.context = context;
            base.Run(context);
            SendReponse(outputData);
        }
       
    }
    }