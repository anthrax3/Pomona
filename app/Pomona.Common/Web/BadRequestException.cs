﻿#region License

// Pomona is open source software released under the terms of the LICENSE specified in the
// project's repository, or alternatively at http://pomona.io/

#endregion

using System;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Pomona.Common.Web
{
    [Serializable]
    public class BadRequestException<TBody> : BadRequestException, IWebClientException<TBody>
    {
        public BadRequestException(HttpRequestMessage request,
                                   HttpResponseMessage response,
                                   object body,
                                   Exception innerException)
            : base(request, response, body, innerException)
        {
        }


        protected BadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        public new TBody Body
        {
            get { return (TBody)base.Body; }
        }
    }

    [Serializable]
    public class BadRequestException : WebClientException
    {
        public BadRequestException(HttpRequestMessage request,
                                   HttpResponseMessage response,
                                   object body,
                                   Exception innerException)
            : base(request, response, body, innerException)
        {
        }


        protected BadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}