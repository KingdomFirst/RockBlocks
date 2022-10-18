<!--
// <copyright>
// Copyright 2022 by Kingdom First Solutions
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
<%@ WebHandler Language="C#" Class="RockWeb.Plugins.rocks_kfs.Webhooks.Edify" %>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using PostalServerDotNet.v1.Model.Webhook;
using Rock;
using Rock.Logging;
using Rock.Model;
using Rock.Workflow.Action;

namespace RockWeb.Plugins.rocks_kfs.Webhooks
{

    public class EdifyEvent : EventData
    {
        public string WorkflowActionGuid
        {
            get
            {
                var retval = "";
                if ( this.Payload != null && this.Payload.Message != null )
                {
                    var tag = this.Payload.Message.Tag;
                    if ( tag.IsNotNullOrWhiteSpace() && tag.Contains( "workflow_action_guid" ) )
                    {
                        var tagDict = tag.TrimEnd( '|' ).Split( '|' ).ToDictionary( item => item.Split( '=' )[0], item => item.Split( '=' )[1] );
                        retval = tagDict["workflow_action_guid"];
                    }
                }
                return retval;
            }
        }

        public string CommunicationRecipientGuid
        {
            get
            {
                var retval = "";
                if ( this.Payload != null && this.Payload.Message != null )
                {
                    var tag = this.Payload.Message.Tag;
                    if ( tag.IsNotNullOrWhiteSpace() && tag.Contains( "communication_recipient_guid" ) )
                    {
                        var tagDict = tag.TrimEnd( '|' ).Split( '|' ).ToDictionary( item => item.Split( '=' )[0], item => item.Split( '=' )[1] );
                        retval = tagDict["communication_recipient_guid"];
                    }
                }
                return retval;
            }
        }
    }

    /// <summary>
    /// Handles the responses from Edify
    /// </summary>
    public class Edify : IHttpAsyncHandler
    {
        public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback cb, Object extraData )
        {
            EdifyResponseAsync edifyAsync = new EdifyResponseAsync( cb, context, extraData );
            edifyAsync.StartAsyncWork();
            return edifyAsync;
        }

        public void EndProcessRequest( IAsyncResult result ) { }

        public void ProcessRequest( HttpContext context )
        {
            throw new InvalidOperationException();
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }

    internal class EdifyResponseAsync : IAsyncResult
    {
        private bool _completed;
        private readonly Object _state;
        private readonly AsyncCallback _callback;
        private readonly HttpContext _context;

        bool IAsyncResult.IsCompleted { get { return _completed; } }
        WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
        Object IAsyncResult.AsyncState { get { return _state; } }
        bool IAsyncResult.CompletedSynchronously { get { return false; } }

        private const bool ENABLE_LOGGING = false;

        public EdifyResponseAsync( AsyncCallback callback, HttpContext context, Object state )
        {
            _callback = callback;
            _context = context;
            _state = state;
            _completed = false;
        }

        public void StartAsyncWork()
        {
            ThreadPool.QueueUserWorkItem( ( workItemState ) =>
            {
                try
                {
                    StartAsyncTask( workItemState );
                }
                catch ( Exception ex )
                {
                    Rock.Model.ExceptionLogService.LogException( ex );
                    _context.Response.StatusCode = 500;
                    _completed = true;
                    _callback( this );
                }
            }, null );
        }

        private void StartAsyncTask( Object workItemState )
        {
            var request = _context.Request;
            var response = _context.Response;

            response.ContentType = "text/plain";

            if ( request.HttpMethod != "POST" )
            {
                response.Write( "Invalid request type." );
                response.StatusCode = ( int ) System.Net.HttpStatusCode.MethodNotAllowed;
                _completed = true;
                _callback( this );
                return;
            }

            if ( request.ContentType.Contains( "application/json" ) )
            {
                ProcessJsonContent( request, response );
            }
            else
            {
                response.Write( "Invalid content type." );
                response.StatusCode = ( int ) System.Net.HttpStatusCode.NotAcceptable;
            }

            _completed = true;
            _callback( this );
        }

        private void ProcessJsonContent( HttpRequest request, HttpResponse response )
        {
            string payload = string.Empty;

            using ( Stream s = request.InputStream )
            {
                using ( StreamReader readStream = new StreamReader( s, Encoding.UTF8 ) )
                {
                    payload = readStream.ReadToEnd();
                }
            }

            var edifyEvent = JsonConvert.DeserializeObject<EdifyEvent>( payload );

            if ( edifyEvent == null )
            {
                response.Write( "Invalid content type." );
                response.StatusCode = ( int ) System.Net.HttpStatusCode.NotAcceptable;
                return;
            }

            ProcessEdifyEvent( edifyEvent, new Rock.Data.RockContext() );
            //ProcessEdifyEventListAsync( eventList );

            response.StatusCode = ( int ) System.Net.HttpStatusCode.OK;
        }

