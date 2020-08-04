// <copyright>
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
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI;
using System.Web.UI.WebControls;
using EventbriteDotNetFramework.Entities;
using EventbriteDotNetFramework.Responses;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using rocks.kfs.Eventbrite;
using rocks.kfs.Eventbrite.Utility.ExtensionMethods;

namespace RockWeb.Plugins.rocks_kfs.Eventbrite
{
    #region Block Attributes

    [DisplayName( "Eventbrite Settings" )]
    [Category( "KFS > Eventbrite" )]
    [Description( "Allows you to configure any necessary system settings for Eventbrite integration" )]

    #endregion Block Attributes

    #region Block Settings

    [LinkedPage( "Group Detail", "", true, "", "", 0 )]
    [GroupField( "New Group Parent", "Where new groups, if created, will be placed under. This parent group's group type must allow children of the 'New Group Type' setting below.", false )]
    [GroupTypeField( "New Group Type", "Group type to be used when creating new groups.", false )]
    [CustomCheckboxListField( "New Event Statuses", "Which event statuses from Eventbrite would you like to be available for creating new groups?", "live,completed,draft,canceled,started,ended", false, "live,completed,draft,canceled,started,ended" )]
    [BooleanField( "Enable Logging", "Enable logging for Eventbrite sync methods from this block.", false )]
    [BooleanField( "Display Eventbrite Event Name", "Display Eventbrite Event name on the grid of linked events instead of just the Event Id. (Warning, this may cause some performance issues)", false )]

    #endregion Block Settings

    public partial class EventbriteSettings : Rock.Web.UI.RockBlock
    {
        private string _accessToken = null;
        private List<RockEventbriteEvent> _groups = null;

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

            _accessToken = Settings.GetAccessToken();

            if ( !Page.IsPostBack )
            {
                ShowDetail();
                loadEvents();
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
            loadEvents();
        }

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
            pnlCreateGroupFromEventbrite.Visible = false;
            loadOrganizations();
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
            ShowDetail( true );
            loadEvents();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            pnlToken.Visible = false;
            HideSecondaryBlocks( false );
            nbNotification.Visible = false;
            pnlCreateGroupFromEventbrite.Visible = true;
            ShowDetail();
            loadEvents();
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
                var detailPage = GetAttributeValue( "GroupDetail" );

                if ( detailPage.IsNullOrWhiteSpace() )
                {
                    var colIndex = gEBLinkedGroups.GetColumnByHeaderText( "Edit" );
                    e.Row.Cells[gEBLinkedGroups.GetColumnIndex( colIndex )].Visible = false;
                }

