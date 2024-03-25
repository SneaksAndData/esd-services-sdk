using Azure.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

namespace Snd.Sdk.Tests.CustomMocks
{
    public class MockAzureResponse : Azure.Response
    {
        private readonly int status;

        public MockAzureResponse() { }
        public MockAzureResponse(int status)
        {
            this.status = status;
        }
        public override int Status => this.status;

        public override string ReasonPhrase => throw new NotImplementedException();

        public override Stream ContentStream { get; set; }
        public override string ClientRequestId { get; set; }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        protected override bool ContainsHeader(string name)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string value)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }

        public override bool IsError => this.status > 200;
    }
}
