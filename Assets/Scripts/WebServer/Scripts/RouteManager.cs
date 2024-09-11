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
    public class RouteManager: MonoBehaviour
    {
        public List<RequestBase> Requests = new List<RequestBase>();
        public static RouteManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }


    }


}