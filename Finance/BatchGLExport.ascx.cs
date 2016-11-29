
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kingdomfirstsolutions.Finance
{
    [DisplayName( "Batch GL Export" )]
    [Category( "KFS > Finance" )]
    [Description( "Lists all financial batches and provides GL Export capability" )]
    [BooleanField( "Show Accounting Code", "Should the accounting code column be displayed.", false, "", 1 )]
    public partial class BatchGLExport : Rock.Web.UI.RockBlock
    {
        #region Fields

        private RockDropDownList ddlAction;
        private int? _entityTypeId = null;
        private string _entityQualifierColumn = string.Empty;
        private string _entityQualifierValue = string.Empty;
        private List<string> _errorMessage = new List<string>();

        protected RockContext rockContext = new RockContext();
        private FinancialBatchService batchService = null;
        private AttributeService attributeService = null;


        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            batchService = new FinancialBatchService( rockContext );
            attributeService = new AttributeService( rockContext );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            gfBatchFilter.ApplyFilterClick += gfBatchFilter_ApplyFilterClick;
            gfBatchFilter.ClearFilterClick += gfBatchFilter_ClearFilterClick;
            gfBatchFilter.DisplayFilterValue += gfBatchFilter_DisplayFilterValue;

            gBatchList.DataKeyNames = new string[] { "Id" };
            gBatchList.Actions.ShowExcelExport = false;
            gBatchList.Actions.ShowMergeTemplate = false;
            gBatchList.GridRebind += gBatchList_GridRebind;
            gBatchList.RowDataBound += gBatchList_RowDataBound;

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

            dpExportDate.SelectedDate = RockDateTime.Today;

            Rock.Model.Attribute attribute = null;

            _entityTypeId = EntityTypeCache.Read( typeof( Rock.Model.FinancialBatch ) ).Id;

            IQueryable<Rock.Model.Attribute> attributeQuery = null;

            if ( _entityTypeId != null )
            {
                attributeQuery = attributeService.Get( _entityTypeId, _entityQualifierColumn, _entityQualifierValue );
            }
            if ( attributeQuery.Count() == 0 )
            {
                Rock.Model.Attribute edtAttribute = new Rock.Model.Attribute();
                edtAttribute.FieldTypeId = FieldTypeCache.Read( Rock.SystemGuid.FieldType.DATE_TIME ).Id;
                edtAttribute.Name = "Batch Exported";
                edtAttribute.Key = "GLExport_BatchExported";

                attribute = Rock.Attribute.Helper.SaveAttributeEdits( edtAttribute, _entityTypeId, _entityQualifierColumn, _entityQualifierValue );
                // Attribute will be null if it was not valid
                if ( attribute == null )
                {
                    nbWarningMessage.Text += "Batch exported attribute not created!";
                    nbWarningMessage.Visible = true;
                }

                AttributeCache.FlushEntityAttributes();
            }


        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfBatchFilter.DeleteUserPreferences();
            BindFilter();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            nbResult.Visible = false;

            if ( !Page.IsPostBack )
            {
                SetVisibilityOption();
                BindFilter();
                BindGrid();
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
            SetVisibilityOption();
            BindGrid();
        }

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Row Limit":
                    {
                        // row limit filter was removed, so hide it just in case
                        e.Value = null;
                        break;
                    }

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

                case "Contains Transaction Type":
                    {
                        var transactionTypeValueId = e.Value.AsIntegerOrNull();
                        if ( transactionTypeValueId.HasValue )
                        {
                            var transactionTypeValue = DefinedValueCache.Read( transactionTypeValueId.Value );
                            e.Value = transactionTypeValue != null ? transactionTypeValue.ToString() : string.Empty;
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }

                case "Campus":
                    {
                        var campus = CampusCache.Read( e.Value.AsInteger() );
                        if ( campus != null )
                        {
                            e.Value = campus.Name;
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
        /// Handles the ApplyFilterClick event of the gfBatchFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBatchFilter.SaveUserPreference( "Date Range", drpBatchDate.DelimitedValues );
            gfBatchFilter.SaveUserPreference( "Title", tbTitle.Text );
            if ( tbAccountingCode.Visible )
            {
                gfBatchFilter.SaveUserPreference( "Accounting Code", tbAccountingCode.Text );
            }

            gfBatchFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gfBatchFilter.SaveUserPreference( "Campus", campCampus.SelectedValue );
            gfBatchFilter.SaveUserPreference( "Batch Exported", ddlBatchExported.SelectedValue );
            gfBatchFilter.SaveUserPreference( "Contains Transaction Type", ddlTransactionType.SelectedValue );

            BindGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gBatchList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gBatchList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var batchRow = e.Row.DataItem as BatchRow;

                if ( batchRow != null )
                {
                    if ( batchRow.TransactionCount > 0 )
                    {
                        e.Row.AddCssClass( "js-has-transactions" );
                    }

                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gBatchList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gBatchList_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid( e.IsExporting );
        }

        public void btnPreview_Click( object sender, EventArgs e )
        {
            var batchesSelected = new List<int>();

            gBatchList.SelectedKeys.ToList().ForEach( b => batchesSelected.Add( b.ToString().AsInteger() ) );

            var attributeValues = generateAttributeFilters();

            var batchesToUpdate = batchService.Queryable()
                .Where( b => batchesSelected.Contains( b.Id ) )
                .Where( b => !attributeValues.Select( v => v.EntityId ).Contains( b.Id ) )
                .ToList();

            string output = String.Empty;
            List<GLExportLineItem> items = getExportLineItems( batchesToUpdate );
            BatchExportList.ShowFooter = false;
            BatchExportList.Actions.ShowExcelExport = false;
            BatchExportList.Actions.ShowMergeTemplate = false;
            BatchExportList.PagerSettings.Visible = false;
            BatchExportList.DataSource = new BindingList<GLExportLineItem>( items );
            BatchExportList.DataBind();
        }

        public void btnExport_Click( object sender, EventArgs e )
        {
            nbResult.Text = string.Empty;
            Session["JournalType"] = ddlJournalType.SelectedValue;
            Session["AccountingPeriod"] = tbAccountingPeriod.Text;

            var batchesSelected = new List<int>();

            gBatchList.SelectedKeys.ToList().ForEach( b => batchesSelected.Add( b.ToString().AsInteger() ) );

            if ( batchesSelected.Any() )
            {
                var attributeValues = generateAttributeFilters();

                var batchesToUpdate = batchService.Queryable()
                    .Where( b => batchesSelected.Contains( b.Id ) )
                    .Where( b => !attributeValues.Select( v => v.EntityId ).Contains( b.Id ) )
                    .ToList();

                string output = String.Empty;
                List<GLExportLineItem> items = getExportLineItems( batchesToUpdate );

                StringBuilder stringBuilder = new StringBuilder();
                int num = 0;
                foreach ( GLExportLineItem lineitem in items )
                {
                    if ( num > 0 )
                    {
                        stringBuilder.Append( Environment.NewLine );
                    }
                    stringBuilder.Append( convertGLItemToStr( lineitem ) );
                    num++;
                }
                if ( items.Count > 0 && nbResult.NotificationBoxType != NotificationBoxType.Warning )
                {
                    setExported( batchesToUpdate );

                    output = stringBuilder.ToString();

                    Session["GLExportLineItems"] = output;

                    var url = "/Plugins/com_kingdomfirstsolutions/Finance/GLExport.aspx";
                    ScriptManager.RegisterClientScriptBlock( this, typeof( UserControl ), "batchexport", string.Format( "window.open('{0}');", url ), true );

                    //MemoryStream ms = new MemoryStream( Encoding.ASCII.GetBytes( output ) );
                    //Response.ClearContent();
                    //Response.ClearHeaders();
                    //Response.ContentType = "application/text";
                    //Response.AddHeader( "Content-Disposition", "attachment; filename=GLTRN2000.txt" );
                    //ms.WriteTo( Response.OutputStream );
                    //Response.End();
                }
                else if ( nbResult.NotificationBoxType != NotificationBoxType.Warning )
                {
                    nbResult.Text = string.Format( "There were not any batches that were exportable selected." );
                    nbResult.NotificationBoxType = NotificationBoxType.Warning;
                    nbResult.Visible = true;
                }
            }
            else
            {
                nbResult.Text = string.Format( "There were not any batches selected." );
                nbResult.NotificationBoxType = NotificationBoxType.Warning;
                nbResult.Visible = true;
            }

        }

        #endregion

        #region Methods

        protected IQueryable<AttributeValue> generateAttributeFilters()
        {
            var attributeValueService = new AttributeValueService( rockContext );
            var attributes = attributeService.GetByEntityTypeId( _entityTypeId );
            var exported = attributes.AsNoTracking().FirstOrDefault( a => a.Key == "GLExport_BatchExported" );

            var attribute = AttributeCache.Read( exported.Id );

            var attributeValues = attributeValueService
                    .Queryable()
                    .Where( v => v.Attribute.Id == attribute.Id );

            attributeValues = attributeValues.Where( d => d.ValueAsDateTime != null );

            return attributeValues;
        }

        private void setExported( List<FinancialBatch> batchesToUpdate )
        {
            foreach ( var batch in batchesToUpdate )
            {
                batch.LoadAttributes();
                var newStatus = BatchStatus.Closed;
                var newDateExported = RockDateTime.Now;

                var changes = new List<string>();
                History.EvaluateChange( changes, "Status", batch.Status, newStatus );
                batch.Status = newStatus;
                History.EvaluateChange( changes, "Batch Exported", batch.GetAttributeValue( "GLExport_BatchExported" ), newDateExported.ToString() );
                batch.SetAttributeValue( "GLExport_BatchExported", newDateExported );

                if ( !batch.IsValid )
                {
                    string message = string.Format( "Unable to update status or batch export date for the selected batches.<br/><br/>{0}", batch.ValidationResults.AsDelimited( "<br/>" ) );
                    maWarningDialog.Show( message, ModalAlertType.Warning );
                    return;
                }

                int? modifiedByPersonAliasId = batch.ModifiedAuditValuesAlreadyUpdated ? batch.ModifiedByPersonAliasId : ( int? ) null;

                HistoryService.SaveChanges(
                    rockContext,
                    typeof( FinancialBatch ),
                    Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                    batch.Id,
                    changes,
                    false,
                    modifiedByPersonAliasId );

                batch.SaveAttributeValues( rockContext );
            }

            rockContext.SaveChanges();

            nbResult.Text = string.Format(
                "{0} batches were {1}. <br>",
                batchesToUpdate.Count().ToString( "N0" ),
                "closed" );
            nbResult.Text += string.Format(
                "{0} batches were {1}.",
                batchesToUpdate.Count().ToString( "N0" ),
                "exported" );

            nbResult.NotificationBoxType = NotificationBoxType.Success;
            nbResult.Visible = true;

            BindGrid();
        }

        private List<GLExportLineItem> getExportLineItems( List<FinancialBatch> batchesToUpdate )
        {
            List<GLTransaction> batchTransactions = new List<GLTransaction>();

            foreach ( var batch in batchesToUpdate )
            {
                batch.LoadAttributes();
                foreach ( var transaction in batch.Transactions )
                {
                    transaction.LoadAttributes();
                    foreach ( var transactionDetail in transaction.TransactionDetails )
                    {
                        transactionDetail.Account.LoadAttributes();

                        GLTransaction transactionItem = new GLTransaction();
                        transactionItem.glCompany = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_Company" );
                        transactionItem.glFund = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_Fund" );
                        transactionItem.glBankAccount = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_BankAccount" );
                        transactionItem.glRevenueAccount = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_RevenueAccount" );
                        transactionItem.glRevenueDepartment = transactionDetail.Account.GetAttributeValue( "GeneralLedgerExport_RevenueDepartment" );
                        transactionItem.projectCode = transaction.GetAttributeValue( "Project" );
                        transactionItem.total = transactionDetail.Amount;

                        transactionItem.batch = batch;
                        transactionItem.transactionDetail = transactionDetail;

                        batchTransactions.Add( transactionItem );

                    }
                }
            }

            var groupedTransactions = batchTransactions.GroupBy( d => new { d.glBankAccount, d.glCompany, d.glFund, d.glRevenueAccount, d.glRevenueDepartment, d.projectCode } )
                .Select( t => new GLTransaction
                {
                    glBankAccount = t.Key.glBankAccount,
                    glCompany = t.Key.glCompany,
                    glFund = t.Key.glFund,
                    glRevenueAccount = t.Key.glRevenueAccount,
                    glRevenueDepartment = t.Key.glRevenueDepartment,
                    projectCode = t.Key.projectCode,
                    total = t.Sum( f => ( decimal? ) f.total ) ?? 0.0M,

                    batch = t.FirstOrDefault().batch,
                    transactionDetail = t.FirstOrDefault().transactionDetail

                } )
                .ToList();

            List<GLExportLineItem> items = GenerateLineItems( groupedTransactions, ddlJournalType.SelectedValue, tbAccountingPeriod.Text, dpExportDate.SelectedDate );

            return items;
        }

        private List<GLExportLineItem> GenerateLineItems( List<GLTransaction> transactionItems, string journalType, string accountingPeriod, DateTime? selectedDate )
        {
            List<GLExportLineItem> returnList = new List<GLExportLineItem>();
            foreach ( var transaction in transactionItems )
            {
                string projectCode = String.Empty;

                if ( !String.IsNullOrWhiteSpace( transaction.projectCode ) )
                {
                    var dt = DefinedValueCache.Read( transaction.projectCode );
                    var projects = DefinedTypeCache.Read( dt.DefinedTypeId );
                    if ( projects != null )
                    {
                        foreach ( var project in projects.DefinedValues.OrderByDescending( a => a.Value.AsInteger() ).Where( p => p.Guid.ToString().ToLower() == transaction.projectCode.ToString().ToLower() ) )
                        {
                            projectCode = project.GetAttributeValue( "Code" );
                        }
                    }
                }

                GLExportLineItem generalLedgerExportLineItem = new GLExportLineItem()
                {
                    AccountingPeriod = accountingPeriod,
                    AccountNumber = transaction.glBankAccount,
                    Amount = ( decimal ) transaction.total,
                    CompanyNumber = transaction.glCompany,
                    Date = selectedDate,
                    DepartmentNumber = "",
                    Description1 = string.Format( "{0}: {1}", transaction.batch.Id, transaction.batch.Name ), 
                    Description2 = string.Empty,
                    FundNumber = transaction.glFund,
                    JournalNumber = 0,
                    JournalType = journalType,
                    ProjectCode = projectCode
                };
                if ( ValidateObject( generalLedgerExportLineItem, transaction.transactionDetail.Account.Name ) )
                    returnList.Add( generalLedgerExportLineItem );
                GLExportLineItem generalLedgerExportLineItem1 = new GLExportLineItem()
                {
                    AccountingPeriod = accountingPeriod,
                    AccountNumber = transaction.glRevenueAccount,
                    Amount = new decimal( 10, 0, 0, true, 1 ) * ( decimal ) transaction.total,
                    CompanyNumber = transaction.glCompany,
                    Date = selectedDate,
                    DepartmentNumber = transaction.glRevenueDepartment,
                    Description1 = string.Format( "{0}: {1}", transaction.batch.Id, transaction.batch.Name ),
                    Description2 = string.Empty,
                    FundNumber = transaction.glFund,
                    JournalNumber = 0,
                    JournalType = journalType,
                    ProjectCode = projectCode
                };
                if ( ValidateObject( generalLedgerExportLineItem1, transaction.transactionDetail.Account.Name ) )
                    returnList.Add( generalLedgerExportLineItem1 );
            }

            if ( _errorMessage.Count > 0 )
            {
                nbResult.Text = string.Join("", _errorMessage.ToArray() );
                nbResult.NotificationBoxType = NotificationBoxType.Warning;
                nbResult.Visible = true;
            }

            return returnList;
        }

        private string convertGLItemToStr( GLExportLineItem item )
        {
            string[] str = new string[] { "", "".PadLeft( 5, '0' ), null, null, null, null, null, null, null, null };
            string[] strArrays = new string[] { item.CompanyNumber.ToString().PadLeft( 3, '0' ), item.FundNumber.ToString().PadLeft( 3, '0' ), item.AccountingPeriod.ToString().PadLeft( 2, '0' ), item.JournalType, null };
            int journalNumber = item.JournalNumber;
            strArrays[4] = journalNumber.ToString().PadLeft( 5, '0' );
            str[2] = string.Concat( strArrays );
            str[3] = "".PadLeft( 3, '0' );
            str[4] = item.Date.HasValue ? item.Date.Value.ToString( "MMddyy" ) : "<not available>";
            str[5] = item.Description1.Substring( 0, ( item.Description1.Length <= 30 ? item.Description1.Length : 30 ) );
            str[6] = item.Description1.Substring( 0, ( item.Description2.Length <= 30 ? item.Description2.Length : 30 ) );
            str[7] = string.Concat( item.DepartmentNumber.ToString().PadLeft( 3, '0' ), item.AccountNumber.ToString().PadLeft( 9, '0' ) );
            decimal amount = item.Amount;
            str[8] = amount.ToString( "0.00" ).Replace( ".", "" );
            str[9] = item.ProjectCode;
            StringBuilder stringBuilder = new StringBuilder();
            int num = 0;
            string[] strArrays1 = str;
            for ( int i = 0; i < ( int ) strArrays1.Length; i++ )
            {
                string str1 = strArrays1[i];
                if ( num > 0 )
                {
                    stringBuilder.AppendFormat( "\"{0}\",", str1.Replace( "\"", "" ) );
                }
                num++;
            }
            string str2 = stringBuilder.ToString();
            if ( str2 != "" )
            {
                str2 = str2.Substring( 0, str2.Length - 1 );
            }
            return str2;
        }

        private void SetVisibilityOption()
        {
            bool showAccountingCode = GetAttributeValue( "ShowAccountingCode" ).AsBoolean();
            tbAccountingCode.Visible = showAccountingCode;
            gBatchList.Columns[4].Visible = showAccountingCode;

            if ( showAccountingCode )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                tbAccountingCode.Text = !string.IsNullOrWhiteSpace( accountingCode ) ? accountingCode : string.Empty;
            }
        }

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            string titleFilter = gfBatchFilter.GetUserPreference( "Title" );
            tbTitle.Text = !string.IsNullOrWhiteSpace( titleFilter ) ? titleFilter : string.Empty;

            if ( tbAccountingCode.Visible )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                tbAccountingCode.Text = !string.IsNullOrWhiteSpace( accountingCode ) ? accountingCode : string.Empty;
            }

            ddlStatus.BindToEnum<BatchStatus>();
            ddlStatus.Items.Insert( 0, Rock.Constants.All.ListItem );
            string statusFilter = gfBatchFilter.GetUserPreference( "Status" );
            if ( string.IsNullOrWhiteSpace( statusFilter ) )
            {
                statusFilter = BatchStatus.Open.ConvertToInt().ToString();
            }

            ddlStatus.SetValue( statusFilter );

            var definedTypeTransactionTypes = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() );
            ddlTransactionType.BindToDefinedType( definedTypeTransactionTypes, true );
            ddlTransactionType.SetValue( gfBatchFilter.GetUserPreference( "Contains Transaction Type" ) );

            var campusi = CampusCache.All();
            campCampus.Campuses = campusi;
            campCampus.Visible = campusi.Any();
            campCampus.SetValue( gfBatchFilter.GetUserPreference( "Campus" ) );

            drpBatchDate.DelimitedValues = gfBatchFilter.GetUserPreference( "Date Range" );

            string batchExportedFilter = gfBatchFilter.GetUserPreference( "Batch Exported" );
            if ( string.IsNullOrWhiteSpace( batchExportedFilter ) )
            {
                batchExportedFilter = "No";
            }
            ddlBatchExported.SetValue( batchExportedFilter );


        }

        /// <summary>
        /// Formats the value as currency (called from markup)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string FormatValueAsCurrency( decimal value )
        {
            return value.FormatAsCurrency();
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid( bool isExporting = false )
        {

            var txnCountCol = gBatchList.ColumnsOfType<RockBoundField>().FirstOrDefault( c => c.DataField == "TransactionCount" );
            if ( txnCountCol != null )
            {
                txnCountCol.HeaderText = isExporting ? "Transaction Count" :
                    "<span class='hidden-print'>Transaction Count</span><span class='visible-print-inline'>Txns</span>";
            }

            var txnAmountCol = gBatchList.ColumnsOfType<CurrencyField>().FirstOrDefault( c => c.DataField == "TransactionAmount" );
            if ( txnAmountCol != null )
            {
                txnAmountCol.HeaderText = isExporting ? "Transaction Amount" :
                    "<span class='hidden-print'>Transaction Total</span><span class='visible-print-inline'>Txn Total</span>";
            }

            var accountsCol = gBatchList.ColumnsOfType<RockBoundField>().FirstOrDefault( c => c.HeaderText == "Accounts" );
            if ( accountsCol != null )
            {
                accountsCol.DataField = isExporting ? "AccountSummaryText" : "AccountSummaryHtml";
            }

            try
            {
                var qry = GetQuery().AsNoTracking();
                var batchRowQry = qry.Select( b => new BatchRow
                {
                    Id = b.Id,
                    BatchStartDateTime = b.BatchStartDateTime.Value,
                    Name = b.Name,
                    AccountingSystemCode = b.AccountingSystemCode,
                    TransactionCount = b.Transactions.Count(),
                    TransactionAmount = b.Transactions.Sum( t => ( decimal? ) ( t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M ) ) ?? 0.0M,
                    ControlAmount = b.ControlAmount,
                    CampusName = b.Campus != null ? b.Campus.Name : "",
                    Status = b.Status,
                    UnMatchedTxns = b.Transactions.Any( t => !t.AuthorizedPersonAliasId.HasValue ),
                    BatchNote = b.Note,
                    batch = b,
                    AccountSummaryList = b.Transactions
                        .SelectMany( t => t.TransactionDetails )
                        .GroupBy( d => d.AccountId )
                        .Select( s => new BatchAccountSummary
                        {
                            AccountId = s.Key,
                            AccountOrder = s.Max( d => d.Account.Order ),
                            AccountName = s.Max( d => d.Account.Name ),
                            Amount = s.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M
                        } )
                        .OrderBy( s => s.AccountOrder )
                        .ToList()
                } );

                var attributeBatchRowList = batchRowQry.Skip( gBatchList.PageIndex * gBatchList.PageSize ).Take( gBatchList.PageSize ).ToList();

                foreach ( var batchRow in attributeBatchRowList )
                {
                    batchRow.batch.LoadAttributes();
                    batchRow.batchExportedDT = batchRow.batch.GetAttributeValue( "GLExport_BatchExported" );
                }

                gBatchList.SetLinqDataSource( batchRowQry );
                gBatchList.DataSource = attributeBatchRowList;
                gBatchList.EntityTypeId = EntityTypeCache.Read<Rock.Model.FinancialBatch>().Id;
                gBatchList.DataBind();

                var qryTransactionDetails = qry.SelectMany( a => a.Transactions ).SelectMany( a => a.TransactionDetails );
                var accountSummaryQry = qryTransactionDetails.GroupBy( a => a.Account ).Select( a => new
                {
                    a.Key.Name,
                    a.Key.Order,
                    TotalAmount = ( decimal? ) a.Sum( d => d.Amount )
                } ).OrderBy( a => a.Order );

            }
            catch ( Exception ex )
            {
                nbWarningMessage.Text = ex.Message;
            }
        }

        /// <summary>
        /// Gets the query.  Set the timeout to 90 seconds in case the user
        /// has not set any filters and they've imported N years worth of
        /// batch data into Rock.
        /// </summary>
        /// <returns></returns>
        private IOrderedQueryable<FinancialBatch> GetQuery()
        {
            rockContext.Database.CommandTimeout = 90;
            var qry = batchService.Queryable()
                .Where( b => b.BatchStartDateTime.HasValue );

            // filter by date
            string dateRangeValue = gfBatchFilter.GetUserPreference( "Date Range" );
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

            // filter by status
            var status = gfBatchFilter.GetUserPreference( "Status" ).ConvertToEnumOrNull<BatchStatus>();
            if ( status.HasValue )
            {
                qry = qry.Where( b => b.Status == status );
            }

            // filter by batches that contain transactions of the specified transaction type
            var transactionTypeValueId = gfBatchFilter.GetUserPreference( "Contains Transaction Type" ).AsIntegerOrNull();
            if ( transactionTypeValueId.HasValue )
            {
                qry = qry.Where( a => a.Transactions.Any( t => t.TransactionTypeValueId == transactionTypeValueId.Value ) );
            }

            // filter by title
            string title = gfBatchFilter.GetUserPreference( "Title" );
            if ( !string.IsNullOrEmpty( title ) )
            {
                qry = qry.Where( batch => batch.Name.StartsWith( title ) );
            }

            // filter by accounting code
            if ( tbAccountingCode.Visible )
            {
                string accountingCode = gfBatchFilter.GetUserPreference( "Accounting Code" );
                if ( !string.IsNullOrEmpty( accountingCode ) )
                {
                    qry = qry.Where( batch => batch.AccountingSystemCode.StartsWith( accountingCode ) );
                }
            }

            // filter by campus
            var campus = CampusCache.Read( gfBatchFilter.GetUserPreference( "Campus" ).AsInteger() );
            if ( campus != null )
            {
                qry = qry.Where( b => b.CampusId == campus.Id );
            }

            string batchExported = gfBatchFilter.GetUserPreference( "Batch Exported" );
            if ( !string.IsNullOrEmpty( batchExported ) )
            {
                var attributeValues = generateAttributeFilters();

                if ( batchExported == "No" )
                {
                    qry = qry.Where( b => !attributeValues.Select( v => v.EntityId ).Contains( b.Id ) );
                }
                else
                {
                    qry = qry.Where( b => attributeValues.Select( v => v.EntityId ).Contains( b.Id ) );
                }
            }

            IOrderedQueryable<FinancialBatch> sortedQry = null;

            SortProperty sortProperty = gBatchList.SortProperty;
            if ( sortProperty != null )
            {
                switch ( sortProperty.Property )
                {
                    case "TransactionCount":
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                sortedQry = qry.OrderBy( b => b.Transactions.Count() );
                            }
                            else
                            {
                                sortedQry = qry.OrderByDescending( b => b.Transactions.Count() );
                            }

                            break;
                        }

                    case "TransactionAmount":
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                sortedQry = qry.OrderBy( b => b.Transactions.Sum( t => ( decimal? ) ( t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M ) ) ?? 0.0M );
                            }
                            else
                            {
                                sortedQry = qry.OrderByDescending( b => b.Transactions.Sum( t => ( decimal? ) ( t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M ) ) ?? 0.0M );
                            }

                            break;
                        }

                    default:
                        {
                            sortedQry = qry.Sort( sortProperty );
                            break;
                        }
                }
            }
            else
            {
                sortedQry = qry
                    .OrderByDescending( b => b.BatchStartDateTime )
                    .ThenBy( b => b.Name );
            }

            return sortedQry;
        }

        private bool ValidateObject( GLExportLineItem model, string accountName )
        {

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext context = new ValidationContext( model, null, null );

            if ( !Validator.TryValidateObject( model, context, errors, true ) )
            {
                string tempMessage = string.Empty;

                foreach ( ValidationResult e in errors )
                {
                    tempMessage = string.Format( "Error with {0}: {1}<br>", accountName, e.ErrorMessage );

                    if ( !_errorMessage.Contains( tempMessage ) )
                    {
                        _errorMessage.Add( tempMessage );
                    }
                }  

                return false;
            }

            return true;

        }

        #endregion

        #region Helper Class

        public class BatchAccountSummary
        {
            public int AccountId { get; set; }
            public int AccountOrder { get; set; }
            public string AccountName { get; set; }
            public decimal Amount { get; set; }
            public override string ToString()
            {
                return string.Format( "{0}: {1}", AccountName, Amount.FormatAsCurrency() );
            }
        }

        public class BatchRow
        {
            public int Id { get; set; }
            public DateTime BatchStartDateTime { get; set; }
            public string Name { get; set; }
            public string AccountingSystemCode { get; set; }
            public int TransactionCount { get; set; }
            public decimal TransactionAmount { get; set; }
            public decimal ControlAmount { get; set; }
            public List<BatchAccountSummary> AccountSummaryList { get; set; }
            public string CampusName { get; set; }
            public BatchStatus Status { get; set; }
            public bool UnMatchedTxns { get; set; }
            public string BatchNote { get; set; }

            public FinancialBatch batch { get; set; }
            public string batchExportedDT { get; set; }

            public decimal Variance
            {
                get
                {
                    return TransactionAmount - ControlAmount;
                }
            }

            public string AccountSummaryText
            {
                get
                {
                    var summary = new List<string>();
                    AccountSummaryList.ForEach( a => summary.Add( a.ToString() ) );
                    return summary.AsDelimited( Environment.NewLine );
                }
            }

            public string AccountSummaryHtml
            {
                get
                {
                    var summary = new List<string>();
                    AccountSummaryList.ForEach( a => summary.Add( a.ToString() ) );
                    return "<small>" + summary.AsDelimited( "<br/>" ) + "</small>";
                }
            }

            public string StatusText
            {
                get
                {
                    return Status.ConvertToString();
                }
            }


            public string StatusLabelClass
            {
                get
                {
                    switch ( Status )
                    {
                        case BatchStatus.Closed:
                            return "label label-default";
                        case BatchStatus.Open:
                            return "label label-info";
                        case BatchStatus.Pending:
                            return "label label-warning";
                    }

                    return string.Empty;
                }
            }

            public string Notes
            {
                get
                {
                    var notes = new StringBuilder();

                    switch ( Status )
                    {
                        case BatchStatus.Open:
                            {
                                if ( UnMatchedTxns )
                                {
                                    notes.Append( "<span class='label label-warning'>Unmatched Transactions</span><br/>" );
                                }

                                break;
                            }
                    }

                    notes.Append( BatchNote );

                    return notes.ToString();
                }
            }
        }

        public class GLTransaction
        {
            public string glCompany { get; set; }
            public string glFund { get; set; }
            public string glBankAccount { get; set; }
            public string glRevenueDepartment { get; set; }
            public string glRevenueAccount { get; set; }
            public string projectCode { get; set; }
            public decimal total { get; set; }

            public FinancialBatch batch { get; set; }
            public FinancialTransactionDetail transactionDetail { get; set; }
        }

        public class GLExportLineItem
        {
            [StringLength( 2, ErrorMessage = "AccountingPeriod cannot have more than 2 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "AccountingPeriod must be numeric." )]
            public string AccountingPeriod { get; set; }
            [StringLength( 9, ErrorMessage = "AccountNumber cannot have more than 9 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "AccountNumber must be numeric." )]
            [Required()]
            public string AccountNumber { get; set; }
            public decimal Amount { get; set; }
            [StringLength( 4, ErrorMessage = "CompanyNumber cannot have more than 4 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "CompanyNumber must be numeric." )]
            [Required()]
            public string CompanyNumber { get; set; }
            public DateTime? Date { get; set; }
            [StringLength( 3, ErrorMessage = "DepartmentNumber cannot have more than 3 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "DepartmentNumber must be numeric." )]
            public string DepartmentNumber { get; set; }
            public string Description1 { get; set; }
            public string Description2 { get; set; }
            [StringLength( 5, ErrorMessage = "FundNumber cannot have more than 5 characters in length." )]
            [RegularExpression( "([0-9]+)", ErrorMessage = "FundNumber must be numeric." )]
            [Required()]
            public string FundNumber { get; set; }
            [Range( 0, 99999, ErrorMessage = "JournalNumber cannot have more than 5 characters in length." )]
            public int JournalNumber { get; set; }
            [StringLength( 2, ErrorMessage = "JournalType cannot have more than 2 characters in length." )]
            public string JournalType { get; set; }
            [StringLength( 50, ErrorMessage = "ProjectCode cannot have more than 50 characters in length." )]
            public string ProjectCode { get; set; }
        }

        #endregion
    }
}