using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlobProcessSample
{
    public static class WebhookSubscriptionFunction
    {
        /* This function will act as an event subscription endpoint for your event subscription. The function implements the validation handshake and returns a Bad Request response
            for all other incoming events. 
        */

        [FunctionName("WebhookSubscriptionFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function begun");
            string response = string.Empty;
            const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";

            string requestContent = await req.Content.ReadAsStringAsync();
            log.LogInformation($"Received events: {requestContent}");
            EventGridEvent[] eventGridEvents = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                JObject dataObject = eventGridEvent.Data as JObject;

                // Deserialize the event data into the appropriate type based on event type
                if (string.Equals(eventGridEvent.EventType, SubscriptionValidationEvent, StringComparison.OrdinalIgnoreCase))
                {
                    var eventData = dataObject.ToObject<SubscriptionValidationEventData>();
                    log.LogInformation($"Got SubscriptionValidation event data, validationCode: {eventData.ValidationCode},  validationUrl: {eventData.ValidationUrl}, topic: {eventGridEvent.Topic}");
                    // Do any additional validation (as required) such as validating that the Azure resource ID of the topic matches
                    // the expected topic and then return back the below response
                    var responseData = new SubscriptionValidationResponse();
                    responseData.ValidationResponse = eventData.ValidationCode;
                    return req.CreateResponse(HttpStatusCode.OK, responseData);
                }
            }

            // Responding back with a 400 Bad Request is intentional and only for the purpose of demonstrating dead lettering.
            return req.CreateResponse(HttpStatusCode.BadRequest, response);
        }
    }
}
