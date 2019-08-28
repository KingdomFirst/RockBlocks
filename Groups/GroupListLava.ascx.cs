// <copyright>
// Copyright 2019 by Kingdom First Solutions
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
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.RsvpGroups
{
    #region Block Attributes

    [DisplayName( "Group List Lava" )]
    [Category( "KFS > Groups" )]
    [Description( "Lists Groups for lava display." )]

    #endregion

    #region Block Settings

    [LinkedPage( "Detail Page", "", true, "", "", 0 )]
    [GroupField( "Parent Group", "If a group is chosen, only the groups under this group will be displayed.", false, order: 1 )]
    [GroupTypesField( "Include Group Types", "The group types to display in the list.", true, "", "", 2 )]
    [CodeEditorField( "Lava Template", "The lava template to use to format the group list.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, "{% include '~/Plugins/rocks_kfs/Groups/Lava/GroupList.lava' %}", "", 3 )]
    [BooleanField( "Display Inactive Groups", "Include inactive groups in the lava results", false, order: 4 )]
    [CustomDropdownListField( "Initial Active Setting", "Select whether to initially show all or just active groups in the lava", "0^All,1^Active", false, "1", "", 5 )]
    [TextField( "Inactive Parameter Name", "The page parameter name to toggle inactive groups", false, "showinactivegroups", order: 6 )]

    #endregion

    public partial class RsvpGroupList : RockBlock
    {
        #region Fields

        private bool _hideInactive = true;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;

            if ( this.GetAttributeValue( "DisplayInactiveGroups" ).AsBoolean() )
            {
                var hideInactiveGroups = this.GetAttributeValue( "HideInactiveGroups" ).AsBooleanOrNull();
                if ( !hideInactiveGroups.HasValue )
                {
                    hideInactiveGroups = this.GetAttributeValue( "InitialActiveSetting" ) == "1";
                }

                _hideInactive = hideInactiveGroups ?? true;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                var inactiveGroups = PageParameter( GetAttributeValue( "InactiveParameterName" ) ).AsBooleanOrNull();
                if ( this.GetAttributeValue( "DisplayInactiveGroups" ).AsBoolean() && inactiveGroups.HasValue )
                {
                    _hideInactive = !inactiveGroups ?? true;
                }
                ListGroups();
            }

            base.OnLoad( e );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the GroupList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ListGroups();
        }

        #endregion

        #region Internal Methods

        private void ListGroups()
        {
            List<Guid> includeGroupTypeGuids = GetAttributeValue( "IncludeGroupTypes" ).SplitDelimitedValues().Select( a => Guid.Parse( a ) ).ToList();

            if ( includeGroupTypeGuids.Any() )
            {
                RockContext rockContext = new RockContext();

                var qry = new GroupService( rockContext ).Queryable().Where( g => g.IsPublic == true );

                var parentGroupGuid = GetAttributeValue( "ParentGroup" ).AsGuidOrNull();
                if ( parentGroupGuid != null )
                {
                    var availableGroupIds = new List<int>();

                    var parentGroup = new GroupService( rockContext ).Get( parentGroupGuid ?? new Guid() );
                    if ( parentGroup != null )
                    {
                        availableGroupIds = GetChildGroups( parentGroup ).Select( g => g.Id ).ToList();
                    }
                    else
                    {
                        availableGroupIds = new List<int>();
                    }

                    qry = qry.Where( g => availableGroupIds.Contains( g.Id ) );
                }

                if ( _hideInactive )
                {
                    qry = qry.Where( g => g.IsActive == true );
                }

                if ( includeGroupTypeGuids.Count > 0 )
                {
                    qry = qry.Where( g => includeGroupTypeGuids.Contains( g.GroupType.Guid ) );
                }

                var groups = new List<Group>();

                foreach ( var group in qry.ToList() )
                {
                    groups.Add( group );
                }

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Groups", groups );

                Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                linkedPages.Add( "DetailPage", LinkedPageRoute( "DetailPage" ) );
                mergeFields.Add( "LinkedPages", linkedPages );

                if ( this.GetAttributeValue( "DisplayInactiveGroups" ).AsBoolean() )
                {
                    mergeFields.Add( "ShowInactive", this.GetAttributeValue( "DisplayInactiveGroups" ) );
                    mergeFields.Add( "InitialActive", this.GetAttributeValue( "InitialActiveSetting" ) );
                    mergeFields.Add( "InactiveParameter", this.GetAttributeValue( "InactiveParameterName" ) );
                }

                string template = GetAttributeValue( "LavaTemplate" );

                lContent.Text = template.ResolveMergeFields( mergeFields );
            }
        }

        /// <summary>
        /// Recursively loads all descendants of the group
        /// </summary>
        /// <param name="group">Group to load from</param>
        /// <returns></returns>
        private List<Group> GetChildGroups( Group group )
        {
            List<Group> childGroups = group.Groups.ToList();
            List<Group> grandChildGroups = new List<Group>();
            foreach ( var childGroup in childGroups )
            {
                grandChildGroups.AddRange( GetChildGroups( childGroup ) );
            }
            childGroups.AddRange( grandChildGroups );
            return childGroups;
        }

        #endregion
    }
}
