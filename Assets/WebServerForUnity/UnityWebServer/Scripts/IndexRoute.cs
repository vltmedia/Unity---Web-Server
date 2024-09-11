using UnityEngine;
using UnityWebServer;

[UnityHttpServer]
public class IndexRoute : MonoBehaviour {

    [UnityHttpRoute("/")]
    public void RouteIndex(HttpRequest request, HttpResponse response) {
        response.BodyText = "<html><body>Hello From Unity!</body></html>";
    }
}
