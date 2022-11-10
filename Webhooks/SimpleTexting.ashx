<%@ WebHandler Language="C#" Class="SimpleTextingAsync" %>
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Rock;
using Rock.Communication.SmsActions;
using Rock.Data;
using Rock.Model;
using SimpleTextingDotNet.v2.Model.WebhookReports;

/// <summary>
/// Simple Texting Webhook that can process text messages through SMS Pipeline
/// </summary>
public class SimpleTextingAsync : IHttpAsyncHandler
{
    public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback cb, Object extraData )
    {
        SimpleTextingResponseAsync simpleTexting = new SimpleTextingResponseAsync( cb, context, extraData );
        simpleTexting.StartAsyncWork();
        return simpleTexting;
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

class SimpleTextingResponseAsync : IAsyncResult
{
    private bool _completed;
    private readonly Object _state;
    private readonly AsyncCallback _callback;
    private readonly HttpContext _context;

    bool IAsyncResult.IsCompleted { get { return _completed; } }
    WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
    Object IAsyncResult.AsyncState { get { return _state; } }
    bool IAsyncResult.CompletedSynchronously { get { return false; } }

    /// <summary>
    /// Gets a <see cref="T:System.Threading.WaitHandle" /> that is used to wait for an asynchronous operation to complete.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public WaitHandle AsyncWaitHandle
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTextingResponseAsync"/> class.
    /// </summary>
    /// <param name="callback">The callback.</param>
    /// <param name="context">The context.</param>
    /// <param name="state">The state.</param>
    public SimpleTextingResponseAsync( AsyncCallback callback, HttpContext context, Object state )
    {
        _callback = callback;
        _context = context;
        _state = state;
        _completed = false;
    }

    /// <summary>
    /// Starts the asynchronous work.
    /// </summary>
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

    /// <summary>
    /// Starts the asynchronous task.
    /// </summary>
    /// <param name="workItemState">State of the work item.</param>
    private void StartAsyncTask( Object workItemState )
    {
        var request = _context.Request;
        var response = _context.Response;

        response.ContentType = "text/json";

        if ( request.HttpMethod != "POST" )
        {
            response.Write( "Invalid request type." );
            response.StatusCode = ( int ) System.Net.HttpStatusCode.MethodNotAllowed;
        }
        else if ( !request.ContentType.Contains( "application/json" ) )
        {
            response.Write( "Invalid content type." );
            response.StatusCode = ( int ) System.Net.HttpStatusCode.NotAcceptable;
        }
        else
        {
            var processedObj = ProcessJsonContent( request, response );
            if ( processedObj != null )
            {
                switch ( processedObj.GetType().Name )
                {
                    case nameof( UnsubscribeReport ):
                        // ignore Unsubscribe reports for now, perhaps if we add contact syncing or group sync with contact lists we will remove people here.
                        break;
                    case nameof( MessageReport ):
                        var messageReport = ( MessageReport ) processedObj;
                        if ( messageReport.Type == SimpleTextingDotNet.Enum.Trigger.INCOMING_MESSAGE )
                        {
                            MessageReceived( messageReport );
                        }
                        // ignore outgoing message reports for now, do we need them for anything?
                        break;
                    case nameof( DeliveryMessageReport ):
                        var deliveryReport = ( DeliveryMessageReport ) processedObj;
                        if ( deliveryReport.Type == SimpleTextingDotNet.Enum.Trigger.NON_DELIVERED_REPORT )
                        {
                            MessageNotDelivered( deliveryReport );
                        }
                        else
                        {
                            MessageDelivered( deliveryReport );
                        }
                        break;
                }

                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 500;
            }
        }

        _completed = true;
        _callback( this );
    }

    private Object ProcessJsonContent( HttpRequest request, HttpResponse response )
    {
        Object retObj = null;
        string payload = string.Empty;

        using ( Stream s = request.InputStream )
        {
            using ( StreamReader readStream = new StreamReader( s, Encoding.UTF8 ) )
            {
                payload = readStream.ReadToEnd();
            }
        }

        UnsubscribeReport unsubscribeReport = null;
        MessageReport messageReport = null;
        DeliveryMessageReport deliveryReport = null;

        unsubscribeReport = JsonConvert.DeserializeObject<UnsubscribeReport>( payload );

        switch ( unsubscribeReport.Type )
        {
            case SimpleTextingDotNet.Enum.Trigger.INCOMING_MESSAGE:
            case SimpleTextingDotNet.Enum.Trigger.OUTGOING_MESSAGE:
                messageReport = JsonConvert.DeserializeObject<MessageReport>( payload );
                unsubscribeReport = null;
                break;
            case SimpleTextingDotNet.Enum.Trigger.DELIVERY_REPORT:
            case SimpleTextingDotNet.Enum.Trigger.NON_DELIVERED_REPORT:
                deliveryReport = JsonConvert.DeserializeObject<DeliveryMessageReport>( payload );
                unsubscribeReport = null;
                break;
        }

        if ( unsubscribeReport == null && messageReport == null && deliveryReport == null )
        {
            response.Write( "Invalid content type." );
            response.StatusCode = ( int ) System.Net.HttpStatusCode.NotAcceptable;
            return retObj;
        }

        if ( unsubscribeReport != null )
        {
            retObj = unsubscribeReport;
        }
        else if ( messageReport != null )
        {
            retObj = messageReport;
        }
        else if ( deliveryReport != null )
        {
            retObj = deliveryReport;
        }

        response.StatusCode = ( int ) System.Net.HttpStatusCode.OK;
        return retObj;
    }


    /// <summary>
    /// Mark message as not delivered.
    /// </summary>
    private void MessageNotDelivered( DeliveryMessageReport deliveryReport )
    {
        if ( deliveryReport != null && deliveryReport.Values != null && deliveryReport.Values.MessageId.IsNotNullOrWhiteSpace() )
        {
            var messageId = deliveryReport.Values.MessageId;

            using ( RockContext rockContext = new RockContext() )
            {
                CommunicationRecipientService recipientService = new CommunicationRecipientService( rockContext );

                var communicationRecipient = recipientService
                    .Queryable()
                    .Where( r => r.UniqueMessageId == messageId )
                    .FirstOrDefault();

                if ( communicationRecipient != null )
                {
                    communicationRecipient.Status = CommunicationRecipientStatus.Failed;
                    communicationRecipient.StatusNote = $"Message failure notification from Simple Texting on {RockDateTime.Now.ToString()}. Carrier: {deliveryReport.Values.Carrier}";
                    rockContext.SaveChanges();
                }
                else
                {
                    ExceptionLogService.LogException( $"No recipient was found with the specified MessageId value! ({messageId})" );
                }
            }
        }
    }

    /// <summary>
    /// Mark message as delivered.
    /// </summary>
    private void MessageDelivered( DeliveryMessageReport deliveryReport )
    {
        if ( deliveryReport != null && deliveryReport.Values != null && deliveryReport.Values.MessageId.IsNotNullOrWhiteSpace() )
        {
            var messageId = deliveryReport.Values.MessageId;

            using ( RockContext rockContext = new RockContext() )
            {
                CommunicationRecipientService recipientService = new CommunicationRecipientService( rockContext );

                var communicationRecipient = recipientService
                    .Queryable()
                    .Where( r => r.UniqueMessageId == messageId )
                    .FirstOrDefault();

                if ( communicationRecipient != null )
                {
                    communicationRecipient.Status = CommunicationRecipientStatus.Delivered;
                    communicationRecipient.StatusNote = $"Message Confirmed delivered by Simple Texting on {RockDateTime.Now.ToString()}";
                    rockContext.SaveChanges();
                }
                else
                {
                    ExceptionLogService.LogException( $"No recipient was found with the specified MessageId value! ({messageId})" );
                }
            }
        }
    }

    /// <summary>
    /// Messages Received handler
    /// </summary>
    private void MessageReceived( MessageReport messageReport )
    {
        var request = _context.Request;

        string fromPhone = string.Empty;
        string toPhone = string.Empty;
        string body = string.Empty;

        if ( messageReport != null && messageReport.Values != null && messageReport.Values.AccountPhone.IsNotNullOrWhiteSpace() )
        {
            toPhone = messageReport.Values.AccountPhone;
        }

        if ( messageReport != null && messageReport.Values != null && messageReport.Values.ContactPhone.IsNotNullOrWhiteSpace() )
        {
            fromPhone = messageReport.Values.ContactPhone;
        }

        if ( messageReport != null && messageReport.Values != null && messageReport.Values.Text.IsNotNullOrWhiteSpace() )
        {
            body = messageReport.Values.Text;
        }

        var processedMessage = false;
        if ( toPhone.IsNotNullOrWhiteSpace() && fromPhone.IsNotNullOrWhiteSpace() )
        {
            processedMessage = ProcessMessage( request, toPhone, fromPhone, body, messageReport );
        }
    }

    /// <summary>
    /// Processes the message when a message is received.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="toPhone"></param>
    /// <param name="fromPhone"></param>
    /// <param name="body"></param>
    /// <param name="messageReport"></param>
    /// <returns>True if message was sent back, false if no message was sent</returns>
    public bool ProcessMessage( HttpRequest request, string toPhone, string fromPhone, string body, MessageReport messageReport )
    {
        var message = new SmsMessage
        {
            ToNumber = toPhone,
            FromNumber = fromPhone,
            Message = body
        };

        if ( !string.IsNullOrWhiteSpace( message.ToNumber ) && !string.IsNullOrWhiteSpace( message.FromNumber ) )
        {
            using ( var rockContext = new RockContext() )
            {
                message.FromPerson = new PersonService( rockContext ).GetPersonFromMobilePhoneNumber( message.FromNumber, false );

                if ( message.FromPerson == null )
                {
                    // Hard coded to adding a 1 at the beginning if it does not match someone and is less than 11 characters for now because Simple Texting does not support international text messages receiving or sending.
                    // This should still try to match if the contact phone is >= 11 though.
                    message.FromNumber = ( message.FromNumber.Length < 11 ) ? $"1{message.FromNumber}" : message.FromNumber;
                    message.FromPerson = new PersonService( rockContext ).GetPersonFromMobilePhoneNumber( message.FromNumber, true );
                }

                var smsPipelineId = request.QueryString["smsPipelineId"].AsIntegerOrNull();

                SimpleTextingDotNet.v2.Client simpleTextingClient = null;
                // Simple Texting does not just respond to text messages based on the response in the webhook, so we need to standup the component, client and actually send a message with the methods we have.
                var simpleTextingComponent = new rocks.kfs.SimpleTexting.Communications.Transport.SimpleTexting();
                simpleTextingComponent.LoadAttributes();

                if ( simpleTextingComponent.IsActive )
                {
                    simpleTextingClient = new SimpleTextingDotNet.v2.Client( simpleTextingComponent.GetAttributeValue( rocks.kfs.SimpleTexting.Communications.Transport.SimpleTexting.AttributeKey.ApiKey ) );
                }

                if ( messageReport != null && messageReport.Values != null && messageReport.Values.MediaItems != null && messageReport.Values.MediaItems.Any() && simpleTextingClient != null )
                {
                    Guid imageGuid;
                    foreach ( var mediaitem in messageReport.Values.MediaItems )
                    {
                        var getMediaItem = simpleTextingClient.GetMediaItem( mediaitem );

                        string imageUrl = "";
                        string mimeType = "";
                        string fileExtension = "";
                        if ( getMediaItem != null && getMediaItem.Link.IsNotNullOrWhiteSpace() )
                        {
                            imageUrl = getMediaItem.Link;
                            mimeType = getMediaItem.ContentType;
                            fileExtension = getMediaItem.Ext;
                        }
                        else
                        {
                            // due to the SimpleTexting API not currently being able to return an object for media items not in our organization, we will try a full url to retrieve it.
                            imageUrl = $"https://app2.simpletexting.com/content/public-files/{mediaitem}";
                        }

                        imageGuid = Guid.NewGuid();

                        System.IO.Stream stream = null;
                        var httpWebRequest = ( HttpWebRequest ) HttpWebRequest.Create( imageUrl );
                        var httpWebResponse = ( HttpWebResponse ) httpWebRequest.GetResponse();

                        if ( httpWebResponse.ContentLength == 0 )
                        {
                            continue;
                        }
                        if ( mimeType.IsNullOrWhiteSpace() )
                        {
                            mimeType = httpWebResponse.ContentType;
                            // v13+ has a static helper method to get file extension from mimeType, copying method for now...
                            // fileExtension = Rock.Utility.FileUtilities.GetFileExtensionFromContentType( mimeType );
                            fileExtension = GetFileExtensionFromContentType( mimeType );
                        }

                        string fileName = string.Format( "SMS-Attachment-{0}.{1}", mediaitem, fileExtension );
                        stream = httpWebResponse.GetResponseStream();
                        // v13+ adds new AddFileFromStream Method. For now we will do our own using copied code.
                        //var binaryFile = new BinaryFileService( rockContext ).AddFileFromStream( stream, mimeType, httpWebResponse.ContentLength, fileName, Rock.SystemGuid.BinaryFiletype.COMMUNICATION_ATTACHMENT, imageGuid );
                        var binaryFile = AddFileFromStream( rockContext, stream, mimeType, httpWebResponse.ContentLength, fileName, Rock.SystemGuid.BinaryFiletype.COMMUNICATION_ATTACHMENT, imageGuid );
                        message.Attachments.Add( binaryFile );
                    }
                }

                var outcomes = SmsActionService.ProcessIncomingMessage( message, smsPipelineId );
                var smsResponse = SmsActionService.GetResponseFromOutcomes( outcomes );
                var attachmentUrls = new List<string>();

                if ( smsResponse == null )
                {
                    return false; // Message was not sent back.
                }

                if ( smsResponse.Attachments != null && smsResponse.Attachments.Any() )
                {
                    foreach ( var binaryFile in smsResponse.Attachments )
                    {
                        attachmentUrls.Add( binaryFile.Url );
                    }
                }

                if ( !string.IsNullOrWhiteSpace( smsResponse.Message ) )
                {
                    if ( simpleTextingClient != null )
                    {
                        simpleTextingClient.SendMessage( message.FromNumber, smsResponse.Message, accountPhone: message.ToNumber, mediaItems: attachmentUrls );
                        return true; // Message was sent back.
                    }
                }

                return false; // Message was not sent back.
            }
        }

        return false; // Message was not sent back.
    }

    /// <summary>
    /// Adds the file from stream. This method will save the current context.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="mimeType">Type of the MIME.</param>
    /// <param name="contentLength">Length of the content.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="binaryFileTypeGuid">The binary file type unique identifier.</param>
    /// <param name="imageGuid">The image unique identifier.</param>
    /// <returns></returns>
    public BinaryFile AddFileFromStream( RockContext rockContext, Stream stream, string mimeType, long contentLength, string fileName, string binaryFileTypeGuid, Guid? imageGuid )
    {
        int? binaryFileTypeId = Rock.Web.Cache.BinaryFileTypeCache.GetId( binaryFileTypeGuid.AsGuid() );

        imageGuid = imageGuid == null || imageGuid == Guid.Empty ? Guid.NewGuid() : imageGuid;
        using ( var memoryStream = new System.IO.MemoryStream() )
        {
            stream.CopyTo( memoryStream );
            var binaryFile = new BinaryFile
            {
                IsTemporary = false,
                BinaryFileTypeId = binaryFileTypeId,
                MimeType = mimeType,
                FileName = fileName,
                FileSize = contentLength,
                ContentStream = memoryStream,
                Guid = imageGuid.Value
            };

            var binaryFileService = new BinaryFileService( rockContext );
            binaryFileService.Add( binaryFile );
            rockContext.SaveChanges();
            return binaryFile;
        }
    }
    /// <summary>
    /// Gets the type of the file extension from content. If you need to go the other way use System.Web.MimeMapping.GetMimeMapping(String)
    /// </summary>
    /// <returns></returns>
    public static string GetFileExtensionFromContentType( string contentType )
    {
        // Add to this list as needed.
        var mapping = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
            {
                { "bmp", "image/bmp" },
                { "gif", "image/gif" },
                { "jpeg", "image/jpeg" },
                { "jpg", "image/jpeg" },
                { "jpe", "image/jpeg" },
                { "png", "image/png" },
                { "sgi", "image/sgi" },
                { "svg", "image/svg+xml" },
                { "svgz", "image/svg+xml" },
                { "tiff", "image/tiff" },
                { "tif", "image/tiff" }
            };

        return mapping.FirstOrDefault( x => x.Value.Contains( contentType ) ).Key ?? string.Empty;
    }
}
