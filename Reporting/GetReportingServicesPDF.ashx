<%@ WebHandler Language="C#" Class="com.kfs.Reporting.SQLReportingServices.GetReportingServicesPDF" %>

using System;
using System.Collections.Generic;
using System.Web;

using com.kfs.Reporting.SQLReportingServices;
using Microsoft.Reporting.WebForms;

namespace com.kfs.Reporting.SQLReportingServices
{
    public class GetReportingServicesPDF : IHttpHandler
    {

        HttpContext Context;
        public void ProcessRequest( HttpContext context )
        {
            try
            {
                Context = context;
                string reportPath = GetRequestParameter( "reportPath" );

                if ( String.IsNullOrWhiteSpace( reportPath ) )
                {
                    throw new Exception( "Report Path is required." );
                }

                byte[] reportBody = new byte[] { };
                if ( !String.IsNullOrWhiteSpace( reportPath ) )
                {
                    reportBody = LoadReport( HttpUtility.UrlDecode( reportPath ) );
                }
                context.Response.ContentType = "application/pdf";
                context.Response.OutputStream.Write( reportBody, 0, reportBody.Length );
            }
            catch ( Exception ex )
            {

                if ( context.Response.IsClientConnected )
                {
                    Rock.Model.ExceptionLogService.LogException( ex, context );
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = ex.Message;
                    context.ApplicationInstance.CompleteRequest();

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

        private string GetRequestParameter( string paramName )
        {
            string value = null;
            if ( !String.IsNullOrWhiteSpace( Context.Request.QueryString[paramName] ) )
            {
                value = Context.Request.QueryString[paramName];
            }

            return value;
        }

        private byte[] LoadReport( string reportPath )
        {
            var provider = new ReportingServicesProvider();
            var rv = new ReportViewer();
            rv.ProcessingMode = ProcessingMode.Remote;
            var report = rv.ServerReport;
            report.ReportServerUrl = new Uri( provider.ServerUrl );
            report.ReportPath = provider.GetFolderPath( reportPath );
            report.ReportServerCredentials = provider.GetBrowserCredentials();

            List<ReportParameter> reportParams = new List<ReportParameter>();
            foreach ( var param in report.GetParameters() )
            {
                string qsValue = GetRequestParameter( param.Name );
                if ( !String.IsNullOrWhiteSpace( qsValue ) )
                {
                    reportParams.Add( new ReportParameter( param.Name, HttpUtility.UrlDecode( qsValue ) ) );
                }
            }
            report.SetParameters( reportParams );
            Warning[] warnings;
            string[] streamids;
            string mimeType;
            string encoding;
            string extension;

            var byteArray = report.Render( "PDF", null, out mimeType, out encoding, out extension, out streamids, out warnings );

            return byteArray;

        }

    }
}