// <copyright>
// Copyright 2021 by Kingdom First Solutions
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
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomDotNetFramework.Entities;
using ZoomDotNetFramework.Responses;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using rocks.kfs.Zoom;

namespace RockWeb.Plugins.rocks_kfs.Zoom
{
    #region Block Attributes

    [DisplayName( "Zoom Settings" )]
    [Category( "KFS > Zoom" )]
    [Description( "Allows you to configure any necessary system settings for Zoom integration" )]

    #endregion Block Attributes

    #region Block Settings

    [BooleanField(
        "Enable Logging",
        Description = "Enable logging for Zoom sync methods from this block.",
        IsRequired = false,
        DefaultBooleanValue = false,
        Order = 1,
        Key = AttributeKey.EnableLogging )]

    #endregion Block Settings

    public partial class ZoomSettings : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string EnableLogging = "EnableLogging";
        }

        private string _appAccountId = null;
        private string _appClientId = null;
        private string _appClientSecret = null;
        private string _webhookURL = null;

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            this.BlockUpdated += Block_BlockUpdated;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            _appAccountId = Settings.GetAppAccountId();
            _appClientId = Settings.GetAppClientId();
            _appClientSecret = Settings.GetAppClientSecret();
            _webhookURL = string.Format( "{0}Plugins/rocks_kfs/Zoom/Webhook.ashx", GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" ) );

            if ( !Page.IsPostBack )
            {
                ShowDetail();
            }
        }

        #endregion Control Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail();
        }

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;
            pnlApiSettings.Visible = true;
            HideSecondaryBlocks( true );
            lView.Visible = false;
            btnEdit.Visible = false;
            btnSyncNow.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            _appAccountId = tbAppAccountId.Text;
            _appClientId = tbAppClientId.Text;
            _appClientSecret = tbAppClientSecret.Text;
            Settings.SaveApiSettings( _appAccountId, _appClientId, _appClientSecret );
            nbNotification.Text = "API Settings Saved!";

            pnlApiSettings.Visible = false;
            HideSecondaryBlocks( false );
            nbNotification.Visible = true;
            nbNotification.Title = "";
            nbNotification.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
            nbNotification.Dismissable = true;
            ShowDetail( true );
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            pnlApiSettings.Visible = false;
            HideSecondaryBlocks( false );
            nbNotification.Visible = false;
            ShowDetail();
        }

        protected void btnSyncNow_Click( object sender, EventArgs e )
        {
            rocks.kfs.Zoom.Zoom.SyncZoomRoomDT( enableLogging: GetAttributeValue( AttributeKey.EnableLogging ).AsBoolean() );
            Response.Redirect( Request.RawUrl, false );
        }

        #endregion Events

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail( bool showNotificationbox = false )
        {
            if ( _appAccountId.IsNullOrWhiteSpace() || _appClientId.IsNullOrWhiteSpace() || _appClientSecret.IsNullOrWhiteSpace() )
            {
                pnlApiSettings.Visible = true;
                HideSecondaryBlocks( true );
                lView.Visible = false;
                btnEdit.Visible = false;
                btnSyncNow.Visible = false;
            }
            else
            {
                tbAppAccountId.Text = _appAccountId;
                tbAppClientId.Text = _appClientId;
                tbAppClientSecret.Text = _appClientSecret;
                var isAuthenticated = rocks.kfs.Zoom.Zoom.ZoomAuthCheck();
                if ( isAuthenticated )
                {
                    lblLoginStatus.Text = "API Authenticated";
                    lblLoginStatus.CssClass = "pull-right label label-success";
                }
                else
                {
                    lblLoginStatus.Text = "API Not Authenticated";
                    lblLoginStatus.CssClass = "pull-right label label-danger";
                }
                lView.Text = new DescriptionList()
                    .Add( "App Account ID", _appAccountId )
                    .Add( "App Client ID", _appClientId )
                    .Add( "App Client Secret", _appClientSecret )
                    .Html;
                pnlApiSettings.Visible = false;
                nbNotification.Visible = showNotificationbox;
                lView.Visible = true;
                btnEdit.Visible = true;
                btnSyncNow.Visible = true;
                btnSyncNow.Enabled = isAuthenticated;
            }
        }

        #endregion Internal Methods
    }
}