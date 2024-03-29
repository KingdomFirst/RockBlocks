﻿// <copyright>
// Copyright 2022 by Kingdom First Solutions
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
using System.Web.UI.HtmlControls;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.rocks_kfs.CheckIn
{
    [DisplayName( "Pager Entry" )]
    [Category( "KFS > Check-in" )]
    [Description( "Displays a prompt for pager number entry." )]

    [LavaField( "Title Template",
        Key = AttributeKey.Title,
        IsRequired = false,
        DefaultValue = "{{ Family.Name }}",
        Category = "Text",
        Order = 5 )]

    [TextField( "Caption",
        Key = AttributeKey.Caption,
        IsRequired = false,
        DefaultValue = "Please enter the pager number",
        Category = "Text",
        Order = 6 )]

    [BooleanField( "Display Keypad",
        Key = AttributeKey.DisplayKeypad,
        IsRequired = false,
        Description = "If your pager id's are numbers only and you have touch screen kiosks you can enable a touch screen keypad.",
        DefaultBooleanValue = false,
        Category = "Options",
        Order = 7 )]

    [TextField( "Pager Attribute Key",
        Key = AttributeKey.PagerAttribute,
        IsRequired = true,
        Description = "Attribute Key on Family Group type for Pager.",
        DefaultValue = "rocks.kfs.PagerNumber",
        Category = "Options",
        Order = 8 )]

    [CheckinConfigurationTypeField( "Check-in Type",
        Key = AttributeKey.CheckinType,
        Description = "Select the check-in type(s) to utilize this capability. This capability will be displayed for all check-in types by default.",
        Category = "Options",
        Order = 9 )]

    [CustomCheckboxListField( "Groups",
        Key = AttributeKey.Groups,
        ListSource = "SELECT g.Guid Value, g.Name as Text FROM [Group] g  JOIN GroupType gt ON g.GroupTypeId = gt.Id LEFT JOIN GroupType igt ON igt.Id = gt.InheritedGroupTypeId LEFT JOIN GroupType igt2 ON igt2.Id = igt.InheritedGroupTypeId WHERE gt.Id = 15 OR igt.Id = 15 OR igt2.Id = 15",
        Description = "Select the check-in group(s) to utilize this pager entry capability. This capability will be displayed for all groups by default.",
        Category = "Options",
        Order = 10 )]

    public partial class PagerEntry : CheckInBlockMultiPerson
    {
        private new static class AttributeKey
        {
            public const string Title = "TitleTemplate";
            public const string Caption = "Caption";
            public const string DisplayKeypad = "DisplayKeypad";
            public const string PagerAttribute = "PagerAttribute";
            public const string CheckinType = "CheckinType";
            public const string Groups = "Groups";
            public const string MultiPersonFirstPage = CheckInBlockMultiPerson.AttributeKey.MultiPersonFirstPage;
            public const string MultiPersonDonePage = CheckInBlockMultiPerson.AttributeKey.MultiPersonDonePage;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/checkin-core.js" );

            var bodyTag = this.Page.Master.FindControl( "bodyTag" ) as HtmlGenericControl;
            if ( bodyTag != null )
            {
                bodyTag.AddCssClass( "checkin-pagerentry-bg" );
            }

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
            }
            else
            {
                CheckInFamily currentFamily = CurrentCheckInState.CheckIn.CurrentFamily;
                if ( currentFamily != null )
                {
                    currentFamily.Group.LoadAttributes();
                }
                else
                {
                    GoBack();
                }
                var checkinTypes = GetAttributeValues( AttributeKey.CheckinType ).AsGuidList();
                if ( checkinTypes.Any() && LocalDeviceConfig.CurrentCheckinTypeId.HasValue )
                {
                    var checkinTypeGroup = GroupTypeCache.Get( LocalDeviceConfig.CurrentCheckinTypeId.Value );
                    if ( !checkinTypes.Contains( checkinTypeGroup.Guid ) )
                    {
                        GoToNextPage();
                    }
                }

                var groupsSetting = GetAttributeValues( AttributeKey.Groups ).AsGuidList();
                if ( groupsSetting.Any() && CurrentCheckInState.CheckIn.Families.Any( f => f.Selected ) )
                {
                    var family = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
                    if ( family != null )
                    {
                        var people = family.People.Where( p => p.Selected );
                        var redirectToNextPage = true;
                        foreach ( var person in people )
                        {
                            var groupTypes = person.GroupTypes;
                            foreach ( var groupType in groupTypes )
                            {
                                var groups = groupType.Groups;
                                foreach ( var group in groups )
                                {
                                    if ( groupsSetting.Contains( group.Group.Guid ) )
                                    {
                                        redirectToNextPage = false;
                                    }
                                }
                            }
                        }
                        if ( redirectToNextPage )
                        {
                            GoToNextPage();
                        }
                    }
                }

                lTitle.Text = GetTitleText();
                lbSubmit.Text = "Check In";
                lbSubmit.Attributes.Add( "data-loading-text", "Printing..." );

                tbPagerNumber.Label = GetAttributeValue( AttributeKey.Caption );

                pnlKeypad.Visible = GetAttributeValue( AttributeKey.DisplayKeypad ).AsBoolean();
            }
        }

        private string GetTitleText()
        {
            // The checkinPerson, selectedGroup, and selectedLocation are only needed for individual checkins, so no use running the queries if this is a mutli person checkin.
            var checkinPerson = CurrentCheckInState.CheckInType.TypeOfCheckin == TypeOfCheckin.Individual
                ? CurrentCheckInState.CheckIn.Families
                    .Where( f => f.Selected )
                    .SelectMany( f => f.People.Where( p => p.Selected ) )
                    .FirstOrDefault()
                : null;

            var selectedGroup = checkinPerson?.GroupTypes
                .Where( t => t.Selected )
                .SelectMany( t => t.Groups.Where( g => g.Selected ) )
                .FirstOrDefault();

            var selectedLocation = selectedGroup?.Locations.Where( l => l.Selected ).FirstOrDefault()?.Location;

            var selectedIndividuals = CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Selected == true ).Select( p => p.Person );

            var mergeFields = new Dictionary<string, object>
            {
                { LavaMergeFieldName.Family, CurrentCheckInState.CheckIn.CurrentFamily.Group },
                { LavaMergeFieldName.SelectedIndividuals, selectedIndividuals },
                { LavaMergeFieldName.CheckinType, CurrentCheckInState.CheckInType.TypeOfCheckin },
                { LavaMergeFieldName.SelectedGroup, selectedGroup?.Group },
                { LavaMergeFieldName.SelectedLocation, selectedLocation },
            };

            var headerLavaTemplate = GetAttributeValue( AttributeKey.Title ) ?? string.Empty;
            return headerLavaTemplate.ResolveMergeFields( mergeFields );
        }

        protected void lbSubmit_Click( object sender, EventArgs e )
        {
            if ( KioskCurrentlyActive )
            {
                CheckInFamily family = CurrentCheckInState.CheckIn.CurrentFamily;
                if ( family != null )
                {
                    if ( tbPagerNumber.Text.IsNotNullOrWhiteSpace() )
                    {
                        family.Group.SetAttributeValue( GetAttributeValue( AttributeKey.PagerAttribute ), tbPagerNumber.Text );
                        family.Group.SaveAttributeValue( GetAttributeValue( AttributeKey.PagerAttribute ) );

                        GoToNextPage();
                    }
                    else
                    {
                        maWarning.Show( "Please enter a pager number to proceed.", Rock.Web.UI.Controls.ModalAlertType.None );
                    }
                }
                else
                {
                    maWarning.Show( "We're sorry, your family could not be loaded. Please try again.", Rock.Web.UI.Controls.ModalAlertType.None );
                }
            }
        }

        private void GoToNextPage()
        {
            if ( CurrentCheckInState.CheckInType.TypeOfCheckin == TypeOfCheckin.Family )
            {
                var queryParams = CheckForOverride();
                NavigateToLinkedPage( AttributeKey.MultiPersonDonePage, queryParams );
            }
            else
            {
                base.NavigateToNextPage();
            }
        }

        protected void lbBack_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState.CheckInType.TypeOfCheckin == TypeOfCheckin.Family )
            {
                var queryParams = CheckForOverride();
                NavigateToLinkedPage( AttributeKey.MultiPersonFirstPage, queryParams );
            }
            else
            {
                GoBack();
            }
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            CancelCheckin();
        }
    }
}