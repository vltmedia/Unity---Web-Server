
using System.Net;
using UnityEngine;
namespace WebServer
{


    [CreateAssetMenu(fileName = "Request_Get_", menuName = "WebServer/Request: Get", order = 1)]

public class RequestGet : RequestBase
{
    public override void Run(HttpListenerContext context)
    {
        this.context = context;
        base.Run(context);
            SendReponse(routeData);
    }

}
}