using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Groups
{
    [DisplayName( "Group Copy Button" )]
    [Category( "KFS > Groups" )]
    [Description( "Allows a copy button to be placed on any group aware page to be able to copy the group to a new one, such as group leader toolbox." )]
    [BooleanField( "Copy Location", "Copies the location of the existing group to the new group as well.", true, "" )]

    public partial class GroupCopy : Rock.Web.UI.RockBlock, ISecondaryBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlGroupCopy );

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
                string groupId = PageParameter( "GroupId" );
                if ( !string.IsNullOrWhiteSpace( groupId ) )
                {
                    ShowPanels( groupId.AsInteger() );
                }
                else
                {
                    pnlGroupCopy.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            string groupId = PageParameter( "GroupId" );
            if ( !string.IsNullOrWhiteSpace( groupId ) )
            {
                ShowPanels( groupId.AsInteger() );
            }
            else
            {
                pnlGroupCopy.Visible = false;
            }
        }

        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        private Group GetGroup( int groupId, RockContext rockContext = null )
        {
            string key = string.Format( "Group:{0}", groupId );
            Group group = RockPage.GetSharedItem( key ) as Group;
            if ( group == null )
            {
                rockContext = rockContext ?? new RockContext();
                group = new GroupService( rockContext ).Queryable( "GroupType,GroupLocations.Schedules" )
                    .Where( g => g.Id == groupId )
                    .FirstOrDefault();
                RockPage.SaveSharedItem( key, group );
            }

            return group;
        }

        /// <summary>
        /// Determines if the current user is a leader in the group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        private bool EnableCopy( int groupId, RockContext rockContext = null )
        {
            var isLeader = false;

            string key = string.Format( "Group:{0}", groupId );
            Group group = RockPage.GetSharedItem( key ) as Group;
            if ( group == null )
            {
                rockContext = rockContext ?? new RockContext();
                group = new GroupService( rockContext ).Queryable( "GroupType,GroupLocations.Schedules" )
                    .Where( g => g.Id == groupId )
                    .FirstOrDefault();
                RockPage.SaveSharedItem( key, group );
            }
            
            foreach ( var member in group.Members )
            {
                if ( member.PersonId.Equals( CurrentPersonId ) && member.GroupRole.IsLeader )
                {
                    isLeader = true;
                }
            }

            return isLeader;
        }

        private void ShowPanels( int groupId )
        {
            Group group = null;

            RockContext rockContext = new RockContext();

            if ( !groupId.Equals( 0 ) )
            {
                group = GetGroup( groupId, rockContext );

                pnlGroupCopy.Visible = EnableCopy( groupId, rockContext );

                hfGroupId.Value = group.Id.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCopy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCopyButton_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var authService = new AuthService( rockContext );
            var attributeService = new AttributeService( rockContext );

            int groupId = hfGroupId.ValueAsInt();
            var group = groupService.Queryable( "GroupType" )
                    .Where( g => g.Id == groupId )
                    .FirstOrDefault();

            if ( group != null )
            {
                group.LoadAttributes( rockContext );

                // clone the group
                var newGroup = group.Clone( false );
                newGroup.CreatedByPersonAlias = null;
                newGroup.CreatedByPersonAliasId = null;
                newGroup.CreatedDateTime = RockDateTime.Now;
                newGroup.ModifiedByPersonAlias = null;
                newGroup.ModifiedByPersonAliasId = null;
                newGroup.ModifiedDateTime = RockDateTime.Now;
                newGroup.Id = 0;
                newGroup.Guid = Guid.NewGuid();
                newGroup.IsSystem = false;
                newGroup.Name = group.Name + " - Copy";

                if ( GetAttributeValue( "CopyLocation" ).AsBoolean( true ) )
                {
                    foreach ( GroupLocation location in group.GroupLocations )
                    {
                        newGroup.GroupLocations.Add( location );
                    }
                }

                var auths = authService.GetByGroup( group.Id );
                rockContext.WrapTransaction( () =>
                {
                    groupService.Add( newGroup );
                    rockContext.SaveChanges();

                    newGroup.LoadAttributes( rockContext );
                    if ( group.Attributes != null && group.Attributes.Any() )
                    {
                        foreach ( var attributeKey in group.Attributes.Select( a => a.Key ) )
                        {
                            string value = group.GetAttributeValue( attributeKey );
                            newGroup.SetAttributeValue( attributeKey, value );
                        }
                    }

                    newGroup.SaveAttributeValues( rockContext );

                    /* Take care of Group Member Attributes */
                    var entityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id;
                    string qualifierColumn = "GroupId";
                    string qualifierValue = group.Id.ToString();

                    // Get the existing attributes for this entity type and qualifier value
                    var attributes = attributeService.Get( entityTypeId, qualifierColumn, qualifierValue );

                    foreach ( var attribute in attributes )
                    {
                        var newAttribute = attribute.Clone( false );
                        newAttribute.Id = 0;
                        newAttribute.Guid = Guid.NewGuid();
                        newAttribute.IsSystem = false;
                        newAttribute.EntityTypeQualifierValue = newGroup.Id.ToString();

                        foreach ( var qualifier in attribute.AttributeQualifiers )
                        {
                            var newQualifier = qualifier.Clone( false );
                            newQualifier.Id = 0;
                            newQualifier.Guid = Guid.NewGuid();
                            newQualifier.IsSystem = false;

                            newAttribute.AttributeQualifiers.Add( qualifier );
                        }

                        attributeService.Add( newAttribute );
                    }

                    rockContext.SaveChanges();

                    var person = CurrentPerson;
                    GroupMember currentGroupMember = null;
                    if ( person != null )
                    {
                        currentGroupMember = group.Members.Where( gm => gm.PersonId == person.Id ).FirstOrDefault();
                    }
                    if ( currentGroupMember != null )
                    {
                        var newGroupMember = currentGroupMember.Clone( false );
                        newGroupMember.Id = 0;
                        newGroupMember.Guid = Guid.NewGuid();
                        newGroupMember.IsSystem = false;
                        newGroup.Members.Add( newGroupMember );
                    }

                    rockContext.SaveChanges();

                    foreach ( var auth in auths )
                    {
                        var newAuth = auth.Clone( false );
                        newAuth.Id = 0;
                        newAuth.Guid = Guid.NewGuid();
                        newAuth.GroupId = newGroup.Id;
                        newAuth.CreatedByPersonAlias = null;
                        newAuth.CreatedByPersonAliasId = null;
                        newAuth.CreatedDateTime = RockDateTime.Now;
                        newAuth.ModifiedByPersonAlias = null;
                        newAuth.ModifiedByPersonAliasId = null;
                        newAuth.ModifiedDateTime = RockDateTime.Now;
                        authService.Add( newAuth );
                    }

                    rockContext.SaveChanges();
                    Rock.Security.Authorization.Clear();
                } );

                NavigateToCurrentPage( new Dictionary<string, string> { { "GroupId", newGroup.Id.ToString() } } );
            }
        }
        /// <summary>
        /// Hook so that other blocks can set the visibility of all ISecondaryBlocks on it's page
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlGroupCopy.Visible = visible;
        }
    }
}