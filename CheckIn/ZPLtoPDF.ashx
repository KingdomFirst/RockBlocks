<%@ WebHandler Language="C#" Class="ZPLtoPDF" %>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

using Rock;

public class ZPLtoPDF : IHttpHandler
{
    private HttpRequest request;
    private HttpResponse response;

    public void ProcessRequest( HttpContext context )
    {
        var dpmm = "8";
        var width = "4";
        var height = "2";
        var index = string.Empty;
        var zpl = string.Empty;

        request = context.Request;
        response = context.Response;

        context.Response.Buffer = true;
        context.Response.Clear();
        context.Response.ContentType = "application/pdf";
        context.Response.AddHeader( "content-disposition", "inline;filename=file.pdf" );

        var reader = new StreamReader( request.InputStream );
        zpl = reader.ReadToEnd();

        if ( !string.IsNullOrWhiteSpace( request.QueryString["zpl"] ) )
        {
            zpl = request.QueryString["zpl"];
        }

        if ( !string.IsNullOrWhiteSpace( request.QueryString["dpmm"] ) )
        {
            dpmm = request.QueryString["dpmm"];
        }
        if ( !string.IsNullOrWhiteSpace( request.QueryString["width"] ) )
        {
            width = request.QueryString["width"];
        }
        if ( !string.IsNullOrWhiteSpace( request.QueryString["height"] ) )
        {
            height = request.QueryString["height"];
        }
        if ( !string.IsNullOrWhiteSpace( request.QueryString["index"] ) )
        {
            index = request.QueryString["index"];
        }

        if ( !string.IsNullOrWhiteSpace( zpl ) )
        {
            byte[] pdfBytes = null;
            byte[] zplBytes = Encoding.UTF8.GetBytes( zpl );
            var labelaryUrl = string.Format( "http://api.labelary.com/v1/printers/{0}dpmm/labels/{1}x{2}/{3}", dpmm, width, height, ( index != string.Empty ? index + "/" : string.Empty ) );
            var labelaryRequest = ( HttpWebRequest ) WebRequest.Create( labelaryUrl );
            labelaryRequest.Method = "POST";
            labelaryRequest.Accept = "application/pdf"; // omit this line to get PNG images back
            labelaryRequest.ContentType = "application/x-www-form-urlencoded";
            labelaryRequest.ContentLength = zplBytes.Length;

            var requestStream = labelaryRequest.GetRequestStream();
            requestStream.Write( zplBytes, 0, zplBytes.Length );
            requestStream.Close();

            try
            {
                var labelaryResponse = ( HttpWebResponse ) labelaryRequest.GetResponse();
                var responseStream = labelaryResponse.GetResponseStream();
                pdfBytes = responseStream.ReadBytesToEnd();
                responseStream.Close();
                response.OutputStream.Write( pdfBytes, 0, pdfBytes.Length );
            }
            catch ( WebException e )
            {
                Console.WriteLine( "Error: {0}", e.Status );
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