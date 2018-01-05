using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Finance
{
    [DisplayName( "KFS Pledge Detail" )]
    [Category( "KFS > Finance" )]
    [Description( "Allows the details of a given pledge to be edited." )]
    [GroupTypeField( "Select Group Type", "Optional Group Type that if selected will display a list of groups that pledge can be associated to for selected user", false, "", "", 1 )]
    public partial class PledgeDetail : RockBlock, IDetailBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                ShowDetail( PageParameter( "pledgeId" ).AsInteger() );
            }
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            LoadGroups( ddlGroup.SelectedValueAsInt() );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            FinancialPledge pledge;
            var rockContext = new RockContext();
            var pledgeService = new FinancialPledgeService( rockContext );
            var pledgeId = hfPledgeId.Value.AsInteger();

            if ( pledgeId == 0 )
            {
                pledge = new FinancialPledge();
                pledgeService.Add( pledge );
            }
            else
            {
                pledge = pledgeService.Get( pledgeId );
            }

            pledge.PersonAliasId = ppPerson.PersonAliasId;
            pledge.GroupId = ddlGroup.SelectedValueAsInt();
            pledge.AccountId = apAccount.SelectedValue.AsIntegerOrNull();
            pledge.TotalAmount = tbAmount.Text.AsDecimal();

            pledge.StartDate = dpDateRange.LowerValue.HasValue ? dpDateRange.LowerValue.Value : DateTime.MinValue;
            pledge.EndDate = dpDateRange.UpperValue.HasValue ? dpDateRange.UpperValue.Value : DateTime.MaxValue;

            pledge.PledgeFrequencyValueId = ddlFrequencyType.SelectedValue.AsIntegerOrNull();

            if ( !pledge.IsValid )
            {
                // Controls will render the error messages
                return;
            }

            rockContext.SaveChanges();

            NavigateToParentPage();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="pledgeId">The pledge identifier.</param>
        public void ShowDetail( int pledgeId )
        {
            pnlDetails.Visible = true;
            var frequencyTypeGuid = new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_FREQUENCY );
            ddlFrequencyType.BindToDefinedType( DefinedTypeCache.Read( frequencyTypeGuid ), true );

            var personId = PageParameter( "personId" ).AsIntegerOrNull();

            using ( var rockContext = new RockContext() )
            {
                FinancialPledge pledge = null;

                if ( pledgeId > 0 )
                {
                    pledge = new FinancialPledgeService( rockContext ).Get( pledgeId );
                    lActionTitle.Text = ActionTitle.Edit( FinancialPledge.FriendlyTypeName ).FormatAsHtmlTitle();
                    pdAuditDetails.SetEntity( pledge, ResolveRockUrl( "~" ) );
                }

                if ( pledge == null )
                {
                    pledge = new FinancialPledge();
                    lActionTitle.Text = ActionTitle.Add( FinancialPledge.FriendlyTypeName ).FormatAsHtmlTitle();
                    // hide the panel drawer that show created and last modified dates
                    pdAuditDetails.Visible = false;
                }

                var isReadOnly = !IsUserAuthorized( Authorization.EDIT );
                var isNewPledge = pledge.Id == 0;

                hfPledgeId.Value = pledge.Id.ToString();
                Person person = null;

                if ( pledge.PersonAlias != null )
                {
                    person = pledge.PersonAlias.Person;

                }
                else if ( personId.HasValue )
                {
                    person = new PersonService( rockContext ).Get( (int)personId );
                }

                ppPerson.SetValue( person );

                ppPerson.Enabled = !isReadOnly;

                GroupType groupType = null;
                Guid? groupTypeGuid = GetAttributeValue( "SelectGroupType" ).AsGuidOrNull();
                if ( groupTypeGuid.HasValue )
                {
                    groupType = new GroupTypeService( rockContext ).Get( groupTypeGuid.Value );
                }

                if ( groupType != null )
                {
                    ddlGroup.Label = groupType.Name;
                    ddlGroup.Visible = true;
                    LoadGroups( pledge.GroupId );
                    ddlGroup.Enabled = !isReadOnly;
                }
                else
                {
                    ddlGroup.Visible = false;
                }

                apAccount.SetValue( pledge.Account );
                apAccount.Enabled = !isReadOnly;
                tbAmount.Text = !isNewPledge ? pledge.TotalAmount.ToString() : string.Empty;
                tbAmount.ReadOnly = isReadOnly;

                dpDateRange.LowerValue = pledge.StartDate;
                dpDateRange.UpperValue = pledge.EndDate;
                dpDateRange.ReadOnly = isReadOnly;

                ddlFrequencyType.SelectedValue = !isNewPledge ? pledge.PledgeFrequencyValueId.ToString() : string.Empty;
                ddlFrequencyType.Enabled = !isReadOnly;

                if ( isReadOnly )
                {
                    nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( FinancialPledge.FriendlyTypeName );
                    lActionTitle.Text = ActionTitle.View( BlockType.FriendlyTypeName );
                    btnCancel.Text = "Close";
                }

                btnSave.Visible = !isReadOnly;
            }
        }

        private void LoadGroups( int? currentGroupId )
        {
            ddlGroup.Items.Clear();

            int? personId = ppPerson.PersonId;
            Guid? groupTypeGuid = GetAttributeValue( "SelectGroupType" ).AsGuidOrNull();
            if ( personId.HasValue && groupTypeGuid.HasValue  )
            {
                using ( var rockContext = new RockContext() )
                {
                    var groups = new GroupMemberService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( m =>
                            m.Group.GroupType.Guid == groupTypeGuid.Value &&
                            m.PersonId == personId.Value &&
                            m.GroupMemberStatus == GroupMemberStatus.Active &&
                            m.Group.IsActive )
                        .Select( m => new
                        {
                            m.GroupId,
                            Name = m.Group.Name
                        } )
                        .ToList()
                        .Distinct()
                        .OrderBy( g => g.Name )
                        .ToList();

                    if ( groups.Any() )
                    {
                        ddlGroup.DataSource = groups;
                        ddlGroup.DataBind();
                        ddlGroup.Items.Insert(0, new ListItem() );
                        ddlGroup.SetValue( currentGroupId );
                    }
                }
            }
        }

    }
}