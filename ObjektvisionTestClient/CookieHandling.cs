using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace ObjektvisionTestClient
{

    public class CookieBehavior : IEndpointBehavior
    {
        private CookieContainer cookieCont;

        public CookieBehavior(CookieContainer cookieCont)
        {
            this.cookieCont = cookieCont;
        }

        public void AddBindingParameters(ServiceEndpoint serviceEndpoint,
            System.ServiceModel.Channels
            .BindingParameterCollection bindingParameters)
        { }

        public void ApplyClientBehavior(ServiceEndpoint serviceEndpoint,
            System.ServiceModel.Dispatcher.ClientRuntime behavior)
        {
            behavior.MessageInspectors.Add(new CookieMessageInspector(cookieCont));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint,
            System.ServiceModel.Dispatcher
            .EndpointDispatcher endpointDispatcher)
        { }

        public void Validate(ServiceEndpoint serviceEndpoint) { }
    }

    public class CookieMessageInspector : IClientMessageInspector
    {
        private CookieContainer cookieCont;

        public CookieMessageInspector(CookieContainer cookieCont)
        {
            this.cookieCont = cookieCont;
        }

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply,
            object correlationState)
        {
            object obj;
            if (reply.Properties.TryGetValue(HttpResponseMessageProperty.Name, out obj))
            {
                HttpResponseMessageProperty httpResponseMsg = obj as HttpResponseMessageProperty;
                if (!string.IsNullOrEmpty(httpResponseMsg.Headers["Set-Cookie"]))
                {
                    cookieCont.SetCookies((Uri)correlationState, httpResponseMsg.Headers["Set-Cookie"]);
                }
            }
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request,
            System.ServiceModel.IClientChannel channel)
        {
            object obj;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out obj))
            {
                HttpRequestMessageProperty httpRequestMsg = obj as HttpRequestMessageProperty;
                SetRequestCookies(channel, httpRequestMsg);
            }
            else
            {
                var httpRequestMsg = new HttpRequestMessageProperty();
                SetRequestCookies(channel, httpRequestMsg);
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMsg);
            }

            return channel.RemoteAddress.Uri;
        }

        private void SetRequestCookies(System.ServiceModel.IClientChannel channel, HttpRequestMessageProperty httpRequestMessage)
        {
            httpRequestMessage.Headers["Cookie"] = cookieCont.GetCookieHeader(channel.RemoteAddress.Uri);
        }
    }
}
