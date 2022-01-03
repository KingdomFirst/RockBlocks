<!--
// <copyright>
// Copyright 2021 by Kingdom First Solutions
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
<%@ WebHandler Language="C#" Class="RockWeb.Plugins.rocks_kfs.Zoom.ZoomRoomWebhook" %>
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Net;
using Rock;
using Rock.Data;
using Newtonsoft.Json;
using rocks.kfs.Zoom;
using rocks.kfs.Zoom.Model;

namespace RockWeb.Plugins.rocks_kfs.Zoom
{
    /// <summary>
    /// Handles retrieving Zoom meeting data from api
    /// </summary>
    public class ZoomRoomWebhook : IHttpHandler
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

            // Check and ensure payload is in JSON format. 
            if ( !( ( postedData.StartsWith( "{" ) && postedData.EndsWith( "}" ) )
                || ( postedData.StartsWith( "[" ) && postedData.EndsWith( "]" ) ) ) )
            {
                var payloadCollection = HttpUtility.ParseQueryString( postedData );
                postedData = JsonConvert.SerializeObject( payloadCollection.AllKeys.ToDictionary( y => y, y => payloadCollection[y] ) );
            }

            var zoomRoomData = JsonConvert.DeserializeObject<ScheduleMeetingWebhookResponse>( postedData );
            if ( zoomRoomData == null )
            {
                response.Write( "Invalid Data." );
                response.StatusCode = HttpStatusCode.BadRequest.ConvertToInt();
                return;
            }

            int roomOccurrenceId = request.QueryString["token"].AsInteger();
            var meetingId = zoomRoomData.meeting_number;
            var zrOccurrenceService = new RoomOccurrenceService( rockContext );
            var roomOccurrence = zrOccurrenceService.Queryable().FirstOrDefault( ro => ro.Id == roomOccurrenceId );

            if ( roomOccurrence != null )
            {
                var meetingIdLong = long.Parse( meetingId );
                roomOccurrence.ZoomMeetingId = meetingIdLong;
                try
                {
                    var meeting = rocks.kfs.Zoom.Zoom.Api().GetZoomMeeting( meetingIdLong );
                    roomOccurrence.ZoomMeetingJoinUrl = meeting.Join_Url;
                }
                catch { }
            }
            rockContext.SaveChanges();
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