using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


using Rock;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;

namespace RockWeb.Plugins.com_kingdomfirstsolutions.Utility
{
    public partial class Http404Error : System.Web.UI.Page
    {
        /// <summary>
        /// Handles the Init event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Init( object sender, EventArgs e )
        {
            // Check to see if exception should be logged
            if ( GlobalAttributesCache.Get().GetValue( "Log404AsException" ).AsBoolean( true ) )
            {
                ExceptionLogService.LogException( new Exception( string.Format( "404 Error: {0}", Request.Url.AbsoluteUri ) ), Context );
            }

            // If this is an API call, set status code and exit
            if ( Request.Url.Query.Contains( Request.Url.Authority + ResolveUrl( "~/api/" ) ) )
            {
                Response.StatusCode = 404;
                Response.Flush();
                Response.End();
                return;
            }
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            try
            {
                // Set form action to pass XSS test
                form1.Action = "/";

                // try to get site's 404 page
                SiteCache site = SiteCache.GetSiteByDomain( Request.Url.Host );
                if ( site != null && site.PageNotFoundPageId.HasValue )
                {
                    //site.RedirectToPageNotFoundPage();
                    PageReference redirectRef = site.PageNotFoundPageReference;

                    if ( redirectRef.QueryString == null )
                    {
                        redirectRef.QueryString = new System.Collections.Specialized.NameValueCollection();
                    }
                    string orignalUrl = Request.RawUrl.IndexOf( '/' ) == 0 ? Request.RawUrl.Substring( 1 ) : Request.RawUrl;
                    redirectRef.QueryString.Add( "originalUrl", orignalUrl );

                    Response.Redirect( redirectRef.BuildUrl(), false );
                }
                else
                {
                    Response.StatusCode = 404;
                    lLogoSvg.Text = System.IO.File.ReadAllText( HttpContext.Current.Request.MapPath( "~/Assets/Images/rock-logo-sm.svg" ) );
                }
            }
            catch
            {
                Response.StatusCode = 404;
                lLogoSvg.Text = System.IO.File.ReadAllText( HttpContext.Current.Request.MapPath( "~/Assets/Images/rock-logo-sm.svg" ) );
            }
        }
    }
}