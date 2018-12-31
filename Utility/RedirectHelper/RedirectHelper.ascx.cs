using System;
using System.ComponentModel;
using System.Web.UI;

using Rock.Attribute;
using Rock.Model;

namespace RockWeb.Plugins.com_kingdomfirstsolutions.Utility
{
    #region Block Attributes

    [DisplayName( "Redirect Helper" )]
    [Category( "KFS > Utility" )]
    [Description( "Helper block for orgnanizations that are in transistion from their old website to Rock. Checks an external site to see if the page exists and redirects instead of displaying 404 error." )]

    [BooleanField("Enabled", "A true/false flag indicating if the Redirect Helper page should be enabled. Default is false", false, "", 0, "Enabled")]
    [TextField("User Agent", "Name of the UserAgent that will verify that the page exists on the external site. Default is \"KFS Redirect Helper\".", false, "KFS Redirect Helper","", 1, "UserAgent")]
    [UrlLinkField( "External Site", "Base URL to an external site to check to see if the page exists.", true, "", "Redirect", 0, "ExternalSite" )]
    [BooleanField( "Use Permanent Redirect", "A true/false flag indicating if a permanent redirect response should supplied to the browser. Default is false.", false, "Redirect", 0, "UsePermanentRedirect" )]

    #endregion

    public partial class RedirectHelper : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            bool isEnabled = false;

            bool.TryParse( GetAttributeValue( "Enabled" ), out isEnabled );
            if ( !isEnabled || String.IsNullOrWhiteSpace( GetAttributeValue( "ExternalSite" ) ) )
            {
                return;
            }

            string orignalUrl = String.Empty;
            if ( !String.IsNullOrWhiteSpace( Request.QueryString["originalUrl"] ) )
            {
                orignalUrl = Server.UrlDecode( Request.QueryString["originalUrl"] );
            }
            else
            {
                return;
            }

            if ( !Page.IsPostBack )
            {
                if ( ExternalPageExists( orignalUrl )  )
                {
                    RedirectUser( orignalUrl );
                }
            }
        }

        private bool ExternalPageExists( string originalUrl )
        {
            bool pageExists = false;

            try
            {
                string baseUrl = GetAttributeValue( "ExternalSite" );
                if ( !baseUrl.EndsWith( "/" ) )
                {
                    baseUrl += "/";
                }
                string url = string.Concat( baseUrl, originalUrl );

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create( url );
                req.UserAgent = GetAttributeValue( "UserAgent" );

                System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse();

                if ( resp.StatusCode == System.Net.HttpStatusCode.OK )
                {
                    pageExists = true;
                }
            }
            catch ( Exception ex )
            {
                pageExists = false;
            }

            return pageExists;
        }

        private void RedirectUser( string originalUrl )
        {
            string baseUrl = GetAttributeValue( "ExternalSite" );
            if ( !baseUrl.EndsWith( "/" ) )
            {
                baseUrl += "/";
            }
            string url = string.Concat( baseUrl, originalUrl );

            bool use301 = false;

            bool.TryParse( GetAttributeValue( "UsePermanentRedirect" ), out use301 );

            Response.Redirect( url, false );
            if ( use301 )
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.MovedPermanently;
                Response.AddHeader( "Location", url );
                Response.End();
            }
        }
    }
}
