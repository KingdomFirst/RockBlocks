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

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.Eventbrite
{
    #region Block Attributes

    [DisplayName( "Eventbrite Sync Button" )]
    [Category( "KFS > Eventbrite" )]
    [Description( "Allows a sync button to be placed on any group aware page to be able to sync this group with Eventbrite." )]

    #endregion

    #region Block Settings
    #endregion

    public partial class EventbriteSync : Rock.Web.UI.RockBlock, ISecondaryBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlEventbriteButtons );
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
                    pnlEventbriteButtons.Visible = false;
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
                pnlEventbriteButtons.Visible = false;
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
                group = new GroupService( rockContext ).GetNoTracking( groupId );
                RockPage.SaveSharedItem( key, group );
            }

            return group;
        }

        private void ShowPanels( int groupId )
        {
            Group group = null;

            RockContext rockContext = new RockContext();

            if ( !groupId.Equals( 0 ) )
            {
                group = GetGroup( groupId, rockContext );

                pnlEventbriteButtons.Visible = IsUserAuthorized( Authorization.EDIT ) || group.IsAuthorized( Authorization.EDIT, this.CurrentPerson );

                hfGroupId.Value = group.Id.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSync control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSyncButton_Click( object sender, EventArgs e )
        {
            int groupId = hfGroupId.ValueAsInt();

            rocks.kfs.Eventbrite.Eventbrite.SyncEvent( groupId );
        }

        /// <summary>
        /// Hook so that other blocks can set the visibility of all ISecondaryBlocks on it's page
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlEventbriteButtons.Visible = visible;
        }
    }
}
