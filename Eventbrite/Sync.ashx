<!--
// <copyright>
// Copyright 2020 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
-->
<%@ WebHandler Language="C#" Class="RockWeb.Plugins.rocks_kfs.Eventbrite.Sync" %>
using System.IO;
using System.Web;
using System.Net;
using Rock;
using Rock.Data;
using Newtonsoft.Json;
using rocks.kfs.Eventbrite.Entities;
using rocks.kfs.Eventbrite.Utility.ExtensionMethods;

namespace RockWeb.Plugins.rocks_kfs.Eventbrite
{
    /// <summary>
    /// Handles retrieving file data from storage
    /// </summary>
    public class Sync : IHttpHandler
    {

        private HttpRequest request;
        private HttpResponse response;


        public void ProcessRequest( HttpContext context )
        {
            request = context.Request;
            response = context.Response;

            RockContext rockContext = new RockContext();

            response.ContentType = "text/plain";

            if ( request.HttpMethod != "POST" )
            {
                response.Write( "Invalid request type." );
                response.StatusCode = HttpStatusCode.NotAcceptable.ConvertToInt();
                return;
            }

            string postedData = string.Empty;
            using ( var reader = new StreamReader( request.InputStream ) )
            {
                postedData = reader.ReadToEnd();
            }

            var eventbriteData = JsonConvert.DeserializeObject<WebhookResponse>( postedData );
            if ( eventbriteData == null )
            {
                response.Write( "Invalid Data." );
                response.StatusCode = HttpStatusCode.BadRequest.ConvertToInt();
                return;
            }

            switch ( eventbriteData.Config.Action )
            {
                case "attendee.updated":
                    var splitUrl = eventbriteData.Api_Url.SplitDelimitedValues( "/" );
                    // api_url example: https://www.eventbriteapi.com/v3/events/113027799190/attendees/1955015294/
                    // 6th element in array should be event id as long.
                    if ( eventbriteData.Api_Url.IsNotNullOrWhiteSpace() )
                    {
                        //var tasks = new[]
                        //{
                        //    System.Threading.Tasks.Task.Run(() => rocks.kfs.Eventbrite.Eventbrite.SyncAttendee( eventbriteData.Api_Url ))
                        //};
                        rocks.kfs.Eventbrite.Eventbrite.SyncAttendee( eventbriteData.Api_Url );
                        response.Write( string.Format( "SyncAttendee Ran ({0})", eventbriteData.Api_Url ) );
                    }
                    else
                    {
                        response.Write( "Invalid data received." );
                        response.StatusCode = HttpStatusCode.BadRequest.ConvertToInt();
                    }
                    if ( splitUrl.Length > 4 && splitUrl[4] == "events" && splitUrl.Length > 5 )
                    {
                        var ebEventId = splitUrl[5].AsLongOrNull();
                        if ( ebEventId.HasValue )
                        {
                            var group = rocks.kfs.Eventbrite.Eventbrite.GetGroupByEBEventId( ebEventId.Value );
                            if ( group != null )
                            {
                                var tasks = new[]
                                {
                                    System.Threading.Tasks.Task.Run(() => rocks.kfs.Eventbrite.Eventbrite.SyncEvent( group.Id, ThrottleSync: true ))
                                };
                            }
                            response.Write( string.Format( "Eventbrite sync started for group {0}", group.Name ) );
                        }
                        else
                        {
                            response.Write( string.Format( "Cannot find linked group for event id: {0}", ebEventId.ToString() ) );
                        }
                    }
                    break;
                case "order.updated":
                case "order.placed":
                case "order.refunded":
                    // do something with orders...
                    break;
                case "event.created":
                case "event.published":
                case "event.unpublished":
                case "event.updated":
                    // do something with event updates
                    break;
                case "ticket_class.updated":
                    break;
                default:
                    // skip it
                    break;
            }

        }

        /// <summary>
        /// Sends a 403 (forbidden)
        /// </summary>
        /// <param name="context">The context.</param>
        private void SendNotAuthorized( HttpContext context )
        {
            context.Response.StatusCode = System.Net.HttpStatusCode.Forbidden.ConvertToInt();
            context.Response.StatusDescription = "Not authorized to view file";
            context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.</returns>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}