        private void ProcessEdifyEventListAsync( List<EdifyEvent> edifyEvents )
        {
            var rockContext = new Rock.Data.RockContext();

            foreach ( var edifyEvent in edifyEvents )
            {
                ProcessEdifyEvent( edifyEvent, rockContext );
            }

        }

        private void ProcessEdifyEvent( EdifyEvent edifyEvent, Rock.Data.RockContext rockContext )
        {

            Guid? actionGuid = null;
            Guid? communicationRecipientGuid = null;

            if ( !string.IsNullOrWhiteSpace( edifyEvent.WorkflowActionGuid ) )
            {
                actionGuid = edifyEvent.WorkflowActionGuid.AsGuidOrNull();
            }

            if ( !string.IsNullOrWhiteSpace( edifyEvent.CommunicationRecipientGuid ) )
            {
                communicationRecipientGuid = edifyEvent.CommunicationRecipientGuid.AsGuidOrNull();
            }

            if ( actionGuid != null )
            {
                ProcessForWorkflow( actionGuid, rockContext, edifyEvent );
            }

            if ( communicationRecipientGuid != null )
            {
                ProcessForRecipient( communicationRecipientGuid, rockContext, edifyEvent );
            }
        }

        /// <summary>
        /// Processes for recipient.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="communicationRecipientGuid">The communication recipient unique identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        private void ProcessForRecipient( Guid? communicationRecipientGuid, Rock.Data.RockContext rockContext, EdifyEvent edifyEvent )
        {
            RockLogger.Log.Debug( RockLogDomains.Communications, "ProcessForRecipient {@payload}", edifyEvent );

            if ( !communicationRecipientGuid.HasValue )
            {
                return;
            }

            var communicationRecipient = new CommunicationRecipientService( rockContext ).Get( communicationRecipientGuid.Value );
            if ( communicationRecipient != null && communicationRecipient.Communication != null )
            {
                var communicationGuid = Rock.SystemGuid.InteractionChannel.COMMUNICATION.AsGuid();
                var interactionComponent = new InteractionComponentService( rockContext )
                    .GetComponentByEntityId( communicationGuid, communicationRecipient.CommunicationId, communicationRecipient.Communication.Subject );

                rockContext.SaveChanges();

                var interactionService = new InteractionService( rockContext );
                DateTime timeStamp = RockDateTime.ConvertLocalDateTimeToRockDateTime( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ).AddSeconds( edifyEvent.Timestamp.Value ).ToLocalTime() );

                switch ( edifyEvent.Event )
                {
                    case "MessageHeld":
                    case "MessageDeliveryFailed":
                        communicationRecipient.Status = CommunicationRecipientStatus.Failed;
                        communicationRecipient.StatusNote = edifyEvent.Payload.Details + edifyEvent.Payload.Output;

                        if ( edifyEvent.Payload.Details.Contains( "Bounced Address" ) || edifyEvent.Payload.Output.Contains( "Bounced Address" ) )
                        {
                            Rock.Communication.Email.ProcessBounce(
                                edifyEvent.Payload.Message.To,
                                Rock.Communication.BounceType.HardBounce,
                                edifyEvent.Payload.Details,
                                timeStamp );
                        }
                        else if ( edifyEvent.Payload.Status == "HardFail" )
                        {
                            Rock.Communication.Email.ProcessBounce(
                                edifyEvent.Payload.Message.To,
                                Rock.Communication.BounceType.HardBounce,
                                edifyEvent.Payload.Output,
                                timeStamp );
                        }
                        else
                        {
                            Rock.Communication.Email.ProcessBounce(
                                edifyEvent.Payload.Message.To,
                                Rock.Communication.BounceType.SoftBounce,
                                communicationRecipient.StatusNote,
                                RockDateTime.ConvertLocalDateTimeToRockDateTime( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ).AddSeconds( edifyEvent.Timestamp.Value ).ToLocalTime() ) );
                        }
                        break;
                    case "MessageSent":
                        communicationRecipient.Status = CommunicationRecipientStatus.Delivered;
                        communicationRecipient.StatusNote = string.Format( "Confirmed delivered by Edify at {0}", timeStamp.ToString() );
                        break;
                    case "MessageDelayed":
                        // TODO: handle MessageDelayed.
                        break;
                    case "MessageBounced":
                        communicationRecipient.Status = CommunicationRecipientStatus.Failed;
                        var statusMsg = "";
                        if ( edifyEvent.Payload != null && edifyEvent.Payload.Details.IsNotNullOrWhiteSpace() || edifyEvent.Payload.Output.IsNotNullOrWhiteSpace() )
                        {
                            statusMsg = edifyEvent.Payload.Details + edifyEvent.Payload.Output;
                        }
                        else if ( edifyEvent.Payload != null && edifyEvent.Payload.Bounce != null )
                        {
                            statusMsg = $"Messaged Bounced - {edifyEvent.Payload.Bounce.Subject}";
                        }
                        else
                        {
                            statusMsg = "Message Bounced";
                        }
                        communicationRecipient.StatusNote = statusMsg;

                        Rock.Communication.Email.ProcessBounce(
                            edifyEvent.Payload.Message.To,
                            Rock.Communication.BounceType.HardBounce,
                            statusMsg,
                            timeStamp );
                        break;
                    case "MessageLoaded":
                        communicationRecipient.Status = CommunicationRecipientStatus.Opened;
                        communicationRecipient.OpenedDateTime = timeStamp;
                        communicationRecipient.OpenedClient = string.Format(
                            "{0} {1} ({2})",
                            edifyEvent.Payload.ClientOs ?? "unknown",
                            edifyEvent.Payload.ClientBrowser ?? "unknown",
                            edifyEvent.Payload.ClientDeviceType ?? "unknown" );

                        if ( interactionComponent != null )
                        {
                            interactionService.AddInteraction(
                                interactionComponent.Id,
                                communicationRecipient.Id,
                                "Opened",
                                edifyEvent.Payload.Message.MessageId,
                                communicationRecipient.PersonAliasId,
                                timeStamp,
                                edifyEvent.Payload.ClientBrowser,
                                edifyEvent.Payload.ClientOs,
                                edifyEvent.Payload.ClientDeviceType,
                                edifyEvent.Payload.ClientDeviceBrand,
                                edifyEvent.Payload.IpAddress,
                                null );
                        }

                        break;

                    case "MessageLinkClicked":
                        if ( interactionComponent != null )
                        {
                            interactionService.AddInteraction(
                                interactionComponent.Id,
                                communicationRecipient.Id,
                                "Click",
                                edifyEvent.Payload.Url,
                                communicationRecipient.PersonAliasId,
                                timeStamp,
                                edifyEvent.Payload.ClientBrowser,
                                edifyEvent.Payload.ClientOs,
                                edifyEvent.Payload.ClientDeviceType,
                                edifyEvent.Payload.ClientDeviceBrand,
                                edifyEvent.Payload.IpAddress,
                                null );
                        }

                        break;

                    case "DomainDNSError":
                        // Do nothing.
                        break;
                }

