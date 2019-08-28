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
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.Utility
{
    /// <summary>
    /// Block that allows person attribute value to be set from another entity attribute value.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Person Attribute From Another Entity" )]
    [Category( "KFS > Utility" )]
    [Description( "This block displays a person attribute from another entity attribute." )]

    #endregion

    #region Block Settings

    [TextField( "Source Entity Attribute Key", "The Attribute Key from the source entity that should be evaluated and presented.", order: 0 )]
    [BooleanField( "Load Attribute Value", "If the person has a current attribute value, should it be loaded?", order: 1 )]
    [LinkedPage( "Redirect Page", "Optional page the user should be sent to after saving attribute value", false, order: 2 )]
    [BooleanField( "Checkbox Mode", "Should the attribute be displayed as a simple checkbox?", false, "Mode", 0 )]
    [TextField( "Checkbox Text", "The text that should be placed beside the checkbox.  If left blank, the Attribute Name will be used.", false, "", "Mode", 1 )]
    [CustomDropdownListField( "Checkbox Attribute Value Mode", "The type of value that should be used when saving in Checkbox Mode.", "Date,Boolean", false, "Date", "Mode", 2 )]
    [BooleanField( "Instant Save", "Should the save happen as soon as the box is checked?", false, "Mode", 3 )]
    [TextField( "Panel Heading", "The heading that should be displayed above the attribute.", false, "", "Text Settings", 0 )]
    [TextField( "Save Button Text", "The text that the save button should present.", false, "Save", "Text Settings", 1 )]
    [ContextAware]

    #endregion

    public partial class PersonAttributeFromAnotherEntity : RockBlock, ISecondaryBlock
    {
        #region Base Method Overrides

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
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            IHasAttributes entity = ContextEntity() as IHasAttributes;

            if ( entity == null || entity.Attributes.Count == 0 )
            {
                return;
            }
            else
            {
                ShowDetails();
            }
        }

        /// <summary>
        /// Allow primary blocks to hide this one. This is common when the primary block goes
        /// into edit mode.
        /// </summary>
        /// <param name="visible">true if this block should be visible, false if it should be hidden.</param>
        void ISecondaryBlock.SetVisible( bool visible )
        {
            pnlDetails.Visible = false;
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Shows the read-only attribute values.
        /// </summary>
        protected void ShowDetails()
        {
            var loadAttributeValue = GetAttributeValue( "LoadAttributeValue" ).AsBoolean( false );
            ltTitle.Text = GetAttributeValue( "PanelHeading" );
            btnSave.Text = GetAttributeValue( "SaveButtonText" );

            IHasAttributes entity = ContextEntity() as IHasAttributes;

            if ( entity != null && CurrentPerson != null )
            {
                var rockContext = new RockContext();

                if ( entity.Attributes == null )
                {
                    entity.LoadAttributes();
                }

                if ( CurrentPerson.Attributes == null )
                {
                    CurrentPerson.LoadAttributes();
                }

                var sourceAttributeKey = GetAttributeValue( "SourceEntityAttributeKey" );
                var sourceAttributeValue = entity.GetAttributeValue( sourceAttributeKey );
                var attributeService = new AttributeService( rockContext );
                var targetAttribute = attributeService.GetByGuids( new List<Guid>() { sourceAttributeValue.AsGuid() } ).ToList().FirstOrDefault();

                fsAttributes.Controls.Clear();

                string validationGroup = string.Format( "vgAttributeValues_{0}", this.BlockId );
                btnSave.ValidationGroup = validationGroup;

                var attribute = AttributeCache.Get( targetAttribute );
                string attributeValue = CurrentPerson.GetAttributeValue( attribute.Key );
                string formattedValue = string.Empty;

                if ( GetAttributeValue( "CheckboxMode" ).AsBoolean( false ) )
                {
                    var checkboxText = GetAttributeValue( "CheckboxText" );
                    CheckBox checkbox = new CheckBox();
                    checkbox.ID = "SimpleCheckBox";
                    checkbox.Text = String.IsNullOrWhiteSpace( checkboxText ) ? attribute.Name : checkboxText;

                    if ( GetAttributeValue( "InstantSave" ).AsBoolean( false ) )
                    {
                        checkbox.AutoPostBack = true;

                        checkbox.CheckedChanged += new EventHandler( this.Check_Change );
                        btnSave.Visible = false;
                    }

                    if ( loadAttributeValue && !string.IsNullOrWhiteSpace( attributeValue ) && attributeValue.AsBoolean( true ) )
                    {
                        checkbox.Checked = true;
                    }

                    fsAttributes.Controls.Add( checkbox );
                }
                else
                {
                    attribute.AddControl( fsAttributes.Controls, attributeValue, validationGroup, loadAttributeValue, true );
                }

                pnlDetails.Visible = true;
            }
        }

        protected void SaveDetails()
        {
            var checkValue = false;
            CheckBox check = ( CheckBox ) fsAttributes.FindControl( "SimpleCheckBox" );

            if ( check != null && check.Checked )
            {
                checkValue = true;
            }

            IHasAttributes entity = ContextEntity() as IHasAttributes;

            var rockContext = new RockContext();
            var sourceAttributeKey = GetAttributeValue( "SourceEntityAttributeKey" );
            var sourceAttributeValue = entity.GetAttributeValue( sourceAttributeKey );
            var attributeService = new AttributeService( rockContext );
            var targetAttribute = attributeService.GetByGuids( new List<Guid>() { sourceAttributeValue.AsGuid() } ).ToList().FirstOrDefault();

            int personEntityTypeId = EntityTypeCache.Get( typeof( Person ) ).Id;

            var changes = new History.HistoryChangeList();

            var attribute = AttributeCache.Get( targetAttribute );

            if ( CurrentPerson != null )
            {
                Control attributeControl = fsAttributes.FindControl( string.Format( "attribute_field_{0}", attribute.Id ) );
                if ( GetAttributeValue( "CheckboxMode" ).AsBoolean( false ) || attributeControl != null )
                {
                    string originalValue = CurrentPerson.GetAttributeValue( attribute.Key );
                    string newValue = string.Empty;

                    if ( GetAttributeValue( "CheckboxMode" ).AsBoolean( false ) )
                    {
                        var valueMode = GetAttributeValue( "CheckboxAttributeValueMode" );
                        if ( valueMode == "Date" )
                        {
                            if ( checkValue )
                            {
                                newValue = RockDateTime.Now.ToString();
                            }
                            else
                            {
                                newValue = string.Empty;
                            }
                        }
                        else if ( valueMode == "Boolean" )
                        {
                            if ( checkValue )
                            {
                                newValue = "True";
                            }
                            else
                            {
                                newValue = "False";
                            }
                        }
                    }
                    else
                    {
                        newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                    }

                    Rock.Attribute.Helper.SaveAttributeValue( CurrentPerson, attribute, newValue, rockContext );

                    // Check for changes to write to history
                    if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                    {
                        string formattedOriginalValue = string.Empty;
                        if ( !string.IsNullOrWhiteSpace( originalValue ) )
                        {
                            formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                        }

                        string formattedNewValue = string.Empty;
                        if ( !string.IsNullOrWhiteSpace( newValue ) )
                        {
                            formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                        }

                        History.EvaluateChange( changes, attribute.Name, formattedOriginalValue, formattedNewValue, attribute.FieldType.Field.IsSensitive() );
                    }
                }
            }

            if ( changes.Any() )
            {
                HistoryService.SaveChanges( rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                    CurrentPerson.Id, changes );
            }

            string linkedPage = GetAttributeValue( "RedirectPage" );
            if ( string.IsNullOrWhiteSpace( linkedPage ) )
            {
                ShowDetails();
            }
            else
            {
                var pageParams = new Dictionary<string, string>();
                pageParams.Add( "av", "updated" );
                var pageReference = new Rock.Web.PageReference( linkedPage, pageParams );
                Response.Redirect( pageReference.BuildUrl(), false );
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            SaveDetails();
        }

        /// <summary>
        /// Handles the Click event of the Simple Checkbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Check_Change( Object sender, EventArgs e )
        {
            SaveDetails();
        }

        #endregion
    }
}
