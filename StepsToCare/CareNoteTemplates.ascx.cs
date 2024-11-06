// <copyright>
// Copyright 2021 by Kingdom First Solutions
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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using rocks.kfs.StepsToCare.Model;

namespace RockWeb.Plugins.rocks_kfs.StepsToCare
{
    #region Block Attributes

    [DisplayName( "Care Note Templates" )]
    [Category( "KFS > Steps To Care" )]
    [Description( "Care Note Templates block for KFS Steps to Care package. Used for adding and editing care Note Templates for adding quick notes to needs." )]

    #endregion

    #region Block Settings

    #endregion

    public partial class CareNoteTemplates : Rock.Web.UI.RockBlock
    {

        /// <summary>
        /// View State Keys
        /// </summary>
        private static class ViewStateKey
        {
            public const string AvailableAttributes = "AvailableAttributes";
        }

        #region Properties

        public List<AttributeCache> AvailableAttributes { get; set; }

        private bool _canAddEditDelete = false;

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAttributes = ViewState[ViewStateKey.AvailableAttributes] as List<AttributeCache>;

            AddDynamicControls();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState[ViewStateKey.AvailableAttributes] = AvailableAttributes;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlCareNoteTemplates );

            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );

            gList.GridRebind += gList_GridRebind;
            //gList.RowDataBound += gList_RowDataBound;
            gList.DataKeyNames = new string[] { "Id" };
            gList.Actions.ShowAdd = _canAddEditDelete;
            gList.Actions.AddClick += gList_AddClick;
            gList.IsDeleteEnabled = _canAddEditDelete;
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
                BindAttributes();
                AddDynamicControls();
                BindGrid();
            }
            else
            {
                var rockContext = new RockContext();
                NoteTemplate item = new NoteTemplateService( rockContext ).Get( hfNoteTemplateId.ValueAsInt() );
                if ( item == null )
                {
                    item = new NoteTemplate();
                }
                item.LoadAttributes();

                phAttributes.Controls.Clear();
                Helper.AddEditControls( item, phAttributes, false, BlockValidationGroup, 2 );
            }
        }

        /// <summary>
        /// Binds the attributes.
        /// </summary>
        private void BindAttributes()
        {
            // Parse the attribute filters
            AvailableAttributes = new List<AttributeCache>();

            int entityTypeId = new NoteTemplate().TypeId;
            foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                .Where( a =>
                    a.EntityTypeId == entityTypeId &&
                    a.IsGridColumn )
                .OrderByDescending( a => a.EntityTypeQualifierColumn )
                .ThenBy( a => a.Order )
                .ThenBy( a => a.Name ) )
            {
                AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
            }
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
            BindAttributes();
            AddDynamicControls();
            BindGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdAddNoteTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAddNoteTemplate_SaveClick( object sender, EventArgs e )
        {
            var result = AddNote( hfNoteTemplateId.Value.AsInteger() );

            mdAddNoteTemplate.Hide();
            BindGrid();
        }

        /// <summary>
        /// Handles the SaveThenAddClick event of the mdAddNoteTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAddNoteTemplate_SaveThenAddClick( object sender, EventArgs e )
        {
            var result = AddNote( hfNoteTemplateId.Value.AsInteger() );

            BindGrid();
            mdAddNoteTemplate.Title = "Add Template";
            ShowDetail( 0 );
        }

        /// <summary>
        /// Handles the RowDataBound event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        public void gList_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                NoteTemplate noteTemplate = e.Row.DataItem as NoteTemplate;
                if ( noteTemplate != null )
                {
                    Literal lIcon = e.Row.FindControl( "lIcon" ) as Literal;
                    if ( lIcon != null )
                    {
                        lIcon.Text = string.Format( "<i class=\"{0}\"></i>", noteTemplate.Icon );
                    }
                }
            }

        }

        /// <summary>
        /// Handles the AddClick event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gList_AddClick( object sender, EventArgs e )
        {
            ShowDetail( 0 );
            mdAddNoteTemplate.Title = "Add Template";
            mdAddNoteTemplate.Show();
        }

        /// <summary>
        /// Handles the Edit event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gList_Edit( object sender, RowEventArgs e )
        {
            ShowDetail( e.RowKeyId );
            mdAddNoteTemplate.Title = "Edit Template";
            mdAddNoteTemplate.Show();
        }

        /// <summary>
        /// Handles the Delete event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gList_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            NoteTemplateService service = new NoteTemplateService( rockContext );
            NoteTemplate noteTemplate = service.Get( e.RowKeyId );
            if ( noteTemplate != null )
            {
                service.Delete( noteTemplate );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the GridReorder event of the gList control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gList_GridReorder( object sender, GridReorderEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                NoteTemplateService noteTemplateService = new NoteTemplateService( rockContext );
                var noteTemplates = noteTemplateService.Queryable().OrderBy( nt => nt.Order ).ToList();
                noteTemplateService.Reorder( noteTemplates, e.OldIndex, e.NewIndex );
                rockContext.SaveChanges();
            }

            BindGrid();
        }
        #endregion

        #region Methods

        private void BindGrid()
        {
            gList.Visible = true;
            RockContext rockContext = new RockContext();
            NoteTemplateService noteTemplateService = new NoteTemplateService( rockContext );
            var qry = noteTemplateService.Queryable().AsNoTracking().OrderBy( nt => nt.Order );

            var list = qry.ToList();

            gList.DataSource = list;
            gList.DataBind();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="noteTemplateId">The care worker identifier</param>
        public void ShowDetail( int noteTemplateId )
        {
            NoteTemplate noteTemplate = null;
            var rockContext = new RockContext();
            NoteTemplateService noteTemplateService = new NoteTemplateService( rockContext );
            if ( !noteTemplateId.Equals( 0 ) )
            {
                noteTemplate = noteTemplateService.Get( noteTemplateId );
                pdAuditDetails.SetEntity( noteTemplate, ResolveRockUrl( "~" ) );
            }

            if ( noteTemplate == null )
            {
                noteTemplate = new NoteTemplate { Id = 0 };
                pdAuditDetails.Visible = false;
                cbActive.Checked = true;
            }
            else
            {
                cbActive.Checked = noteTemplate.IsActive;

            }

            tbIcon.Text = noteTemplate.Icon;
            tbNote.Text = noteTemplate.Note;

            noteTemplate.LoadAttributes();
            phAttributes.Controls.Clear();
            Helper.AddEditControls( noteTemplate, phAttributes, true, BlockValidationGroup, 2 );

            hfNoteTemplateId.Value = noteTemplate.Id.ToString();
        }

        /// <summary>
        /// Adds the New Worker
        /// </summary>
        private bool AddNote( int noteTemplateId )
        {
            if ( Page.IsValid )
            {
                RockContext rockContext = new RockContext();
                NoteTemplateService noteTemplateService = new NoteTemplateService( rockContext );

                NoteTemplate noteTemplate = null;

                if ( !noteTemplateId.Equals( 0 ) )
                {
                    noteTemplate = noteTemplateService.Get( noteTemplateId );
                }

                NoteTemplate lastNoteTemplate = null;
                if ( noteTemplate == null )
                {
                    noteTemplate = new NoteTemplate { Id = 0 };
                    lastNoteTemplate = noteTemplateService.Queryable().OrderByDescending( b => b.Order ).FirstOrDefault();
                }

                noteTemplate.Icon = tbIcon.Text;

                noteTemplate.Note = tbNote.Text;

                noteTemplate.IsActive = cbActive.Checked;


                if ( lastNoteTemplate != null )
                {
                    noteTemplate.Order = lastNoteTemplate.Order + 1;
                }
                else if ( noteTemplate.Id == 0 )
                {
                    noteTemplate.Order = 0;
                }

                if ( noteTemplate.IsValid )
                {
                    if ( noteTemplate.Id.Equals( 0 ) )
                    {
                        noteTemplateService.Add( noteTemplate );
                    }

                    // get attributes
                    noteTemplate.LoadAttributes();
                    Helper.GetEditValues( phAttributes, noteTemplate );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        noteTemplate.SaveAttributeValues( rockContext );
                    } );

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void AddDynamicControls()
        {
            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    bool columnExists = gList.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = attribute.Key;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gList.Columns.Add( boundField );
                    }
                }
            }

            // Add delete column
            var deleteField = new DeleteField();
            gList.Columns.Add( deleteField );
            deleteField.Click += gList_Delete;
        }

        #endregion

    }
}
