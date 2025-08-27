using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Guidance.Dtos
{
    public class WebsocketMsg
    {
        public WebsocketType SocketType;
        public object Data;

        public WebsocketMsg(WebsocketType socketType, object data)
        {
            SocketType = socketType;
            Data = data;
        }
    }
}