                rockContext.SaveChanges();
            }
        }

        private void ProcessForWorkflow( Guid? actionGuid, Rock.Data.RockContext rockContext, EdifyEvent edifyEvent )
        {
            RockLogger.Log.Debug( RockLogDomains.Communications, "ProcessForWorkflow {@payload}", edifyEvent );

            string status = string.Empty;
            switch ( edifyEvent.Event )
            {
                case "MessageSent":
                    status = SendEmailWithEvents.SENT_STATUS;
                    break;

                case "MessageLinkClicked":
                    status = SendEmailWithEvents.CLICKED_STATUS;
                    break;

                //case "open":
                case "MessageLoaded":
                    status = SendEmailWithEvents.OPENED_STATUS;
                    break;

                case "MessageHeld":
                case "MessageDeliveryFailed":
                    status = SendEmailWithEvents.FAILED_STATUS;
                    string message = edifyEvent.Payload.Details + edifyEvent.Payload.Output;

                    if ( edifyEvent.Payload.Status == "HardFail" )
                    {
                        Rock.Communication.Email.ProcessBounce(
                                edifyEvent.Payload.Message.To,
                                Rock.Communication.BounceType.HardBounce,
                                message,
                                RockDateTime.ConvertLocalDateTimeToRockDateTime( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ).AddSeconds( edifyEvent.Timestamp.Value ).ToLocalTime() ) );
                    }
                    else
                    {
                        Rock.Communication.Email.ProcessBounce(
                                edifyEvent.Payload.Message.To,
                                Rock.Communication.BounceType.SoftBounce,
                                message,
                                RockDateTime.ConvertLocalDateTimeToRockDateTime( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ).AddSeconds( edifyEvent.Timestamp.Value ).ToLocalTime() ) );
                    }
                    break;
                case "MessageBounced":
                    status = SendEmailWithEvents.FAILED_STATUS;
                    var statusMsg = "";
                    if ( edifyEvent.Payload != null && edifyEvent.Payload.Details.IsNotNullOrWhiteSpace() || edifyEvent.Payload.Output.IsNotNullOrWhiteSpace() )
                    {
                        statusMsg = edifyEvent.Payload.Details + edifyEvent.Payload.Output;
                    }
                    else if ( edifyEvent.Payload != null && edifyEvent.Payload.Bounce != null )
                    {
                        statusMsg = $"Messaged Bounced - {edifyEvent.Payload.Bounce.Subject}";
                    }
                    else
                    {
                        statusMsg = "Message Bounced";
                    }

                    Rock.Communication.Email.ProcessBounce(
                            edifyEvent.Payload.Message.To,
                            Rock.Communication.BounceType.HardBounce,
                            statusMsg,
                            RockDateTime.ConvertLocalDateTimeToRockDateTime( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ).AddSeconds( edifyEvent.Timestamp.Value ).ToLocalTime() ) );
                    break;
            }

            if ( actionGuid != null && !string.IsNullOrWhiteSpace( status ) )
            {
                SendEmailWithEvents.UpdateEmailStatus( actionGuid.Value, status, edifyEvent.Event, rockContext, true );
            }
        }
    }
}