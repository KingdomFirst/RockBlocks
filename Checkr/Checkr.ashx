<%@ WebHandler Language="C#" Class="RockWeb.Webhooks.com.kfs.CheckrWebhook" %>

using System;
using System.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Rock;
using Rock.Data;
using Rock.Model;
using KFSCheckr = com.kfs.Checkr;
using BackgroundCheck = com.kfs.Checkr.Security.BackgroundCheck;

namespace RockWeb.Webhooks.com.kfs
{
    /// <summary>
    /// Handles the background check results sent from Checkr
    /// </summary>
    public class CheckrWebhook : IHttpHandler
    {

        public void ProcessRequest( HttpContext context )
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            response.ContentType = "text/plain";

            if ( request.HttpMethod != "POST" )
            {
                response.StatusCode = 403;
                response.Write( "Invalid request type." );
                return;
            }

            var stream = new StreamReader( request.InputStream );
            var body = stream.ReadToEnd();

            if ( !string.IsNullOrWhiteSpace( body ) )
            {
                try
                {
                    var rockContext = new Rock.Data.RockContext();

                    BackgroundCheck.RootObject rootObject = JsonConvert.DeserializeObject<BackgroundCheck.RootObject>( body );

                    if ( rootObject.type == "report.completed" )
                    {
                        // Find and update the associated workflow
                        var workflowService = new WorkflowService( rockContext );
                        var workflow = workflowService.Queryable().WhereAttributeValue( rockContext, "CandidateId", rootObject.data.@object.candidate_id ).FirstOrDefault();
                        if ( workflow != null )
                        {
                            workflow.LoadAttributes();

                            BackgroundCheck.Checkr.SaveResults( rootObject, workflow, rockContext );

                            rockContext.WrapTransaction( () =>
                            {
                                rockContext.SaveChanges();
                                workflow.SaveAttributeValues( rockContext );
                                foreach ( var activity in workflow.Activities )
                                {
                                    activity.SaveAttributeValues( rockContext );
                                }
                            } );

                            response.StatusCode = 200;
                            response.Write( "report.completed processed" );
                        }
                        else
                        {
                            response.StatusCode = 202;
                            response.Write( "candidate_id not found" );
                        }
                    }
                }
                catch ( SystemException ex )
                {
                    ExceptionLogService.LogException( ex, context );
                    response.StatusCode = 202;
                    response.Write( string.Format( "{0}", ex ) );
                }
            }
            else
            {
                response.StatusCode = 202;
                response.Write( "nothing processed" );
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

    }
}