

using System.Net;
using UnityEngine;
using WebServer;

[CreateAssetMenu(fileName = "Request_Post_", menuName = "WebServer/Request: Post", order = 2)]

public class RequestPost : RequestBase
{



    public override void Run(HttpListenerContext context)
    {
        this.context = context;
        base.Run(context);
    }

}