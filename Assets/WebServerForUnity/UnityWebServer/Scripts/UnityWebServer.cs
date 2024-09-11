using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityWebServer;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
#endif

namespace UnityWebServer {


    public class UnityWebServer : MonoBehaviour {

        class UnityHttpServer : HttpServer {

            public string DocumentRoot;
            public Dictionary<string, string> mimeMmappings;
            public bool EnableCORS;

            public UnityHttpServer(int port, bool EnableCORS, string path = "www") : base(port) {
                DocumentRoot = Application.streamingAssetsPath + "/" + path;
                mimeMmappings = LoadMimeMappings();
            }

            Dictionary<string, string> LoadMimeMappings() {
                Dictionary<string, string> mappings = new Dictionary<string, string>();
                mappings.Add(".js", "application/x-javascript");
                mappings.Add(".css", "text/css");
                mappings.Add(".png", "image/png");
                mappings.Add(".gif", "image/gif");
                mappings.Add(".jpeg", "image/jpeg");
                mappings.Add(".jpg", "image/jpeg");
                mappings.Add(".tif", "image/tiff");
                mappings.Add(".tiff", "image/tiff");
                return mappings;
            }

#if UNITY_WSA && !UNITY_EDITOR
        public override async Task HandleRequest(HttpRequest request, HttpResponse response) {
            // Call back onto the Unity main thread
            UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                HandleHttpRequest(request, response);
            }, true);
        }
#else
            public override void HandleRequest(HttpRequest request, HttpResponse response) {
                HandleHttpRequest(request, response);
            }
#endif

            public void HandleHttpRequest(HttpRequest request, HttpResponse response) {

                // handle OPTIONS requests
                if (request.Method == "OPTIONS") {

                    response.StatusCode = HttpStatusCode.Ok;

                } else {

                    bool handled = DispatchRequestHandlerForRoute(request.Url, request.Method, request, response);

                    // if not handled, we'll try and load it from streaming assets
                    if (!handled) {
                        string requestPath = request.Url;
                        string path = Application.streamingAssetsPath + "/www" + requestPath;
                        if (File.Exists(path)) {

                            // load content 
                            byte[] bytes = File.ReadAllBytes(path);
                            response.BodyData = bytes;
                            response.ContentLength = bytes.Length;
                            response.StatusCode = HttpStatusCode.Ok;

                            // set content type
                            string extension = Path.GetExtension(path).ToString().ToLower();
                            if (mimeMmappings.ContainsKey(extension)) {
                                response.ContentType = mimeMmappings[extension];
                            } else {
                                response.ContentType = "application/octet-stream";
                            }
                        }
                    }
                }

                // CORS
                if (EnableCORS) {
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PATCH, PUT, DELETE, OPTIONS");
                    response.Headers.Add("Access-Control-Allow-Headers", "Origin, Content-Type, X-Auth-Token");
                }
            }


            bool DispatchRequestHandlerForRoute(string route, string verb, HttpRequest request, HttpResponse response) {

                // strip off any trailing /'s
                if (route.EndsWith("/")) {
                    route = route.Remove(route.Length - 1);
                }

                // search for a handler on all game objects in the scene
                MonoBehaviour[] sceneActive = FindObjectsOfType<MonoBehaviour>();
                foreach (MonoBehaviour mono in sceneActive) {
                    Type type = mono.GetType();
#if UNITY_WSA && !UNITY_EDITOR
                if (type.GetTypeInfo().GetCustomAttributes(typeof(UnityHttpServerAttribute), true).ToArray().Length > 0) {
#else
                    if (type.GetCustomAttributes(typeof(UnityHttpServerAttribute), true).Length > 0) {
#endif
                        foreach (MethodInfo method in type.GetMethods()) {
                            object[] custom_attributes = method.GetCustomAttributes(typeof(UnityHttpRouteAttribute), false).ToArray();
                            if (custom_attributes.Length > 0) {
                                UnityHttpRouteAttribute attribute = custom_attributes[0] as UnityHttpRouteAttribute;

                                string attrRoute = attribute.Route;
                                bool isWildcard = false;
                                // Detect wildcard routes (which have a * )
                                int wildcardIndex = attrRoute.IndexOf("*");
                                if (wildcardIndex >= 0) {
                                    // strip off the route up to the first * found
                                    attrRoute = attrRoute.Substring(0, wildcardIndex);
                                    isWildcard = true;
                                }
                                // if the new route ends in / remove that too for better matching
                                if (attrRoute.EndsWith("/")) {
                                    attrRoute = attrRoute.Remove(attrRoute.Length - 1);
                                }

                                if ((isWildcard && route.ToUpper().StartsWith(attrRoute) && attribute.Verb == verb)
                                    ||
                                    (attrRoute == route.ToUpper() && attribute.Verb == verb)) {
                                    if (method.GetParameters().Length == 2) {
                                        method.Invoke(mono, new object[] { request, response });
                                        return true;
                                    } else if (method.GetParameters().Length == 3) {

                                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                                        if (isWildcard) {
                                            parameters["*"] = "";
                                            if (wildcardIndex < route.Length) {
                                                parameters["*"] = route.Substring(wildcardIndex);
                                            }
                                        }
                                        method.Invoke(mono, new object[] { request, response, parameters });
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                return false;
            }
        }


        public int Port = 8000;
        public bool EnableCORS = true;
        public string StreamingAssetsRoot = "www";

        UnityHttpServer httpServer;

        void Start() {

            httpServer = new UnityHttpServer(Port, EnableCORS, StreamingAssetsRoot);
            httpServer.Start();
        }

        void Update() {
            httpServer.Update();
        }
    }

}