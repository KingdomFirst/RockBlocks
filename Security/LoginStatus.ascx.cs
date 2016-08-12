// <copyright>
// Copyright 2013 by the Spark Development Network
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
using System.ComponentModel;
using System.Web.Security;
using Rock;
using Rock.Attribute;
using Rock.Security;

namespace RockWeb.Blocks.Security
{
    /// <summary>
    /// Displays currently logged in user's name along with options to Login, Logout, or manage account.
    /// </summary>
    [DisplayName( "KFS Login Status" )]
    [Category( "KFS > Security" )]
    [Description( "Displays the currently logged in user's name along with options to Login, Logout, or manage account." )]

    [LinkedPage( "My Account Page", "Page for user to manage their account (if blank will use 'MyAccount' page route)" )]
    public partial class LoginStatus : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            var myAccountUrl = LinkedPageUrl( "MyAccountPage" );

            if ( !string.IsNullOrWhiteSpace( myAccountUrl ) )
            {
                aMyAccount.HRef = myAccountUrl;
            }
            else
            {
                aMyAccount.Visible = false;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var currentPerson = CurrentPerson;
            if ( currentPerson != null )
            {
                lHello.Text = string.Format( "<span>Hello {0}</span>", currentPerson.NickName );

                var currentUser = CurrentUser;
                if ( currentUser == null || !currentUser.IsAuthenticated )
                {
                    aMyAccount.Visible = false;
                    lbLogin.Visible = true;
                }

                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "PersonId", currentPerson.Id.ToString() );

                //divProfilePhoto.Attributes.Add( "style", String.Format( "background-image: url('{0}'); background-size: cover; background-repeat: no-repeat;", Rock.Model.Person.GetPhotoUrl( currentPerson.PhotoId, currentPerson.Age, currentPerson.Gender ) ) );

                imgProvilePhoto.ImageUrl = Rock.Model.Person.GetPhotoUrl( currentPerson.PhotoId, currentPerson.Age, currentPerson.Gender, null, null );

                lbLogin.Visible = false;
            }
            else
            {
                phHello.Visible = false;
                aMyAccount.Visible = false;

                lbLogin.Visible = true;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbLoginLogout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbLoginLogout_Click( object sender, EventArgs e )
        {
            var site = RockPage.Layout.Site;
            if ( site.LoginPageId.HasValue )
            {
                site.RedirectToLoginPage( true );
            }
            else
            {
                FormsAuthentication.RedirectToLoginPage();
            }
        }

        #endregion
    }
}