﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Aliencube.AzureFunctions.Extensions.DependencyInjection.Abstractions;
using Aliencube.XslMapper.FunctionApp.Configurations;
using Aliencube.XslMapper.FunctionApp.Exceptions;
using Aliencube.XslMapper.FunctionApp.Extensions;
using Aliencube.XslMapper.FunctionApp.Helpers;
using Aliencube.XslMapper.FunctionApp.Models;

using Microsoft.Extensions.Logging;

using XslMapper.FunctionApp.Exceptions;

namespace Aliencube.XslMapper.FunctionApp.Functions
{
    /// <summary>
    /// This represents the function entity for the <see cref="XmlToXmlMapperHttpTrigger"/> class.
    /// </summary>
    public class XmlToXmlMapperFunction : FunctionBase<ILogger>, IXmlToXmlMapperFunction
    {
        private readonly AppSettings _settings;
        private readonly IXmlTransformHelper _helper;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlToXmlMapperFunction"/> class.
        /// </summary>
        /// <param name="settings"><see cref="AppSettings"/> instance.</param>
        /// <param name="helper"><see cref="IXmlTransformHelper"/> instance.</param>
        public XmlToXmlMapperFunction(AppSettings settings, IXmlTransformHelper helper)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public override async Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionsBase options = null)
        {
            //! Main de la función, pero primero se carga la clase "XmlToXmlMapperHttpTrigger" y sus dependecias.

            Log.LogInformation("C# HTTP trigger function processed a request.");
            var req = input as HttpRequestMessage;
            var request = (XmlToXmlMapperRequest)null;
            var response = (HttpResponseMessage)null;
            try
            {
                request = await req.Content.ReadAsAsync<XmlToXmlMapperRequest>()
                                   .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var statusCode = HttpStatusCode.BadRequest;
                var result = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                response = req.CreateResponse(HttpStatusCode.BadRequest, result);
                Log.LogError($"Request payload was invalid.");
                Log.LogError(ex.Message);
                Log.LogError(ex.StackTrace);
                return (TOutput)Convert.ChangeType(response, typeof(TOutput));
            }

            try
            {
                var content = await _helper
                    .LoadXslAsync(_settings.Containers.Mappers, request.Mapper.Directory, request.Mapper.Name) // Carga el Xslt
                    .AddArgumentsAsync(request.ExtensionObjects) // Carga DLL
                    .AddParam(request.InputXml) // Agrega el timbre desde %Temp%
                    .TransformAsync(request.InputXml)
                    .ToStringAsync(_settings.EncodeBase64Output);

                var result = new XmlToXmlMapperResponse() { Content = content };
                response = req.CreateResponse(HttpStatusCode.OK, result, _settings.JsonFormatter);
            }
            catch (CloudStorageNotFoundException ex)
            {
                var statusCode = HttpStatusCode.InternalServerError;
                var err = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                response = req.CreateResponse(statusCode, err);
            }
            catch (BlobContainerNotFoundException ex)
            {
                var statusCode = HttpStatusCode.BadRequest;
                var err = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                Log.LogError($"Request payload was invalid.");
                Log.LogError($"XSL mapper not found");
                response = req.CreateResponse(statusCode, err);
            }
            catch (BlobNotFoundException ex)
            {
                var statusCode = HttpStatusCode.BadRequest;
                var err = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                Log.LogError($"Request payload was invalid.");
                Log.LogError($"XSL mapper not found");
                response = req.CreateResponse(statusCode, err);
            }
            catch (NodeXmlNotFoundException ex)
            {
                var statusCode = HttpStatusCode.BadRequest;
                var err = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                Log.LogError($"Ted node must be 1 (one).");
                Log.LogError($"TED node not found");
                response = req.CreateResponse(statusCode, err);
            }
            catch (Exception ex)
            {
                var statusCode = HttpStatusCode.InternalServerError;
                var err = new ErrorResponse((int)statusCode, ex.Message, ex.StackTrace);
                response = req.CreateResponse(statusCode, err);
            }
            return (TOutput)Convert.ChangeType(response, typeof(TOutput));
        }
    }
}