using UnityEngine;
using UnityWebServer;
using System.Text;

[UnityHttpServer]
public class ExampleRoute : MonoBehaviour {

    [UnityHttpRoute("/example")]
    public void RouteIndex(HttpRequest request, HttpResponse response) {
        response.BodyText = "<html><body>Hello From Example!</body></html>";
    }
}
