﻿using System.Web;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return null;
            return GetHandlerForPathParts(pathParts);
        }

        private IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = string.Intern(pathParts[0].ToLower());
            if (pathParts.Length == 1)
            {
                if (pathController == "metadata")
                    return new IndexMetadataHandler();

                return null;
            }

            var pathAction = string.Intern(pathParts[1].ToLower());
            if (pathAction == "wsdl")
            {
                if (pathController == "soap11")
                    return new Soap11WsdlMetadataHandler();
                if (pathController == "soap12")
                    return new Soap12WsdlMetadataHandler();
            }

            if (pathAction != "metadata") return null;

            switch (pathController)
            {
                case "json":
                    return new JsonMetadataHandler();

                case "xml":
                    return new XmlMetadataHandler();

                case "jsv":
                    return new JsvMetadataHandler();

                case "soap11":
                    return new Soap11MetadataHandler();

                case "soap12":
                    return new Soap12MetadataHandler();

                case "types":
                    
                    if (HostContext.Config == null
                        || HostContext.Config.MetadataTypesConfig == null)
                        return null;

                    if (HostContext.Config.MetadataTypesConfig.BaseUrl == null)
                        HostContext.Config.MetadataTypesConfig.BaseUrl = HttpHandlerFactory.GetBaseUrl();

                    return new MetadataTypesHandler { Config = HostContext.Config.MetadataTypesConfig };

                case "operations":
                    
                    return new CustomResponseHandler((httpReq, httpRes) => 
                        HostContext.HasAccessToMetadata(httpReq, httpRes) 
                            ? HostContext.Metadata.GetOperationDtos()
                            : null, "Operations");

                default:
                    string contentType;
                    if (HostContext.ContentTypes
                        .ContentTypeFormats.TryGetValue(pathController, out contentType))
                    {
                        var format = ContentFormat.GetContentFormat(contentType);
                        return new CustomMetadataHandler(contentType, format);
                    }
                    break;
            }
            return null;
        }
    }
}