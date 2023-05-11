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
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using rocks.kfs.ShelbyFinancials;

namespace RockWeb.Plugins.rocks_kfs.ShelbyFinancials
{
    #region Block Attributes

    [DisplayName( "Shelby Financials Batches to Journal" )]
    [Category( "KFS > Shelby Financials" )]
    [Description( "Block used to create Journal Entries in Shelby Financials from multiple Rock Financial Batches." )]

    #endregion

    #region Block Settings

    [LinkedPage(
        "Detail Page",
        Description = "The Financial Batch Detail Page",
        IsRequired = true,
        DefaultValue = "606BDA31-A8FE-473A-B3F8-A00ECF7E06EC",
        Order = 0,
        Key = AttributeKey.DetailPage )]

    [TextField(
        "Button Text",
        Description = "The text to use in the Export Button.",
        IsRequired = false,
        DefaultValue = "Create Shelby Export",
        Order = 1,
        Key = AttributeKey.ButtonText )]

    [IntegerField(
        "Months Back",
        Description = "The number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches.",
        IsRequired = true,
        DefaultIntegerValue = 2,
        Order = 2,
        Key = AttributeKey.MonthsBack )]

    [EnumField(
        "GL Account Grouping",
        Description = "Determines if debit and/or credit lines should be grouped and summed by GL account in the export file.",
        IsRequired = true,
        EnumSourceType = typeof( GLEntryGroupingMode ),
        DefaultEnumValue = ( int ) GLEntryGroupingMode.DebitAndCreditByFinancialAccount,
        Order = 3,
        Key = AttributeKey.AccountGroupingMode )]

    [LavaField(
        "Journal Description Lava",
        Description = "Lava for the journal description column per line. Default: Batch.Id: Batch.Name",
        IsRequired = true,
        DefaultValue = "{{ Batch.Id }}: {{ Batch.Name }}",
        Order = 4,
        Key = AttributeKey.JournalMemoLava )]

    [BooleanField(
        "Enable Debug",
        Description = "Outputs the object graph to help create your Lava syntax.",
        DefaultBooleanValue = false,
        Order = 5,
        Key = AttributeKey.EnableDebug )]

    #endregion