                if ( GetAttributeValue( "DisplayEventbriteEventName" ).AsBoolean() )
                {
                    gEBLinkedGroups.GetColumnByHeaderText( "Eventbrite Event Name" ).Visible = true;
                    gEBLinkedGroups.GetColumnByHeaderText( "Eventbrite Event Id" ).Visible = false;
                }
            }
        }

        protected void lbSyncNow_Click( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyValue.ToString().AsInteger();
            rocks.kfs.Eventbrite.Eventbrite.SyncEvent( groupId, EnableLogging: GetAttributeValue( "EnableLogging" ).AsBoolean() );
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
            loadEvents();
        }

        protected void lbCreateNewRockGroup_Click( object sender, EventArgs e )
        {
            var eb = rocks.kfs.Eventbrite.Eventbrite.Api( Settings.GetAccessToken() );
            var ebEventToCreate = ddlEventbriteEvents.SelectedValue.AsLong();
            var EbEvent = eb.GetEventById( ebEventToCreate );
            var parentGroupGuid = GetAttributeValue( "NewGroupParent" ).AsGuidOrNull();
            var groupTypeGuid = GetAttributeValue( "NewGroupType" ).AsGuidOrNull();
            Group newGroup = null;
            if ( parentGroupGuid.HasValue && groupTypeGuid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var groupService = new GroupService( rockContext );
                    var groupType = GroupTypeCache.Get( groupTypeGuid.Value, rockContext );
                    var parentGroup = groupService.Get( parentGroupGuid.Value );
                    if ( groupType != null && parentGroup != null && parentGroup.GroupType.ChildGroupTypes.Select( gt => gt.Id ).Contains( groupType.Id ) )
                    {
                        newGroup = new Group
                        {
                            IsActive = true,
                            CreatedByPersonAliasId = CurrentPersonAlias.Id,
                            CreatedDateTime = RockDateTime.Now,
                            Description = EbEvent.Description.Text != null ? EbEvent.Description.Text : "",
                            Name = string.Format( "{0} - {1}", EbEvent.Start.Local, EbEvent.Name.Text.ToString() ),
                            ParentGroupId = parentGroup.Id,
                            GroupTypeId = groupType.Id
                        };
                        groupService.Add( newGroup );
                        rockContext.SaveChanges();
                    }
                    else
                    {
                        nbLinkNew.Text = "There is an error with your New Group Type or New Group Parent configuration. If the settings are set properly, please ensure the New Group Parent group type allows child groups of the New Group Type specified.";
                        nbLinkNew.Visible = true;
                        return;
                    }
                }
            }
            var linked = false;
            if ( newGroup != null )
            {
                linked = rocks.kfs.Eventbrite.Eventbrite.LinkEvents( newGroup.Id, EbEvent.Id );
            }

            if ( linked )
            {
                ShowDetail();
                loadEvents();
            }
            else
            {
                nbLinkNew.Text = "Failed to link new group. You must manually associate group via the group detail page or try again.";
                nbLinkNew.Visible = true;
            }
        }

        protected void tbToken_TextChanged( object sender, EventArgs e )
        {
            _accessToken = tbOAuthToken.Text;
            loadOrganizations();
        }

        #endregion Events

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail( bool showNotificationbox = false )
        {
            var isAuthenticated = rocks.kfs.Eventbrite.Eventbrite.EBoAuthCheck();
            if ( _accessToken.IsNullOrWhiteSpace() )
            {
                pnlToken.Visible = true;
                HideSecondaryBlocks( true );
                pnlGridWrapper.Visible = false;
                pnlCreateGroupFromEventbrite.Visible = false;
                btnEdit.Visible = false;
            }
            else
            {
                tbOAuthToken.Text = _accessToken;
                if ( isAuthenticated )
                {
                    lblLoginStatus.Text = "Authenticated";
                    lblLoginStatus.CssClass = "pull-right label label-success";
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
                nbNotification.Visible = showNotificationbox;
                pnlGridWrapper.Visible = true;
                lView.Visible = true;
                btnEdit.Visible = true;
                _groups = new EventbriteEvents().Events( GetAttributeValue( "DisplayEventbriteEventName" ).AsBoolean() );
                gEBLinkedGroups.DataSource = _groups;
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

        private void loadEvents()
        {
            var parentGroupGuid = GetAttributeValue( "NewGroupParent" ).AsGuidOrNull();
            var groupTypeGuid = GetAttributeValue( "NewGroupType" ).AsGuidOrNull();
            var newEventStatuses = GetAttributeValue( "NewEventStatuses" ).SplitDelimitedValues( "," ).ToList();

            ddlEventbriteEvents.Items.Clear();
            if ( parentGroupGuid.HasValue && groupTypeGuid.HasValue )
            {
                var eb = rocks.kfs.Eventbrite.Eventbrite.Api( _accessToken );
                var organizationEvents = eb.GetOrganizationEvents( GetAttributeValue( "NewEventStatuses" ) );
                if ( organizationEvents.Pagination.Has_More_Items )
                {
                    var looper = new OrganizationEventsResponse();
                    for ( int i = 2; i <= organizationEvents.Pagination.PageCount; i++ )
                    {
                        looper = eb.GetOrganizationEvents( i, GetAttributeValue( "NewEventStatuses" ) );
                        organizationEvents.Events.AddRange( looper.Events );
                    }
                }
                foreach ( var evnt in organizationEvents.Events.FindAll( e => newEventStatuses.Contains( e.Status ) ) )
                {
                    if ( !_groups.Select( g => g.EventbriteEventId ).ToList().Contains( evnt.Id ) )
                    {
                        ddlEventbriteEvents.Items.Add( new ListItem( string.Format( "{0} - {1} ({2})", evnt.Name.Text.ToString(), evnt.Start.Local, evnt.Status ), evnt.Id.ToString() ) );
                    }
                }
            }
            else
            {
                ddlEventbriteEvents.Visible = false;
                lbCreateNewRockGroup.Visible = false;
                nbLinkNew.Text = "Set the 'New Group Parent' and 'New Group Type' settings if you wish to use this feature.";
                nbLinkNew.NotificationBoxType = NotificationBoxType.Info;
                nbLinkNew.Visible = true;
            }
            if ( ddlEventbriteEvents.Items.Count == 0 )
            {
                ddlEventbriteEvents.Visible = false;
                nbLinkNew.Text = "There are currently no events available to link.";
                nbLinkNew.NotificationBoxType = NotificationBoxType.Warning;
                nbLinkNew.Dismissable = false;
                nbLinkNew.Visible = true;
                lbCreateNewRockGroup.Visible = false;
            }
        }

        #endregion Internal Methods
    }
}