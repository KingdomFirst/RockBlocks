// <copyright>
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

    public partial class PagerEntry : CheckInBlockMultiPerson
    {
        private new static class AttributeKey
        {
            public const string Title = "TitleTemplate";
            public const string Caption = "Caption";
            public const string DisplayKeypad = "DisplayKeypad";
            public const string PagerAttribute = "PagerAttribute";
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
                if ( !Page.IsPostBack )
                {
                    CheckInFamily family = CurrentCheckInState.CheckIn.CurrentFamily;
                    if ( family != null )
                    {
                        family.Group.LoadAttributes();
                    }
                    else
                    {
                        GoBack();
                    }

                    lTitle.Text = GetTitleText();
                    lbSubmit.Text = "Check In";
                    lbSubmit.Attributes.Add( "data-loading-text", "Printing..." );

                    tbPagerNumber.Label = GetAttributeValue( AttributeKey.Caption );

                    pnlKeypad.Visible = GetAttributeValue( AttributeKey.DisplayKeypad ).AsBoolean();
                }
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

            var timeSelectHeaderLavaTemplate = CurrentCheckInState.CheckInType.TimeSelectHeaderLavaTemplate ?? string.Empty;
            return timeSelectHeaderLavaTemplate.ResolveMergeFields( mergeFields );
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