    public partial class BatchesToJournal : RockBlock, ICustomGridColumns
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string ButtonText = "ButtonText";
            public const string MonthsBack = "MonthsBack";
            public const string AccountGroupingMode = "AccountGroupingMode";
            public const string JournalMemoLava = "JournalDescriptionLava";
            public const string EnableDebug = "EnableDebug";
        }

        #endregion Keys

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gfBatchesToExportFilter.ApplyFilterClick += gfBatchesToExportFilter_ApplyFilterClick;
            gfBatchesToExportFilter.ClearFilterClick += gfBatchesToExportFilter_ClearFilterClick;
            gfBatchesToExportFilter.DisplayFilterValue += gfBatchesToExportFilter_DisplayFilterValue;

            gBatchesToExport.GridRebind += gBatchesToExport_GridRebind;

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            object journalType = Session["JournalType"];
            if ( journalType != null )
            {
                ddlJournalType.SetValue( journalType.ToString() );
            }
            object accountingPeriod = Session["AccountingPeriod"];
            if ( accountingPeriod != null )
            {
                tbAccountingPeriod.Text = accountingPeriod.ToString();
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            var debugEnabled = GetAttributeValue( AttributeKey.EnableDebug ).AsBoolean();

            if ( !Page.IsPostBack )
            {
                BindFilter();
                BindGrid();
            }

            if ( debugEnabled )
            {
                var debugLava = Session["ShelbyFinancialsDebugLava"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( debugLava ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text += debugLava;
                    Session["ShelbyFinancialsDebugLava"] = string.Empty;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            ddlStatus.BindToEnum<BatchStatus>();
            ddlStatus.Items.Insert( 0, Rock.Constants.All.ListItem );
            string statusFilter = gfBatchesToExportFilter.GetUserPreference( "Status" );
            if ( string.IsNullOrWhiteSpace( statusFilter ) )
            {
                statusFilter = BatchStatus.Open.ConvertToInt().ToString();
            }

            ddlStatus.SetValue( statusFilter );

            drpBatchDate.DelimitedValues = gfBatchesToExportFilter.GetUserPreference( "Date Range" );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            btnExportToShelbyFinancials.Text = GetAttributeValue( AttributeKey.ButtonText );
            var monthsBack = GetAttributeValue( AttributeKey.MonthsBack ).AsInteger() * -1;
            var firstBatchDate = RockDateTime.Now.AddMonths( monthsBack );

            var batchIdList = new List<int>();
            var filteredBatchIdList = new List<int>();
            var batchesToExport = new List<BatchData>();

            using ( var rockContextBatches = new RockContext() )
            {
                var batchService = new FinancialBatchService( rockContextBatches );
                var qry = batchService
                    .Queryable().AsNoTracking()
                    .Where( b => b.BatchStartDateTime.HasValue )
                    .Where( b => b.BatchStartDateTime >= firstBatchDate )
                    .Where( b => b.ControlAmount == b.Transactions.Sum( t => t.TransactionDetails.Sum( d => d.Amount ) ) );

                string dateRangeValue = gfBatchesToExportFilter.GetUserPreference( "Date Range" );
                if ( !string.IsNullOrWhiteSpace( dateRangeValue ) )
                {
                    var drp = new DateRangePicker();
                    drp.DelimitedValues = dateRangeValue;
                    if ( drp.LowerValue.HasValue )
                    {
                        qry = qry.Where( b => b.BatchStartDateTime >= drp.LowerValue.Value );
                    }

                    if ( drp.UpperValue.HasValue )
                    {
                        var endOfDay = drp.UpperValue.Value.AddDays( 1 );
                        qry = qry.Where( b => b.BatchStartDateTime < endOfDay );
                    }
                }

                var status = gfBatchesToExportFilter.GetUserPreference( "Status" ).ConvertToEnumOrNull<BatchStatus>();
                if ( status.HasValue )
                {
                    qry = qry.Where( b => b.Status == status );
                }

                SortProperty sortProperty = gBatchesToExport.SortProperty;
                if ( sortProperty != null )
                {
                    qry = qry.Sort( sortProperty );
                }
                else
                {
                    qry = qry
                        .OrderByDescending( b => b.BatchStartDateTime )
                        .ThenBy( b => b.Name );
                }

                batchIdList = qry.Select( b => b.Id ).ToList();
            }

            using ( var rockContextAttributeValues = new RockContext() )
            {
                var dateExportedAttributeId = AttributeCache.Get( "4B6576DD-82F6-419F-8DF0-467D2636822D".AsGuid() ).Id;  //rocks.kfs.ShelbyFinancials.DateExported

                foreach ( var batchId in batchIdList )
                {
                    var attributeValue = new AttributeValueService( rockContextAttributeValues ).GetByAttributeIdAndEntityId( dateExportedAttributeId, batchId );
                    if ( attributeValue == null || attributeValue.ValueAsDateTime == null )
                    {
                        filteredBatchIdList.Add( batchId );
                    }
                }
            }

            using ( var rockContextBatchData = new RockContext() )
            {
                foreach ( var batchId in filteredBatchIdList )
                {
                    var batch = new FinancialBatchService( rockContextBatchData ).Get( batchId );

                    batchesToExport.Add( new BatchData
                    {
                        Id = batch.Id,
                        BatchStartDateTime = batch.BatchStartDateTime,
                        Name = batch.Name,
                        Status = batch.Status.ToString(),
                        Note = batch.Note,
                        Transactions = batch.Transactions.Count,
                        Total = batch.GetTotalTransactionAmount( rockContextBatchData )
                    } );
                }
            }

            gBatchesToExport.DataSource = batchesToExport;
            gBatchesToExport.ObjectList = ( ( List<BatchData> ) gBatchesToExport.DataSource ).ToDictionary( b => b.Id.ToString(), v => v as object );
            gBatchesToExport.EntityTypeId = EntityTypeCache.Get<FinancialBatch>().Id;

            gBatchesToExport.DataBind();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the GridRebind event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gBatchesToExport_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Gfs the batches to export filter display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfBatchesToExportFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }

                case "Status":
                    {
                        var status = e.Value.ConvertToEnumOrNull<BatchStatus>();
                        if ( status.HasValue )
                        {
                            e.Value = status.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Date Range", drpBatchDate.DelimitedValues );

            BindGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.DeleteUserPreferences();
            gfBatchesToExportFilter.SaveUserPreference( "Status", BatchStatus.Open.ConvertToInt().ToString() );
            BindFilter();
            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gBatchesToExport_Click( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "batchId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the Click event of the btnExportToShelbyFinancials control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnExportToShelbyFinancials_Click( object sender, EventArgs e )
        {
            Session["JournalType"] = ddlJournalType.SelectedValue;
            Session["AccountingPeriod"] = tbAccountingPeriod.Text;

            var selectedBatches = new List<int>();

            gBatchesToExport.SelectedKeys.ToList().ForEach( b => selectedBatches.Add( b.ToString().AsInteger() ) );

            if ( selectedBatches.Any() )
            {
                var sfJournal = new SFJournal();
                var items = new List<SFJournal.GLExcelLine>();
                var debugLava = GetAttributeValue( AttributeKey.EnableDebug );

                var rockContext = new RockContext();
                var batchService = new FinancialBatchService( rockContext );
                var batchesToUpdate = new List<FinancialBatch>();

                var exportedBatches = batchService.Queryable()
                    .WhereAttributeValue( rockContext, a => a.Attribute.Key == "rocks.kfs.ShelbyFinancials.DateExported" && ( a.Value != null && a.Value != "" ) )
                    .Select( b => b.Id );

                batchesToUpdate = batchService.Queryable()
                    .Where( b =>
                        selectedBatches.Contains( b.Id )
                        && !exportedBatches.Contains( b.Id )
                      ).ToList();

                foreach ( var batch in batchesToUpdate )
                {
                    var changes = new History.HistoryChangeList();
                    History.EvaluateChange( changes, "Status", batch.Status, BatchStatus.Closed );

                    string errorMessage;

                    if ( batch.IsAutomated && batch.Status == BatchStatus.Pending )
                    {
                        errorMessage = string.Format( "{0} is an automated batch and the status can not be modified when the status is pending. The system will automatically set this batch to OPEN when all transactions have been downloaded.", batch.Name );
                        maWarningDialog.Show( errorMessage, ModalAlertType.Warning );
                        return;
                    }

                    batch.Status = BatchStatus.Closed;

                    if ( !batch.IsValid )
                    {
                        string message = string.Format( "Unable to update status for the selected batches.<br/><br/>{0}", batch.ValidationResults.AsDelimited( "<br/>" ) );
                        maWarningDialog.Show( message, ModalAlertType.Warning );
                        return;
                    }

                    batch.LoadAttributes();

                    var newDate = string.Empty;

                    var oldDate = batch.GetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported" );
                    newDate = RockDateTime.Now.ToString();
                    History.EvaluateChange( changes, "Date Exported", oldDate, newDate.ToString() );

                    var journalCode = ddlJournalType.SelectedValue;
                    var period = tbAccountingPeriod.Text.AsInteger();
                    var groupingMode = ( GLEntryGroupingMode ) GetAttributeValue( AttributeKey.AccountGroupingMode ).AsInteger();

                    items.AddRange( sfJournal.GetGLExcelLines( rockContext, batch, journalCode, period, ref debugLava, GetAttributeValue( AttributeKey.JournalMemoLava ), groupingMode ) );

                    HistoryService.SaveChanges(
                        rockContext,
                        typeof( FinancialBatch ),
                        Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                        batch.Id,
                        changes,
                        false );

                    batch.SetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", newDate );
                    batch.SaveAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", rockContext );
                }

                rockContext.SaveChanges();

                if ( HttpContext.Current.Session["ShelbyFinancialsExcelExport"] != null )
                {
                    HttpContext.Current.Session["ShelbyFinancialsExcelExport"] = null;
                }
                if ( HttpContext.Current.Session["ShelbyFinancialsFileId"] != null )
                {
                    HttpContext.Current.Session["ShelbyFinancialsFileId"] = string.Empty;
                }

                var excel = sfJournal.GLExcelExport( items );

                Session["ShelbyFinancialsExcelExport"] = excel;
                Session["ShelbyFinancialsFileId"] = RockDateTime.Now.ToString( "yyyyMMdd_HHmmss" );
                Session["ShelbyFinancialsDebugLava"] = debugLava;

                NavigateToPage( this.RockPage.Guid, new Dictionary<string, string>() );
            }
            else
            {
                nbError.Text = string.Format( "There were not any batches selected." );
                nbError.NotificationBoxType = NotificationBoxType.Warning;
                nbError.Visible = true;
            }
        }

        #endregion

        #region Classes

        public class BatchData
        {
            public int Id { get; set; }
            public DateTime? BatchStartDateTime { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
            public int Transactions { get; set; }
            public decimal Total { get; set; }
        }

        #endregion
    }
}
