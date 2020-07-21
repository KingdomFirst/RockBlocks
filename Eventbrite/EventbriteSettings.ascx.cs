﻿// <copyright>
// Copyright 2020 by Kingdom First Solutions
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
using System.Linq.Dynamic;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Drawing.Charts;
using EventbriteDotNetFramework;
using EventbriteDotNetFramework.Entities;
using Lucene.Net.Analysis.Hunspell;
using Mono.CSharp;
using OpenXmlPowerTools;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI.Controls;
using rocks.kfs.Eventbrite;

namespace RockWeb.Plugins.rocks_kfs.Eventbrite
{
    [DisplayName( "Eventbrite Settings" )]
    [Category( "KFS > Eventbrite" )]
    [Description( "Allows you to configure any necessary system settings for Eventbrite integration" )]

    [LinkedPage( "Group Detail", "", true, "", "", 0 )]

    public partial class EventbriteSettings : Rock.Web.UI.RockBlock
    {
        private string _accessToken = null;
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowDetail();
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            nbNotification.Visible = false;
            pnlToken.Visible = true;
            HideSecondaryBlocks( true );
            pnlGridWrapper.Visible = false;
            lView.Visible = false;
            btnEdit.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Settings.SaveAccessToken( tbOAuthToken.Text );
            _accessToken = tbOAuthToken.Text;
            nbNotification.Text = "Token Saved!";
            if ( _accessToken.IsNotNullOrWhiteSpace() )
            {
                Settings.SaveOrganizationId( ddlOrganization.SelectedValue );
                nbNotification.Text = "Token and Organization Saved!";
            }

            pnlToken.Visible = false;
            HideSecondaryBlocks( false );
            nbNotification.Visible = true;
            nbNotification.Title = "";
            nbNotification.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Info;
            nbNotification.Dismissable = true;
            ShowDetail();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gEBLinkedGroups_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Header )
            {
                //var ebEvent = e.Row.DataItem as RockEventbriteEvent;
                var detailPage = GetAttributeValue( "GroupDetail" );

                if ( detailPage.IsNullOrWhiteSpace() )
                {
                    var colIndex = gEBLinkedGroups.GetColumnByHeaderText( "Edit" );
                    e.Row.Cells[gEBLinkedGroups.GetColumnIndex( colIndex )].Visible = false;
                }
            }
        }

        protected void lbSyncNow_Click( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyValue.ToString().AsInteger();
            rocks.kfs.Eventbrite.Eventbrite.SyncEvent( groupId );
            ShowDetail();
        }
        protected void lbEditRow_Click( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyValue.ToString().AsInteger();
            var qryParams = new Dictionary<string, string> {
                { "GroupId", groupId.ToString() }
            };
            NavigateToLinkedPage( "GroupDetail", qryParams );
        }
        protected void lbDelete_Click( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyValue.ToString().AsInteger();
            rocks.kfs.Eventbrite.Eventbrite.UnlinkEvents( groupId );
            ShowDetail();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            _accessToken = Settings.GetAccessToken();
            var isAuthenticated = rocks.kfs.Eventbrite.Eventbrite.EBoAuthCheck();
            if ( _accessToken.IsNullOrWhiteSpace() )
            {
                pnlToken.Visible = true;
                HideSecondaryBlocks( true );
                pnlGridWrapper.Visible = false;
            }
            else
            {
                tbOAuthToken.Text = _accessToken;
                if ( isAuthenticated )
                {
                    lblLoginStatus.Text = "Authenticated";
                    lblLoginStatus.CssClass = "pull-right label label-success";
                    loadOrganizations();
                }
                else
                {
                    lblLoginStatus.Text = "Not authenticated";
                    lblLoginStatus.CssClass = "pull-right label label-danger";
                    tbOAuthToken.Text = "";
                }
                lView.Text = new DescriptionList()
                    .Add( "Private Token", _accessToken )
                    .Add( "Organization", Settings.GetOrganizationId() )
                    .Html;
                pnlToken.Visible = false;
                nbNotification.Visible = false;
                pnlGridWrapper.Visible = true;
                lView.Visible = true;
                btnEdit.Visible = true;
                gEBLinkedGroups.DataSource = new EventbriteEvents().Events();
                gEBLinkedGroups.DataBind();
            }
        }

        private void loadOrganizations()
        {
            var ebOrganizations = rocks.kfs.Eventbrite.Eventbrite.Api( _accessToken ).GetOrganizations().Organizations;
            var selectedOrgId = Settings.GetOrganizationId();
            if ( ebOrganizations != null && ebOrganizations.Any() )
            {
                ddlOrganization.Items.Clear();
                ddlOrganization.Items.Add( new ListItem( "Please select an organization", "" ) );
                foreach ( var org in ebOrganizations )
                {
                    ddlOrganization.Items.Add( new ListItem( org.Name, org.Id.ToString() ) );
                }
                if ( selectedOrgId.IsNotNullOrWhiteSpace() )
                {
                    ddlOrganization.SelectedValue = selectedOrgId;
                }
                ddlOrganization.Visible = true;
            }
            else
            {
                ddlOrganization.Visible = false;
            }
        }

        #endregion
    }
}