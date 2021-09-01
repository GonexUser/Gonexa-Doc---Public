using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace APIExaDoc
{
    public class TelemetryHeadersInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public List<string> RequestHeaders { get; set; }
        public List<string> ResponseHeaders { get; set; }

        public TelemetryHeadersInitializer(IHttpContextAccessor httpContextAccessor)
        {
            RequestHeaders = new List<string> { "Referer" }; //whatever you need
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            var requestTelemetry = telemetry as RequestTelemetry;
            // Is this a TrackRequest() ?
            if (requestTelemetry == null) return;


            ISupportProperties propTelemetry = (ISupportProperties)telemetry;

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            foreach (var headerName in RequestHeaders)
            {
                var headers = context.Request.Headers[headerName];
                if (headers.Any())
                {
                    propTelemetry.Properties.Add($"{headerName}", string.Join(Environment.NewLine, headers));
                }
            }
        }
    }
}
