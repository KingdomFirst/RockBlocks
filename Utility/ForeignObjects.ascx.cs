using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Utility
{
    /// <summary>
    /// Block that exposes Foreign Objects.
    /// </summary>
    [DisplayName( "Foreign Objects" )]
    [Category( "Utility" )]
    [Description( "This block displays Foreign Objects (Key, Guid, & Id) and allows for a Lava formatted output. Currently Supports; Person, FinancialAccount, FinancialBatch, FinancialPledge, FinancialTransaction, FinancialScheduledTransaction, Group, GroupMember, Metric, Location, PrayerRequest, ContentChannel, ContentChannelItem" )]
    [BooleanField( "Show Edit Link", "Option to hide the Edit link.", order: 1 )]
    [CodeEditorField( "Lava Template", "The Lava template to use to display the foreign objects.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 500, true, @"<div>
    <span class=""label label-type"">{{ Context.Person.ForeignKey }}</span>
    <span class=""label label-type"">{{ Context.Person.ForeignGuid }}</span>
    <span class=""label label-type"">{{ Context.Person.ForeignId }}</span>
</div>
<br />", order: 2 )]
    [BooleanField( "Enable Debug", "Shows the merge fields available for the Lava.", order: 3 )]
    [ContextAware]

    public partial class ForeignObjects : RockBlock
    {
        #region Fields

        IEntity contextEntity = null;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            contextEntity = this.ContextEntity();
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
                DispalyForeignObjects();
            }

            lbEdit.Visible = UserCanEdit;

            if ( contextEntity == null || !GetAttributeValue( "ShowEditLink" ).AsBoolean() )
            {
                lbEdit.Visible = false;
            }
        }

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            DispalyForeignObjects();
        }

        private void DispalyForeignObjects( )
        {
            SetEditMode( false );

            RockContext rockContext = new RockContext();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            var template = GetAttributeValue( "LavaTemplate" );

            lResults.Text = template.ResolveMergeFields( mergeFields );

            // show debug info
            if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
            {
                lDebug.Visible = true;
                lDebug.Text = mergeFields.lavaDebugInfo();
            }
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the lbEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEdit_Click( object sender, EventArgs e )
        {
            if ( contextEntity != null )
            {
                SetEditMode( true );

                tbForeignKey.Text = contextEntity.ForeignKey;
                tbForeignGuid.Text = contextEntity.ForeignGuid.ToString();
                tbForeignId.Text = contextEntity.ForeignId.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            rockContext.WrapTransaction( ( ) =>
            {
                if ( contextEntity is Person )
                {
                    var personService = new PersonService( rockContext );
                    var changes = new List<string>();
                    var _person = personService.Get( contextEntity.Id );

                    History.EvaluateChange( changes, "Foreign Key", _person.ForeignKey, tbForeignKey.Text );
                    _person.ForeignKey = tbForeignKey.Text;

                    History.EvaluateChange( changes, "Foreign Guid", _person.ForeignGuid.ToString(), tbForeignGuid.Text );
                    _person.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();

                    History.EvaluateChange( changes, "Foreign Id", _person.ForeignId.ToString(), tbForeignId.Text );
                    _person.ForeignId = tbForeignId.Text.AsType<int?>();

                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( Person ),
                                Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                                _person.Id,
                                changes );
                        }
                    }
                }
                else if ( contextEntity is FinancialAccount )
                {
                    var accountService = new FinancialAccountService( rockContext );
                    var _account = accountService.Get( contextEntity.Id );

                    _account.ForeignKey = tbForeignKey.Text;
                    _account.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _account.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is FinancialBatch )
                {
                    var batchService = new FinancialBatchService( rockContext );
                    var changes = new List<string>();
                    var _batch = batchService.Get( contextEntity.Id );

                    History.EvaluateChange( changes, "Foreign Key", _batch.ForeignKey, tbForeignKey.Text );
                    _batch.ForeignKey = tbForeignKey.Text;

                    History.EvaluateChange( changes, "Foreign Guid", _batch.ForeignGuid.ToString(), tbForeignGuid.Text );
                    _batch.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();

                    History.EvaluateChange( changes, "Foreign Id", _batch.ForeignId.ToString(), tbForeignId.Text );
                    _batch.ForeignId = tbForeignId.Text.AsType<int?>();

                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( FinancialBatch ),
                                Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                                _batch.Id,
                                changes );
                        }
                    }
                }
                else if ( contextEntity is FinancialPledge )
                {
                    var pledgeService = new FinancialPledgeService( rockContext );
                    var _pledge = pledgeService.Get( contextEntity.Id );

                    _pledge.ForeignKey = tbForeignKey.Text;
                    _pledge.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _pledge.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is FinancialTransaction )
                {
                    var transactionService = new FinancialTransactionService( rockContext );
                    var changes = new List<string>();
                    var _transaction = transactionService.Get( contextEntity.Id );

                    History.EvaluateChange( changes, "Foreign Key", _transaction.ForeignKey, tbForeignKey.Text );
                    _transaction.ForeignKey = tbForeignKey.Text;

                    History.EvaluateChange( changes, "Foreign Guid", _transaction.ForeignGuid.ToString(), tbForeignGuid.Text );
                    _transaction.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();

                    History.EvaluateChange( changes, "Foreign Id", _transaction.ForeignId.ToString(), tbForeignId.Text );
                    _transaction.ForeignId = tbForeignId.Text.AsType<int?>();

                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( FinancialTransaction ),
                                Rock.SystemGuid.Category.HISTORY_FINANCIAL_TRANSACTION.AsGuid(),
                                _transaction.Id,
                                changes );
                        }
                    }
                }
                else if ( contextEntity is FinancialScheduledTransaction )
                {
                    var transactionScheduledService = new FinancialScheduledTransactionService( rockContext );
                    var _scheduledTransaction = transactionScheduledService.Get( contextEntity.Id );

                    _scheduledTransaction.ForeignKey = tbForeignKey.Text;
                    _scheduledTransaction.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _scheduledTransaction.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is Group )
                {
                    var groupService = new GroupService( rockContext );
                    var _group = groupService.Get( contextEntity.Id );

                    _group.ForeignKey = tbForeignKey.Text;
                    _group.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _group.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is GroupMember )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var changes = new List<string>();
                    var _groupMember = groupMemberService.Get( contextEntity.Id );

                    History.EvaluateChange( changes, "Foreign Key", _groupMember.ForeignKey, tbForeignKey.Text );
                    _groupMember.ForeignKey = tbForeignKey.Text;

                    History.EvaluateChange( changes, "Foreign Guid", _groupMember.ForeignGuid.ToString(), tbForeignGuid.Text );
                    _groupMember.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();

                    History.EvaluateChange( changes, "Foreign Id", _groupMember.ForeignId.ToString(), tbForeignId.Text );
                    _groupMember.ForeignId = tbForeignId.Text.AsType<int?>();

                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( GroupMember ),
                                Rock.SystemGuid.Category.HISTORY_PERSON_GROUP_MEMBERSHIP.AsGuid(),
                                _groupMember.Id,
                                changes );
                        }
                    }
                }
                else if ( contextEntity is Metric )
                {
                    var metricService = new MetricService( rockContext );
                    var _metric = metricService.Get( contextEntity.Id );

                    _metric.ForeignKey = tbForeignKey.Text;
                    _metric.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _metric.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is Location )
                {
                    var locationService = new LocationService( rockContext );
                    var _location = locationService.Get( contextEntity.Id );

                    _location.ForeignKey = tbForeignKey.Text;
                    _location.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _location.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is PrayerRequest )
                {
                    var prayerRequestService = new PrayerRequestService( rockContext );
                    var _request = prayerRequestService.Get( contextEntity.Id );

                    _request.ForeignKey = tbForeignKey.Text;
                    _request.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _request.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is ContentChannel )
                {
                    var contentChannelService = new ContentChannelService( rockContext );
                    var _channel = contentChannelService.Get( contextEntity.Id );

                    _channel.ForeignKey = tbForeignKey.Text;
                    _channel.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _channel.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
                else if ( contextEntity is ContentChannelItem )
                {
                    var contentChannelItemService = new ContentChannelItemService( rockContext );
                    var _item = contentChannelItemService.Get( contextEntity.Id );

                    _item.ForeignKey = tbForeignKey.Text;
                    _item.ForeignGuid = tbForeignGuid.Text.AsType<Guid?>();
                    _item.ForeignId = tbForeignId.Text.AsType<int?>();

                    rockContext.SaveChanges();
                }
            } );

            Page.Response.Redirect( Page.Request.Url.ToString(), true );
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            DispalyForeignObjects();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            if ( UserCanEdit )
            {
                pnlEditDetails.Visible = editable;
                pnlViewDetails.Visible = !editable;
            }
        }

        #endregion

    }
}