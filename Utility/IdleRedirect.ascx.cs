using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;

using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Utility
{
    #region Block Attributes

    [DisplayName( "Idle Redirect Advanced" )]
    [Category( "KFS > Utility" )]
    [Description( "Redirects user to a new url after a specific number of idle seconds." )]

    [TextField( "New Location", "The new location URL to send user to after idle time.  If left blank, the current url will reload.", false )]
    [IntegerField( "Idle Seconds", "How many seconds of idle time to wait before redirecting user", false, 20 )]

    #endregion

    public partial class IdleRedirect : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/idle-timer.min.js" );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            int idleSeconds;

            var newLocation = GetAttributeValue( "NewLocation" );
            if ( newLocation == string.Empty )
            {
                newLocation = HttpContext.Current.Request.Url.AbsoluteUri;
            }

            if ( !int.TryParse( GetAttributeValue( "IdleSeconds" ), out idleSeconds ) )
                idleSeconds = 30;

            int ms = idleSeconds * 1000;
            string script = string.Format( @"
            $(function () {{
                Sys.WebForms.PageRequestManager.getInstance().add_pageLoading(function () {{
                    $.idleTimer('destroy');
                }});
                $.idleTimer({0});
                $(document).bind('idle.idleTimer', function() {{
                    window.location = '{1}';
                }});
            }});
            ", ms, ResolveRockUrl( newLocation ) );
            ScriptManager.RegisterStartupScript( Page, this.GetType(), "idle-timeout", script, true );
        }
   }
}
