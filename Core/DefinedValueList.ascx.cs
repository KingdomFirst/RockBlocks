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
// * Added filters to Grid
// * Added sorting to Grid
// * Removed reordering due to sorting
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Core
{
    /// <summary>
    /// User controls for managing defined values
    /// </summary>
    [DisplayName( "Defined Value List" )]
    [Category( "KFS > Core" )]
    [Description( "Block for viewing values for a defined type with filters." )]

    [DefinedTypeField( "Defined Type",
        Description = "If a Defined Type is set, only its Defined Values will be displayed (regardless of the querystring parameters).",
        IsRequired = false,
        Key = AttributeKey.DefinedType )]

    public partial class DefinedValueList : RockBlock, ISecondaryBlock, ICustomGridColumns
    {
        public static class AttributeKey
        {
            public const string DefinedType = "DefinedType";
        }

        #region Private Variables

        private DefinedType _definedType = null;
        private bool _canEdit = false;

        #endregion

        #region Public Variables

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public List<AttributeCache> AvailableAttributes { get; set; }

        #endregion

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
            this.AddConfigurationUpdateTrigger( upnlSettings );

            int definedTypeId = InitForDefinedType();

            _definedType = new DefinedTypeService( new RockContext() ).Get( definedTypeId );

            if ( _definedType != null )
            {
                gfDefinedValues.ApplyFilterClick += gfDefinedValues_ApplyFilterClick;
                gfDefinedValues.DisplayFilterValue += gfDefinedValues_DisplayFilterValue;

                gDefinedValues.DataKeyNames = new string[] { "Id" };
                gDefinedValues.Actions.ShowAdd = true;
                gDefinedValues.Actions.AddClick += gDefinedValues_Add;
                gDefinedValues.GridRebind += gDefinedValues_GridRebind;

                _canEdit = IsUserAuthorized( Authorization.EDIT );
                gDefinedValues.Actions.ShowAdd = _canEdit;
                gDefinedValues.IsDeleteEnabled = _canEdit;

                modalValue.SaveClick += btnSaveValue_Click;
                modalValue.OnCancelScript = string.Format( "$('#{0}').val('');", hfDefinedValueId.ClientID );

                lTitle.Text = _definedType.Name;
            }
            else
            {
                lTitle.Text = "Values";
            }
        }

        /// <summary>
        /// Initialize items for the grid based on the configured or given defined type.
        /// </summary>
        private int InitForDefinedType()
        {
            Guid definedTypeGuid;
            int definedTypeId = 0;

            // A configured defined type takes precedence over any definedTypeId param value that is passed in.
            if ( Guid.TryParse( GetAttributeValue( AttributeKey.DefinedType ), out definedTypeGuid ) )
            {
                definedTypeId = DefinedTypeCache.Get( definedTypeGuid ).Id;
            }
            else
            {
                definedTypeId = PageParameter( "DefinedTypeId" ).AsInteger();
            }

            return definedTypeId;
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
                if ( _definedType != null )
                {
                    SetFilter();
                    ShowDetail();
                }
                else
                {
                    pnlList.Visible = false;
                }
            }
        }

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAttributes = ViewState["AvailableAttributes"] as List<AttributeCache>;

            AddAttributeColumns();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AvailableAttributes"] = AvailableAttributes;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            InitForDefinedType();

            if ( _definedType != null )
            {
                SetFilter();
                ShowDetail();
            }
            else
            {
                pnlList.Visible = false;
            }
        }

        /// <summary>
        /// Handles the Add event of the gDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gDefinedValues_Add( object sender, EventArgs e )
        {
            gDefinedValues_ShowEdit( 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gDefinedValues_Edit( object sender, RowEventArgs e )
        {
            gDefinedValues_ShowEdit( e.RowKeyId );
        }

        /// <summary>
        /// Handles the Delete event of the gDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gDefinedValues_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            var definedValueService = new DefinedValueService( rockContext );

            DefinedValue value = definedValueService.Get( e.RowKeyId );

            if ( value != null )
            {
                string errorMessage;
                if ( !definedValueService.CanDelete( value, out errorMessage ) )
                {
                    mdGridWarningValues.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                definedValueService.Delete( value );
                rockContext.SaveChanges();
            }

            BindDefinedValuesGrid();
        }

        protected void gfDefinedValues_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {

            if ( AvailableAttributes != null )
            {
                var attribute = AvailableAttributes.FirstOrDefault( a => MakeKeyUniqueToType( a.Key ) == e.Key );
                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch { }
                }
            }

            if ( e.Key == MakeKeyUniqueToType( "Value" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToType( "Description" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToType( "Active" ) )
            {
                return;
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        protected void gfDefinedValues_ApplyFilterClick( object sender, EventArgs e )
        {
            gfDefinedValues.SaveUserPreference( MakeKeyUniqueToType( "Value" ), "Value", tbValue.Text );
            gfDefinedValues.SaveUserPreference( MakeKeyUniqueToType( "Description" ), "Description", tbDescription.Text );
            gfDefinedValues.SaveUserPreference( MakeKeyUniqueToType( "Active" ), "Active", cbActive.Checked.ToString() );

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        try
                        {
                            var values = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            gfDefinedValues.SaveUserPreference( MakeKeyUniqueToType( attribute.Key ), attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                        }
                        catch { }
                    }
                }
            }

            BindDefinedValuesGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfDefinedValues_ClearFilterClick( object sender, EventArgs e )
        {
            gfDefinedValues.DeleteUserPreferences();
            cbActive.Checked = true;
            gfDefinedValues.SaveUserPreference( MakeKeyUniqueToType( "Active" ), "Active", cbActive.Checked.ToString() );
            SetFilter();
        }

        /// <summary>
        /// Handles the Click event of the btnSaveDefinedValue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSaveValue_Click( object sender, EventArgs e )
        {
            DefinedValue definedValue;
            var rockContext = new RockContext();
            DefinedValueService definedValueService = new DefinedValueService( rockContext );

            int definedValueId = hfDefinedValueId.ValueAsInt();

            if ( definedValueId.Equals( 0 ) )
            {
                int definedTypeId = hfDefinedTypeId.ValueAsInt();
                definedValue = new DefinedValue { Id = 0 };
                definedValue.DefinedTypeId = definedTypeId;
                definedValue.IsSystem = false;

                var orders = definedValueService.Queryable()
                    .Where( d => d.DefinedTypeId == definedTypeId )
                    .Select( d => d.Order )
                    .ToList();

                definedValue.Order = orders.Any() ? orders.Max() + 1 : 0;
            }
            else
            {
                definedValue = definedValueService.Get( definedValueId );
            }

            definedValue.Value = tbValueName.Text;
            definedValue.Description = tbValueDescription.Text;
            definedValue.IsActive = cbValueActive.Checked;
            avcDefinedValueAttributes.GetEditValues( definedValue );

            if ( !Page.IsValid )
            {
                return;
            }

            if ( !definedValue.IsValid )
            {
                // Controls will render the error messages                    
                return;
            }

            rockContext.WrapTransaction( () =>
            {
                if ( definedValue.Id.Equals( 0 ) )
                {
                    definedValueService.Add( definedValue );
                }

                rockContext.SaveChanges();

                definedValue.SaveAttributeValues( rockContext );

            } );

            BindDefinedValuesGrid();

            hfDefinedValueId.Value = string.Empty;
            modalValue.Hide();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        public void ShowDetail()
        {
            pnlList.Visible = true;

            hfDefinedTypeId.SetValue( _definedType.Id );

            BindDefinedValuesGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gDefinedValues_GridRebind( object sender, EventArgs e )
        {
            BindDefinedValuesGrid();
        }

        private void BindAttributes()
        {
            // Parse the attribute filters
            AvailableAttributes = new List<AttributeCache>();
            if ( _definedType != null )
            {
                int entityTypeId = new DefinedValue().TypeId;
                string qualifier = _definedType.Id.ToString();
                foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == entityTypeId &&
                        a.IsGridColumn &&
                        a.EntityTypeQualifierColumn.Equals( "DefinedTypeId", StringComparison.OrdinalIgnoreCase ) &&
                        a.EntityTypeQualifierValue.Equals( qualifier ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                }
            }
        }

        /// <summary>
        /// Binds the defined values grid.
        /// </summary>
        protected void AddAttributeColumns()
        {
            // Remove attribute columns
            foreach ( var column in gDefinedValues.Columns.OfType<AttributeField>().ToList() )
            {
                gDefinedValues.Columns.Remove( column );
            }

            // Clear the filter controls
            phAttributeFilters.Controls.Clear();

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                    if ( control != null )
                    {
                        if ( control is IRockControl )
                        {
                            var rockControl = ( IRockControl ) control;
                            rockControl.Label = attribute.Name;
                            rockControl.Help = attribute.Description;
                            phAttributeFilters.Controls.Add( control );
                        }
                        else
                        {
                            var wrapper = new RockControlWrapper();
                            wrapper.ID = control.ID + "_wrapper";
                            wrapper.Label = attribute.Name;
                            wrapper.Controls.Add( control );
                            phAttributeFilters.Controls.Add( wrapper );
                        }
                    }

                    string savedValue = gfDefinedValues.GetUserPreference( MakeKeyUniqueToType( attribute.Key ) );
                    if ( !string.IsNullOrWhiteSpace( savedValue ) )
                    {
                        try
                        {
                            var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                            attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                        }
                        catch { }
                    }

                    string dataFieldExpression = attribute.Key;
                    bool columnExists = gDefinedValues.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = dataFieldExpression;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.HeaderStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gDefinedValues.Columns.Add( boundField );
                    }
                }
            }

            if ( _canEdit )
            {
                var deleteField = new DeleteField();
                gDefinedValues.Columns.Add( deleteField );
                deleteField.Click += gDefinedValues_Delete;
            }
        }

        /// <summary>
        /// Binds the defined values grid.
        /// </summary>
        protected void BindDefinedValuesGrid()
        {
            if ( _definedType != null )
            {
                var queryable = new DefinedValueService( new RockContext() ).Queryable().Where( a => a.DefinedTypeId == _definedType.Id ).OrderBy( a => a.Order );
                var result = queryable.ToList();

                gDefinedValues.DataSource = result;
                gDefinedValues.DataBind();
            }
        }

        /// <summary>
        /// Shows the edit value.
        /// </summary>
        /// <param name="valueId">The value id.</param>
        protected void gDefinedValues_ShowEdit( int valueId )
        {
            ShowDefinedValueEdit( valueId );
        }

        private void ShowDefinedValueEdit( int valueId )
        {
            var definedType = DefinedTypeCache.Get( hfDefinedTypeId.ValueAsInt() );
            DefinedValue definedValue;

            modalValue.SubTitle = String.Format( "Id: {0}", valueId );

            if ( !valueId.Equals( 0 ) )
            {
                definedValue = new DefinedValueService( new RockContext() ).Get( valueId );
                if ( definedType != null )
                {
                    lActionTitleDefinedValue.Text = ActionTitle.Edit( "defined value for " + definedType.Name );
                }
            }
            else
            {
                definedValue = new DefinedValue { Id = 0 };
                definedValue.DefinedTypeId = hfDefinedTypeId.ValueAsInt();
                if ( definedType != null )
                {
                    lActionTitleDefinedValue.Text = ActionTitle.Add( "defined value for " + definedType.Name );
                }
            }


            hfDefinedValueId.SetValue( definedValue.Id );
            tbValueName.Text = definedValue.Value;
            tbValueDescription.Text = definedValue.Description;
            cbValueActive.Checked = definedValue.IsActive;

            avcDefinedValueAttributes.ValidationGroup = modalValue.ValidationGroup;
            avcDefinedValueAttributes.AddEditControls( definedValue );

            modalValue.Show();
        }

        private string MakeKeyUniqueToType( string key )
        {
            if ( _definedType != null )
            {
                return string.Format( "{0}-{1}", _definedType.Id, key );
            }
            return key;
        }

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            BindAttributes();
            AddAttributeColumns();

            tbValue.Text = gfDefinedValues.GetUserPreference( MakeKeyUniqueToType( "Name" ) );
            tbDescription.Text = gfDefinedValues.GetUserPreference( MakeKeyUniqueToType( "Status" ) );
            cbActive.Checked = gfDefinedValues.GetUserPreference( MakeKeyUniqueToType( "Active" ) ).AsBoolean();
        }

        #endregion

        #region ISecondaryBlock

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion
    }
}