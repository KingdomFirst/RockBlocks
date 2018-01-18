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
                response.Write( "Invalid request type." );
                return;
            }

            if ( request.Form["REQUEST"] != null )
            {
                try
                {
                    var rockContext = new Rock.Data.RockContext();

                    BackgroundCheck.RootObject rootObject = JsonConvert.DeserializeObject<BackgroundCheck.RootObject>( request.Form["REQUEST"] );

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
                        }
                    }

                    try
                    {
                        // Return the success XML to PMM
                        XDocument xdocResult = new XDocument( new XDeclaration( "1.0", "UTF-8", "yes" ),
                            new XElement( "OrderXML",
                                new XElement( "Success", "TRUE" ) ) );

                        response.StatusCode = 200;
                        response.ContentType = "text/xml";
                        response.AddHeader( "Content-Type", "text/xml" );
                        xdocResult.Save( response.OutputStream );
                    }
                    catch { }
                }
                catch ( SystemException ex )
                {
                    ExceptionLogService.LogException( ex, context );
                }
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