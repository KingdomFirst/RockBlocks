// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// <notice>
// This file contains modifications by Kingdom First Solutions
// and is a derivative work.
//
// Modification (including but not limited to):
// * Adds ability to display list categories
// </notice>
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using NuGet;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Communication
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Communication List Subscribe" )]
    [Category( "KFS > Communication" )]
    [Description( "Block that allows a person to manage the communication lists that they are subscribed to" )]

    #region Block Attributes

    [GroupCategoryField(
        "Communication List Categories",
        Description = "Select the categories of the communication lists to display, or select none to show all that the user is authorized to view.",
        AllowMultiple = true,
        GroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST,
        DefaultValue = Rock.SystemGuid.Category.GROUPTYPE_COMMUNICATIONLIST_PUBLIC,
        IsRequired = false,
        Key = AttributeKey.CommunicationListCategories,
        Order = 1 )]
    [BooleanField(
        "Show Medium Preference",
        Description = "Show the user's current medium preference for each list and allow them to change it.",
        DefaultBooleanValue = true,
        Key = AttributeKey.ShowMediumPreference,
        Order = 2 )]
    [BooleanField(
        "Show List Categories",
        Description = "Show the category of the selected list categories.",
        DefaultBooleanValue = false,
        Key = AttributeKey.ShowCommunicationListCategories,
        Order = 3
        )]
    [GroupCategoryField(
        "Communication List Categories Default Expanded",
        Description = "By default all categories are displayed collapsed. Select the categories of the communication lists to display expanded (open).",
        AllowMultiple = true,
        GroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST,
        IsRequired = false,
        Key = AttributeKey.CommunicationListCategoriesExpanded,
        Order = 4 )]

    #endregion Block Attributes

    public partial class CommunicationListSubscribe : RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string CommunicationListCategories = "CommunicationListCategories";
            public const string ShowMediumPreference = "ShowMediumPreference";
            public const string ShowCommunicationListCategories = "ShowCommunicationListCategories";
            public const string CommunicationListCategoriesExpanded = "CommunicationListCategoriesOpen";
        }

        #endregion Attribute Keys

        #region fields

        /// <summary>
        /// The person's group member record for each CommunicationListId
        /// </summary>
        private Dictionary<int, GroupMember> personCommunicationListsMember = new Dictionary<int, GroupMember>();

        /// <summary>
        /// The show medium preference
        /// </summary>
        private bool showMediumPreference = true;

        #endregion fields

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                BindRepeater();
            }
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindRepeater();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptCommunicationLists control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptCommunicationLists_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            ProcessDatabound( e );
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptCommunicationListCategories control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptCommunicationListCategories_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var category = e.Item.DataItem as Rock.Model.Category;
            if ( category != null )
            {
                var pwCategoryPanel = e.Item.FindControl( "pwCategoryPanel" ) as PanelWidget;
                var openCategoryGuids = this.GetAttributeValue( AttributeKey.CommunicationListCategoriesExpanded ).SplitDelimitedValues().AsGuidList();

                pwCategoryPanel.Expanded = openCategoryGuids.Contains( category.Guid );
                pwCategoryPanel.Title = category.Name;
                pwCategoryPanel.TitleIconCssClass = category.IconCssClass;
                var rptCommunicationLists = pwCategoryPanel.FindControl( "rptCommunicationLists" ) as Repeater;
                rptCommunicationLists.DataSource = GetListsByCategory( category );
                rptCommunicationLists.DataBind();
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptCommunicationListsNoCategories control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptCommunicationListsNoCategories_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            ProcessDatabound( e );
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbCommunicationListIsSubscribed control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbCommunicationListIsSubscribed_CheckedChanged( object sender, EventArgs e )
        {
            var repeaterItem = ( sender as RockCheckBox ).BindingContainer as RepeaterItem;
            SaveChanges( repeaterItem );
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglCommunicationPreference control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglCommunicationPreference_CheckedChanged( object sender, EventArgs e )
        {
            var repeaterItem = ( sender as Toggle ).BindingContainer as RepeaterItem;
            SaveChanges( repeaterItem );
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Processes the databound.
        /// </summary>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        private void ProcessDatabound( RepeaterItemEventArgs e )
        {
            var group = e.Item.DataItem as Rock.Model.Group;
            if ( group != null )
            {
                var hfGroupId = e.Item.FindControl( "hfGroupId" ) as HiddenField;
                hfGroupId.Value = group.Id.ToString();

                var groupDescription = group.Description.IsNullOrWhiteSpace() ? string.Empty : $@"<br>{group.Description}";
                var groupPublicName = group.GetAttributeValue( "PublicName" );

                var cbCommunicationListIsSubscribed = e.Item.FindControl( "cbCommunicationListIsSubscribed" ) as RockCheckBox;
                cbCommunicationListIsSubscribed.Text = $@"<strong>{groupPublicName}</strong>{groupDescription}";
                if ( groupPublicName.IsNullOrWhiteSpace() )
                {
                    cbCommunicationListIsSubscribed.Text = $@"<strong>{group.Name}</strong>{groupDescription}";
                }

                var groupMember = personCommunicationListsMember.GetValueOrNull( group.Id );
                cbCommunicationListIsSubscribed.Checked = groupMember != null && groupMember.GroupMemberStatus == GroupMemberStatus.Active;

                CommunicationType communicationType = CurrentPerson.CommunicationPreference == CommunicationType.SMS ? CommunicationType.SMS : CommunicationType.Email;

                // if GroupMember record has SMS or Email specified, that takes precedence over their Person.CommunicationPreference
                var groupMemberHasSmsOrEmailPreference = groupMember != null && ( groupMember.CommunicationPreference == CommunicationType.SMS || groupMember.CommunicationPreference == CommunicationType.Email );
                if ( groupMemberHasSmsOrEmailPreference )
                {
                    communicationType = groupMember.CommunicationPreference;
                }

                var tglCommunicationPreference = e.Item.FindControl( "tglCommunicationPreference" ) as Toggle;
                tglCommunicationPreference.Checked = communicationType == CommunicationType.Email;
                tglCommunicationPreference.Visible = showMediumPreference;
            }
        }

        /// <summary>
        /// Gets the lists by category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        private List<Group> GetListsByCategory( Category category )
        {
            int communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;
            int? communicationListGroupTypeDefaultRoleId = GroupTypeCache.Get( communicationListGroupTypeId ).DefaultGroupRoleId;

            var rockContext = new RockContext();

            var memberOfList = new GroupMemberService( rockContext ).GetByPersonId( CurrentPersonId.Value ).AsNoTracking().Select( a => a.GroupId ).ToList();

            // Get a list of syncs for the communication list groups where the default role is sync'd AND the current person is NOT a member of
            // This is used to filter out the list of communication lists.
            var commGroupSyncsForDefaultRole = new GroupSyncService( rockContext )
                .Queryable()
                .Where( a => a.Group.GroupTypeId == communicationListGroupTypeId )
                .Where( a => a.GroupTypeRoleId == communicationListGroupTypeDefaultRoleId )
                .Where( a => !memberOfList.Contains( a.GroupId ) )
                .Select( a => a.GroupId )
                .ToList();

            var communicationLists = new GroupService( rockContext )
               .Queryable()
               .Where( a => a.GroupTypeId == communicationListGroupTypeId && !commGroupSyncsForDefaultRole.Contains( a.Id ) )
               .IsActive()
               .ToList();

            var categoryGuids = new List<Guid>();
            categoryGuids.Add( category.Guid );
            var viewableCommunicationLists = new List<Group>();

            foreach ( var communicationList in communicationLists )
            {
                communicationList.LoadAttributes( rockContext );
                if ( !categoryGuids.Any() )
                {
                    // if no categories where specified, only show lists that the person has VIEW auth
                    if ( communicationList.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) )
                    {
                        viewableCommunicationLists.Add( communicationList );
                    }
                }
                else
                {
                    Guid? categoryGuid = communicationList.GetAttributeValue( "Category" ).AsGuidOrNull();
                    if ( categoryGuid.HasValue && categoryGuids.Contains( categoryGuid.Value ) )
                    {
                        viewableCommunicationLists.Add( communicationList );
                    }
                }
            }

            viewableCommunicationLists = viewableCommunicationLists.OrderBy( a =>
            {
                var name = a.GetAttributeValue( "PublicName" );
                if ( name.IsNullOrWhiteSpace() )
                {
                    name = a.Name;
                }

                return name;
            } ).ToList();

            var groupIds = viewableCommunicationLists.Select( a => a.Id ).ToList();
            var personId = this.CurrentPersonId.Value;

            showMediumPreference = this.GetAttributeValue( AttributeKey.ShowMediumPreference ).AsBoolean();

            var localPersonCommunicationListsMember = new GroupMemberService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( a => groupIds.Contains( a.GroupId ) && a.PersonId == personId )
                .GroupBy( a => a.GroupId )
                .ToList()
                .ToDictionary( k => k.Key, v => v.FirstOrDefault() );

            if ( localPersonCommunicationListsMember != null )
            {
                personCommunicationListsMember.AddRange( localPersonCommunicationListsMember );
            }

            nbNoCommunicationLists.Visible = !viewableCommunicationLists.Any();
            pnlCommunicationPreferences.Visible = viewableCommunicationLists.Any();

            return viewableCommunicationLists.OrderBy( l => l.Order ).ThenBy( l => l.Name ).ToList();
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="item">The item.</param>
        protected void SaveChanges( RepeaterItem item )
        {
            var hfGroupId = item.FindControl( "hfGroupId" ) as HiddenField;
            var cbCommunicationListIsSubscribed = item.FindControl( "cbCommunicationListIsSubscribed" ) as RockCheckBox;
            var tglCommunicationPreference = item.FindControl( "tglCommunicationPreference" ) as Toggle;
            var nbGroupNotification = item.FindControl( "nbGroupNotification" ) as NotificationBox;
            nbGroupNotification.Visible = false;

            using ( var rockContext = new RockContext() )
            {
                int groupId = hfGroupId.Value.AsInteger();
                var groupMemberService = new GroupMemberService( rockContext );
                var group = new GroupService( rockContext ).Get( groupId );
                var groupMemberRecordsForPerson = groupMemberService.Queryable().Where( a => a.GroupId == groupId && a.PersonId == this.CurrentPersonId ).ToList();
                if ( groupMemberRecordsForPerson.Any() )
                {
                    // normally there would be at most 1 group member record for the person, but just in case, mark them all
                    foreach ( var groupMember in groupMemberRecordsForPerson )
                    {
                        if ( cbCommunicationListIsSubscribed.Checked )
                        {
                            if ( groupMember.GroupMemberStatus == GroupMemberStatus.Inactive )
                            {
                                groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                                if ( groupMember.Note == "Unsubscribed" )
                                {
                                    groupMember.Note = string.Empty;
                                }
                            }
                        }
                        else
                        {
                            if ( groupMember.GroupMemberStatus == GroupMemberStatus.Active )
                            {
                                groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
                                if ( groupMember.Note.IsNullOrWhiteSpace() )
                                {
                                    groupMember.Note = "Unsubscribed";
                                }
                            }
                        }

                        CommunicationType communicationType = tglCommunicationPreference.Checked ? CommunicationType.Email : CommunicationType.SMS;
                        groupMember.CommunicationPreference = communicationType;
                    }
                }
                else
                {
                    // they are not currently in the Group
                    if ( cbCommunicationListIsSubscribed.Checked )
                    {
                        var groupMember = new GroupMember();
                        groupMember.PersonId = this.CurrentPersonId.Value;
                        groupMember.GroupId = group.Id;
                        int? defaultGroupRoleId = GroupTypeCache.Get( group.GroupTypeId ).DefaultGroupRoleId;
                        if ( defaultGroupRoleId.HasValue )
                        {
                            groupMember.GroupRoleId = defaultGroupRoleId.Value;
                        }
                        else
                        {
                            nbGroupNotification.Text = "Unable to add to group.";
                            nbGroupNotification.Details = "Group has no default group role";
                            nbGroupNotification.NotificationBoxType = NotificationBoxType.Danger;
                            nbGroupNotification.Visible = true;
                        }

                        groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                        CommunicationType communicationType = tglCommunicationPreference.Checked ? CommunicationType.Email : CommunicationType.SMS;
                        groupMember.CommunicationPreference = communicationType;

                        if ( groupMember.IsValidGroupMember( rockContext ) )
                        {
                            groupMemberService.Add( groupMember );
                            rockContext.SaveChanges();
                        }
                        else
                        {
                            // if the group member couldn't be added (for example, one of the group membership rules didn't pass), add the validation messages to the errormessages
                            nbGroupNotification.Text = "Unable to add to group.";
                            nbGroupNotification.Details = groupMember.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                            nbGroupNotification.NotificationBoxType = NotificationBoxType.Danger;
                            nbGroupNotification.Visible = true;
                        }
                    }
                }

                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Binds the repeater.
        /// </summary>
        protected void BindRepeater()
        {
            if ( this.CurrentPersonId == null )
            {
                return;
            }

            var rockContext = new RockContext();

            var categoryGuids = this.GetAttributeValue( AttributeKey.CommunicationListCategories ).SplitDelimitedValues().AsGuidList();

            var categories = new CategoryService( rockContext ).GetByGuids( categoryGuids ).OrderBy( c => c.Order ).ThenBy( c => c.Name ).ToList();

            var showCategory = this.GetAttributeValue( AttributeKey.ShowCommunicationListCategories ).AsBoolean();

            if ( showCategory )
            {
                rptCommunicationListCategories.Visible = true;
                rptCommunicationListsNoCategories.Visible = false;
                rptCommunicationListCategories.DataSource = categories;
                rptCommunicationListCategories.DataBind();
            }
            else
            {
                rptCommunicationListCategories.Visible = false;
                rptCommunicationListsNoCategories.Visible = true;
                var lists = new List<Group>();
                foreach ( var category in categories )
                {
                    lists.AddRange( GetListsByCategory( category ) );
                }
                rptCommunicationListsNoCategories.DataSource = lists.OrderBy( g => g.Name );
                rptCommunicationListsNoCategories.DataBind();
            }
        }

        #endregion Methods
    }
}