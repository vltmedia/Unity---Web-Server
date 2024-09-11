using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Networking.Sockets;
#endif

namespace UnityWebServer
{
    enum RequestParserState
    {
        Method,
        Url,
        Urlparm,
        Urlvalue,
        Version,
        Headerkey,
        Headervalue,
        Body,
        Ok
    }

    /// <summary>
    /// Class representing a http request.
    /// Code from http://www.codeproject.com/Articles/17071/Sample-HTTP-Server-Skeleton-in-C,
    /// with some modifications.
    /// </summary>
    public class HttpRequest
    {
        private const uint BufferSize = 8192;

        public string Method { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public MyDictionary<string, string> Args { get; set; } 
        public MyDictionary<string, string> Headers { get; set; }
        public int BodySize { get; set; }
        public byte[] BodyData { get; set; }
		public string BodyText {
			get {
				return System.Text.Encoding.UTF8.GetString(BodyData);
			}
			set {
				BodyData = Encoding.UTF8.GetBytes (value);
			}
		}

        public bool IsValid { get; set; }

		public Stream stream { get; set; }

#if UNITY_WSA && !UNITY_EDITOR
        /// <summary>
        /// Reades and creates a http request object from the given stream.
        /// </summary>
        /// <param name="inputStream">the stream to read the http request from</param>
        /// <returns></returns>
        public static async Task<HttpRequest> ParseFromStream(Stream inputStream)
#else
		public static HttpRequest ParseFromStream(Stream inputStream)
#endif
        {
            Debug.Assert(inputStream != null);

            byte[] myReadBuffer = new byte[BufferSize];
            RequestParserState parserState = RequestParserState.Method;
            HttpRequest httpRequest = new HttpRequest();
            httpRequest.Args = new MyDictionary<string, string>();
            httpRequest.Headers = new MyDictionary<string, string>();
			httpRequest.stream = inputStream;


            String myCompleteMessage = "";
            int numberOfBytesRead = 0;

            string hValue = "";
            string hKey = "";


            // binary data buffer index
            int bfndx = 0;

            // Incoming message may be larger than the buffer size.
            do
            {
#if UNITY_WSA && !UNITY_EDITOR
                numberOfBytesRead = await inputStream.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);
#else
				numberOfBytesRead =  inputStream.Read(myReadBuffer, 0, myReadBuffer.Length);
#endif
                myCompleteMessage =
                    String.Concat(myCompleteMessage, Encoding.UTF8.GetString(myReadBuffer.ToArray(), 0, numberOfBytesRead));

                // read buffer index
                int ndx = 0;
                do
                {
                    switch (parserState)
                    {
                        case RequestParserState.Method:
                            if (myReadBuffer[ndx] != ' ')
                                httpRequest.Method += (char)myReadBuffer[ndx++];
                            else
                            {
                                ndx++;
                                parserState = RequestParserState.Url;
                            }
                            break;
                        case RequestParserState.Url:
                            if (myReadBuffer[ndx] == '?')
                            {
                                ndx++;
                                hKey = "";
                                parserState = RequestParserState.Urlparm;
                            }
                            else if (myReadBuffer[ndx] != ' ')
                                httpRequest.Url += (char)myReadBuffer[ndx++];
                            else
                            {
                                ndx++;
                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RequestParserState.Version;
                            }
                            break;
                        case RequestParserState.Urlparm:
                            if (myReadBuffer[ndx] == '=')
                            {
                                ndx++;
                                hValue = "";
                                parserState = RequestParserState.Urlvalue;
                            }
                            else if (myReadBuffer[ndx] == ' ')
                            {
                                ndx++;

                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RequestParserState.Version;
                            }
                            else
                            {
                                hKey += (char)myReadBuffer[ndx++];
                            }
                            break;
                        case RequestParserState.Urlvalue:
                            if (myReadBuffer[ndx] == '&')
                            {
                                ndx++;
                                hKey = HttpUtility.UrlDecode(hKey);
                                hValue = HttpUtility.UrlDecode(hValue);
                                httpRequest.Args[hKey] = httpRequest.Args[hKey] != null ? httpRequest.Args[hKey] + ", " + hValue : hValue;
                                hKey = "";
                                parserState = RequestParserState.Urlparm;
                            }
                            else if (myReadBuffer[ndx] == ' ')
                            {
                                ndx++;
                                hKey = HttpUtility.UrlDecode(hKey);
                                hValue = HttpUtility.UrlDecode(hValue);
                                httpRequest.Args[hKey] = httpRequest.Args[hKey] != null ? httpRequest.Args[hKey] + ", " + hValue : hValue;

                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RequestParserState.Version;
                            }
                            else
                            {
                                hValue += (char)myReadBuffer[ndx++];
                            }
                            break;
                        case RequestParserState.Version:
                            if (myReadBuffer[ndx] == '\r')
                                ndx++;
                            else if (myReadBuffer[ndx] != '\n')
                                httpRequest.Version += (char)myReadBuffer[ndx++];
                            else
                            {
                                ndx++;
                                hKey = "";
                                parserState = RequestParserState.Headerkey;
                            }
                            break;
                        case RequestParserState.Headerkey:
                            if (myReadBuffer[ndx] == '\r')
                                ndx++;
                            else if (myReadBuffer[ndx] == '\n')
                            {
                                ndx++;
                                if (httpRequest.Headers["Content-Length"] != null)
                                {
                                    httpRequest.BodySize = Convert.ToInt32(httpRequest.Headers["Content-Length"]);
                                    httpRequest.BodyData = new byte[httpRequest.BodySize];
									if (httpRequest.BodySize == 0) {
										parserState = RequestParserState.Ok;
									} else {
	                                    parserState = RequestParserState.Body;
									}
                                }
                                else
                                    parserState = RequestParserState.Ok;

                            }
                            else if (myReadBuffer[ndx] == ':')
                                ndx++;
                            else if (myReadBuffer[ndx] != ' ')
                                hKey += (char)myReadBuffer[ndx++];
                            else
                            {
                                ndx++;
                                hValue = "";
                                parserState = RequestParserState.Headervalue;
                            }
                            break;
                        case RequestParserState.Headervalue:
                            if (myReadBuffer[ndx] == '\r')
                                ndx++;
                            else if (myReadBuffer[ndx] != '\n')
                                hValue += (char)myReadBuffer[ndx++];
                            else
                            {
                                ndx++;
                                httpRequest.Headers.Add(hKey, hValue);
                                hKey = "";
                                parserState = RequestParserState.Headerkey;
                            }
                            break;
                        case RequestParserState.Body:
                            // Append to request BodyData
                            Array.Copy(myReadBuffer, ndx, httpRequest.BodyData, bfndx, numberOfBytesRead - ndx);
                            bfndx += numberOfBytesRead - ndx;
                            ndx = numberOfBytesRead;
                            if (httpRequest.BodySize <= bfndx)
                            {
                                parserState = RequestParserState.Ok;
                            }
                            break;
                            //default:
                            //	ndx++;
                            //	break;

                    }
                }
                while (ndx < numberOfBytesRead);

            }
			while ((numberOfBytesRead == BufferSize) || (parserState == RequestParserState.Body));

            if(httpRequest.BodyData == null)
                httpRequest.BodyData = new byte[0];

            if (parserState == RequestParserState.Ok)
                httpRequest.IsValid = true;

            return httpRequest;
        }
	
		public void Close ()
		{
			if (stream != null) {
				stream.Close();
			}
		}
	}
}