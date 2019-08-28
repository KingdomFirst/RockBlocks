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
using System.Data;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;

using CsvHelper;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.Bulldozer
{
    #region Block Attributes

    [DisplayName( "Bulldozer Export" )]
    [Category( "KFS > Bulldozer" )]
    [Description( "Block to export Bulldozer files." )]

    #endregion

    #region Block Settings

    [IntegerField( "Remaing Disk Percentage", "The remain percentage of disk space on the server drive which Rock is located to serve as a safety to run this utility.", true, 10, "", 0 )]
    [IntegerField( "Command Timeout", "Maximum amount of time (in seconds) to wait for the SQL Query to complete.", true, 180, "", 1 )]

    #endregion

    /// <summary>
    /// Block to export Bulldozer files.
    /// </summary>
    public partial class ExportTool : RockBlock
    {
        #region Fields

        private int _commandTimeout = 180;
        private List<int> _entityTypeIdList = new List<int>();
        private int? _personEntityTypeId = null;
        private int? _groupEntityTypeId = null;
        private int? _attributeEntityTypeId = null;
        private int? _groupMemberEntityTypeId = null;
        private int? _prayerRequestEntityTypeId = null;
        private List<Group> _groupList = new List<Group>();
        private List<int> _groupIdList = new List<int>();
        private List<Location> _namedLocationList = new List<Location>();
        private List<int> _namedLocationIdList = new List<int>();

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;

            _personEntityTypeId = EntityTypeCache.GetId<Person>();
            _groupEntityTypeId = EntityTypeCache.GetId<Group>();
            _attributeEntityTypeId = EntityTypeCache.GetId<Rock.Model.Attribute>();
            _groupMemberEntityTypeId = EntityTypeCache.GetId<GroupMember>();
            _prayerRequestEntityTypeId = EntityTypeCache.GetId<PrayerRequest>();
            _commandTimeout = GetAttributeValue( "CommandTimeout" ).AsInteger();

            dvpPersonDataView.EntityTypeId = _personEntityTypeId;
            dvpGroupDataView.EntityTypeId = _groupEntityTypeId;
            dvpAttributeDataView.EntityTypeId = _attributeEntityTypeId;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                pnlExportStatus.Visible = false;

                Session["log"] = null;
                Session["finished"] = null;

                ddlAttributeIncludeExlude.Items.Add( new ListItem( "Include" ) );
                ddlAttributeIncludeExlude.Items.Add( new ListItem( "Exclude" ) );
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Triggers the log event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message.</param>
        private void LogEvent( string message )
        {
            if ( !string.IsNullOrWhiteSpace( message ) )
            {
                var log = string.Empty;
                var isEmpty = Session["log"] == null;

                if ( isEmpty )
                {
                    log = message;
                }
                else
                {
                    log = string.Format( "{0}|{1}", Session["log"], message );
                }

                Session["log"] = log;
            }
        }

        /// <summary>
        /// Handles the Tick event of the tmrSyncExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tmrSyncExport_Tick( object sender, EventArgs e )
        {
            if ( Session["log"] != null )
            {
                var builder = new System.Text.StringBuilder();
                builder.Append( lblExportStatus.Text );
                foreach ( string s in Session["log"].ToString().Split( new char[] { '|' } ) )
                {
                    if ( !string.IsNullOrWhiteSpace( s ) )
                    {
                        builder.Append( string.Format( "<li>{0} {1}: {2}</li>", RockDateTime.Now.ToShortDateString(), RockDateTime.Now.ToLongTimeString(), s ) );
                    }
                }
                lblExportStatus.Text = builder.ToString();

                Session["log"] = string.Empty;
            }

            if ( Session["finished"] != null && ( bool ) Session["finished"] )
            {
                Thread.Sleep( 2500 );
                tmrSyncExport.Enabled = false;
                btnExport.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            nbError.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnExport_Click( object sender, EventArgs e )
        {
            Session["log"] = null;
            Session["finished"] = null;
            lblExportStatus.Text = string.Empty;
            pnlExportStatus.Visible = true;

            btnExport.Enabled = false;
            tmrSyncExport.Enabled = true;
            nbError.Visible = false;

            var bulldozerDirectory = Server.MapPath( "~/App_Data/Bulldozer/" );
            var csvFileDirectory = bulldozerDirectory + "CSVs\\";
            var personImageDirectory = bulldozerDirectory + "PersonImage\\";
            string errorMessage;
            var safe = IsDriveSafe( bulldozerDirectory.Left( 3 ), out errorMessage );

            if ( safe )
            {
                #region Variables

                var personDataViewId = dvpPersonDataView.SelectedValue.AsIntegerOrNull();
                var groupDataViewId = dvpGroupDataView.SelectedValue.AsIntegerOrNull();
                var attributeDataViewId = dvpAttributeDataView.SelectedValue.AsIntegerOrNull();
                var personList = new List<Person>();
                var personIdList = new List<int>();
                var personIdDictionary = new Dictionary<int, int>();
                var personAliasList = new List<int>();
                var campusIdList = new List<int>();
                var connectionRequestList = new List<ConnectionRequest>();
                var prayerRequestList = new List<PrayerRequest>();
                var prayerRequestIdList = new List<int>();
                var groupMemberList = new List<GroupMember>();
                var groupMemberIdList = new List<int>();
                var attendanceList = new List<Attendance>();
                var attendanceLocationList = new List<Location>();
                var attributeList = new List<Rock.Model.Attribute>();
                var attributeIdList = new List<int>();
                var attributeValueList = new List<AttributeValue>();
                var attributeIdsInAttributeValueList = new List<int>();

                var bulldozerIndividualList = new List<BulldozerIndividual>();
                var bulldozerFamilyList = new List<BulldozerFamily>();
                var bulldozerPreviousLastNameList = new List<BulldozerPreviousLastName>();
                var bulldozerPhoneNumberList = new List<BulldozerPhoneNumber>();
                var bulldozerConnectionRequestList = new List<BulldozerConnectionRequest>();
                var bulldozerPrayerRequestList = new List<BulldozerPrayerRequest>();
                var bulldozerUserLoginList = new List<BulldozerUserLogin>();
                var bulldozerGroupList = new List<BulldozerGroup>();
                var bulldozerGroupTypeList = new List<BulldozerGroupType>();
                var bulldozerGroupMemberList = new List<BulldozerGroupMember>();
                var bulldozerNamedLocationList = new List<BulldozerNamedLocation>();
                var bulldozerAttendanceList = new List<BulldozerAttendance>();
                var bulldozerEntityAttributeList = new List<BulldozerEntityAttribute>();
                var bulldozerEntityAttributeValueList = new List<BulldozerEntityAttributeValue>();
                var bulldozerNoteList = new List<BulldozerNote>();

                #endregion

                #region Create Directories

                if ( Directory.Exists( bulldozerDirectory ) )
                {
                    DeleteDirectory( bulldozerDirectory );
                }

                Directory.CreateDirectory( bulldozerDirectory );
                Directory.CreateDirectory( csvFileDirectory );
                Directory.CreateDirectory( personImageDirectory );

                #endregion

                var exportTask = Task.Run( () =>
                {
                    Thread.Sleep( 1000 );

                    using ( var rockContext = new RockContext() )
                    {
                        rockContext.Database.CommandTimeout = _commandTimeout;

                        #region Data Views

                        var personDataView = new DataViewService( rockContext ).GetNoTracking( personDataViewId ?? 0 );
                        var groupDataView = new DataViewService( rockContext ).GetNoTracking( groupDataViewId ?? 0 );
                        var attributeDataView = new DataViewService( rockContext ).GetNoTracking( attributeDataViewId ?? 0 );

                        #endregion

                        if ( personDataView == null && groupDataView == null )
                        {
                            nbError.Heading = "Error";
                            nbError.Text = "You must select a Person Data View and/or Group Data View.";
                            nbError.Visible = true;
                        }
                        else
                        {
                            if ( personDataView != null )
                            {
                                #region People

                                LogEvent( "Beginning Person Export..." );

                                var personService = new PersonService( rockContext );
                                var errorMessages = new List<string>();
                                var paramExpression = personService.ParameterExpression;
                                var whereExpression = personDataView.GetExpression( personService, paramExpression, out errorMessages );

                                personList = personService
                                    .Queryable( true, true )
                                    .AsNoTracking()
                                    .Where( paramExpression, whereExpression, null )
                                    .OrderBy( i => i.PrimaryFamily.Name )
                                    .ThenBy( i => i.PrimaryFamilyId )
                                    .ThenBy( i => i.BirthDate )
                                    .ThenBy( i => i.Gender )
                                    .ThenBy( i => i.Id )
                                    .ToList();

                                personIdList = personList
                                    .Select( p => p.Id )
                                    .ToList();

                                personIdDictionary = new PersonAliasService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( a => personIdList.Contains( a.PersonId ) )
                                    .ToDictionary( k => k.Id, v => v.PersonId );

                                personAliasList = personIdDictionary.Keys.ToList();

                                bulldozerIndividualList = personList
                                    .Select( p => new BulldozerIndividual
                                    {
                                        FamilyId = p.PrimaryFamilyId,
                                        FamilyName = p.PrimaryFamilyId.HasValue ? p.PrimaryFamily.Name : string.Empty,
                                        CreatedDate = p.CreatedDateTime.HasValue ? p.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                        PersonId = p.Id,
                                        Prefix = p.TitleValueId.HasValue ? p.TitleValue.Value : string.Empty,
                                        FirstName = p.FirstName,
                                        NickName = p.NickName,
                                        MiddleName = p.MiddleName,
                                        LastName = p.LastName,
                                        Suffix = p.SuffixValueId.HasValue ? p.SuffixValue.Value : string.Empty,
                                        FamilyRole = p.GetFamilyRole().Name,
                                        MaritalStatus = p.MaritalStatusValueId.HasValue ? p.MaritalStatusValue.Value : string.Empty,
                                        ConnectionStatus = p.ConnectionStatusValueId.HasValue ? p.ConnectionStatusValue.Value : string.Empty,
                                        RecordStatus = p.RecordStatusReasonValueId.HasValue ? p.RecordStatusValue.Value : string.Empty,
                                        IsDeceased = p.IsDeceased,
                                        HomePhone = string.Empty,
                                        MobilePhone = string.Empty,
                                        WorkPhone = string.Empty,
                                        AllowSMS = null,
                                        Email = p.Email,
                                        IsEmailActive = p.IsEmailActive,
                                        AllowBulkEmail = ( p.EmailPreference == EmailPreference.EmailAllowed ),
                                        Gender = p.Gender.ToString(),
                                        DateOfBirth = p.BirthDate.HasValue ? p.BirthDate.Value.ToShortDateString() : string.Empty,
                                        School = string.Empty,
                                        GraduationDate = p.GraduationYear.HasValue ? new DateTime( p.GraduationYear.Value, 6, 1 ).ToShortDateString() : string.Empty,
                                        Anniversary = p.AnniversaryDate.HasValue ? p.AnniversaryDate.Value.ToShortDateString() : string.Empty,
                                        GeneralNote = string.Empty,
                                        MedicalNote = string.Empty,
                                        SecurityNote = string.Empty
                                    } )
                                    .ToList();

                                LogEvent( "Person Export Completed." );

                                #endregion

                                #region Families

                                LogEvent( "Beginning Family Export..." );

                                var firstPersonInFamilyList = personList
                                    .Where( p => p.PrimaryFamilyId != null )
                                    .GroupBy( p => p.PrimaryFamilyId )
                                    .Select( grp => grp.First() )
                                    .ToList();

                                campusIdList = firstPersonInFamilyList
                                    .Where( p => p.PrimaryFamily.CampusId != null )
                                    .GroupBy( p => p.PrimaryFamily.CampusId )
                                    .Select( grp => grp.First() )
                                    .Select( p => p.PrimaryFamily.CampusId.Value )
                                    .ToList();

                                var bulldozerFamilyAddresses = new List<BulldozerGroupAddress>();
                                bulldozerFamilyAddresses = firstPersonInFamilyList
                                    .Select( p => new BulldozerGroupAddress
                                    {
                                        Group = p.PrimaryFamily,
                                        Location1 = p.GetMailingLocation( rockContext ),
                                        Location2 = p.GetMailingLocation( rockContext )
                                    } )
                                    .OrderBy( f => f.Group.Name )
                                    .ThenBy( f => f.Group.Id )
                                    .ToList();

                                bulldozerFamilyList = bulldozerFamilyAddresses
                                    .Select( f => new BulldozerFamily
                                    {
                                        FamilyId = f.Group.Id,
                                        FamilyName = f.Group.Name,
                                        CreatedDate = f.Group.CreatedDateTime.HasValue ? f.Group.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                        Campus = f.Group.CampusId.HasValue ? f.Group.Campus.Name : string.Empty,
                                        Address = ( f.Location1 != null ) ? f.Location1.Street1 : string.Empty,
                                        Address2 = ( f.Location1 != null ) ? f.Location1.Street2 : string.Empty,
                                        City = ( f.Location1 != null ) ? f.Location1.City : string.Empty,
                                        State = ( f.Location1 != null ) ? f.Location1.State : string.Empty,
                                        Zip = ( f.Location1 != null ) ? f.Location1.PostalCode : string.Empty,
                                        Country = ( f.Location1 != null ) ? f.Location1.Country : string.Empty,
                                        SecondaryAddress = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.Street1 : string.Empty,
                                        SecondaryAddress2 = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.Street2 : string.Empty,
                                        SecondaryCity = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.City : string.Empty,
                                        SecondaryState = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.State : string.Empty,
                                        SecondaryZip = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.PostalCode : string.Empty,
                                        SecondaryCountry = ( f.Location2 != null && f.Location2.Id != f.Location1.Id ) ? f.Location2.Country : string.Empty,
                                    } )
                                    .OrderBy( f => f.FamilyName )
                                    .ThenBy( f => f.FamilyId )
                                    .ToList();

                                LogEvent( "Family Export Completed." );

                                #endregion

                                #region Previous Last Names

                                LogEvent( "Beginning Previous Last Name Export..." );

                                foreach ( var p in personList.Where( p => p.GetPreviousNames().Any() ) )
                                {
                                    foreach ( var l in p.GetPreviousNames().ToList() )
                                    {
                                        bulldozerPreviousLastNameList.Add( new BulldozerPreviousLastName
                                        {
                                            PreviousLastNamePersonId = l.PersonAlias.PersonId,
                                            PreviousLastName = l.LastName,
                                            PreviousLastNameId = l.Id
                                        } );
                                    }
                                }

                                LogEvent( "Previous Last Name Export Completed." );

                                #endregion

                                #region Phone Numbers

                                LogEvent( "Beginning Phone Number Export..." );

                                foreach ( var p in personList.Where( p => p.PhoneNumbers.Count > 0 ) )
                                {
                                    foreach ( var n in p.PhoneNumbers )
                                    {
                                        bulldozerPhoneNumberList.Add( new BulldozerPhoneNumber
                                        {
                                            PhonePersonId = n.PersonId,
                                            PhoneType = n.NumberTypeValue.Value,
                                            Phone = n.NumberFormatted,
                                            PhoneIsMessagingEnabled = n.IsMessagingEnabled,
                                            PhoneIsUnlisted = n.IsUnlisted,
                                            PhoneId = n.Id
                                        } );
                                    }
                                }

                                LogEvent( "Phone Number Export Completed." );

                                #endregion

                                #region Connection Requests

                                if ( cbConnectionRequests.Checked )
                                {
                                    LogEvent( "Beginning Connection Request Export..." );

                                    connectionRequestList = new ConnectionRequestService( rockContext )
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( r => personAliasList.Contains( r.PersonAliasId ) )
                                        .ToList();

                                    bulldozerConnectionRequestList.AddRange(
                                        connectionRequestList
                                        .Select( r => new BulldozerConnectionRequest
                                        {
                                            OpportunityForeignKey = r.ConnectionOpportunityId,
                                            OpportunityName = r.ConnectionOpportunity.Name,
                                            ConnectionType = r.ConnectionOpportunity.ConnectionType.Name,
                                            OpportunityDescription = r.ConnectionOpportunity.Description != null ? r.ConnectionOpportunity.Description.RemoveCrLf() : string.Empty,
                                            OpportunityActive = r.ConnectionOpportunity.IsActive,
                                            OpportunityCreated = r.ConnectionOpportunity.CreatedDateTime,
                                            OpportunityModified = r.ConnectionOpportunity.ModifiedDateTime,
                                            RequestForeignKey = r.Id,
                                            RequestPersonId = r.PersonAlias.PersonId,
                                            RequestConnectorId = r.ConnectorPersonAliasId.HasValue ? r.ConnectorPersonAlias.PersonId : 0,
                                            RequestCreated = r.CreatedDateTime,
                                            RequestModified = r.ModifiedDateTime,
                                            RequestStatus = r.ConnectionStatus.Name,
                                            RequestState = r.ConnectionState.ConvertToInt(),
                                            RequestComments = r.Comments != null ? r.Comments.RemoveCrLf() : string.Empty,
                                            RequestFollowUp = r.FollowupDate
                                        } ) );

                                    var connectionRequestActivityList = new ConnectionRequestActivityService( rockContext )
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( a =>
                                            personAliasList.Contains( a.ConnectionRequest.PersonAliasId ) &&
                                            a.ConnectionOpportunityId != null &&
                                            !( a.Note == null || a.Note.Trim() == string.Empty ) )
                                        .ToList();

                                    bulldozerConnectionRequestList.AddRange(
                                        connectionRequestActivityList
                                        .Select( a => new BulldozerConnectionRequest
                                        {
                                            OpportunityForeignKey = a.ConnectionOpportunityId,
                                            OpportunityName = a.ConnectionOpportunity.Name,
                                            ConnectionType = a.ConnectionOpportunity.ConnectionType.Name,
                                            OpportunityDescription = a.ConnectionOpportunity.Description != null ? a.ConnectionOpportunity.Description.RemoveCrLf() : string.Empty,
                                            OpportunityActive = a.ConnectionOpportunity.IsActive,
                                            OpportunityCreated = a.ConnectionOpportunity.CreatedDateTime,
                                            OpportunityModified = a.ConnectionOpportunity.ModifiedDateTime,
                                            RequestForeignKey = a.ConnectionRequest.Id,
                                            RequestPersonId = a.ConnectionRequest.PersonAlias.PersonId,
                                            RequestConnectorId = a.ConnectionRequest.ConnectorPersonAliasId.HasValue ? a.ConnectionRequest.ConnectorPersonAlias.PersonId : 0,
                                            RequestCreated = a.ConnectionRequest.CreatedDateTime,
                                            RequestModified = a.ConnectionRequest.ModifiedDateTime,
                                            RequestStatus = a.ConnectionRequest.ConnectionStatus.Name,
                                            RequestState = a.ConnectionRequest.ConnectionState.ConvertToInt(),
                                            RequestComments = a.ConnectionRequest.Comments != null ? a.ConnectionRequest.Comments.RemoveCrLf() : string.Empty,
                                            RequestFollowUp = a.ConnectionRequest.FollowupDate,
                                            ActivityType = a.ConnectionActivityType.Name,
                                            ActivityNote = a.Note.RemoveCrLf(),
                                            ActivityDate = a.CreatedDateTime,
                                            ActivityConnectorId = a.ConnectorPersonAliasId.HasValue ? a.ConnectorPersonAlias.PersonId : 0
                                        } ) );

                                    LogEvent( "Connection Request Export Completed." );
                                }

                                #endregion

                                #region Prayer Requests

                                if ( cbPrayerRequets.Checked )
                                {
                                    LogEvent( "Beginning Prayer Request Export..." );

                                    prayerRequestList = new PrayerRequestService( rockContext )
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( p =>
                                            ( p.RequestedByPersonAliasId != null && personAliasList.Contains( p.RequestedByPersonAliasId.Value ) ) ||
                                            ( p.CampusId != null && campusIdList.Contains( p.CampusId.Value ) ) )
                                        .ToList();

                                    prayerRequestIdList = prayerRequestList
                                        .Select( p => p.Id )
                                        .ToList();

                                    bulldozerPrayerRequestList.AddRange(
                                        prayerRequestList
                                        .Select( p => new BulldozerPrayerRequest
                                        {
                                            PrayerRequestCategory = p.CategoryId.HasValue ? p.Category.Name : string.Empty,
                                            PrayerRequestText = p.Text,
                                            PrayerRequestDate = p.EnteredDateTime,
                                            PrayerRequestId = p.Id,
                                            PrayerRequestFirstName = p.FirstName,
                                            PrayerRequestLastName = p.LastName,
                                            PrayerRequestEmail = p.Email,
                                            PrayerRequestExpireDate = p.ExpirationDate,
                                            PrayerRequestAllowComments = p.AllowComments,
                                            PrayerRequestIsPublic = p.IsPublic,
                                            PrayerRequestIsApproved = p.IsApproved,
                                            PrayerRequestApprovedDate = p.ApprovedOnDateTime,
                                            PrayerRequestApprovedById = p.ApprovedByPersonAliasId.HasValue ? p.ApprovedByPersonAlias.PersonId : 0,
                                            PrayerRequestCreatedById = p.CreatedByPersonId,
                                            PrayerRequestRequestedById = p.RequestedByPersonAliasId.HasValue ? p.RequestedByPersonAlias.PersonId : 0,
                                            PrayerRequestAnswerText = p.Answer,
                                            PrayerRequestCampus = p.CampusId.HasValue ? p.Campus.Name : string.Empty
                                        } ) );

                                    LogEvent( "Prayer Request Export Completed." );
                                }

                                #endregion

                                #region User Logins

                                if ( cbUserLogins.Checked )
                                {
                                    LogEvent( "Beginning User Login Export..." );

                                    bulldozerUserLoginList = new UserLoginService( rockContext )
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( l => l.PersonId != null && personIdList.Contains( l.PersonId.Value ) )
                                        .OrderBy( l => l.PersonId )
                                        .ThenBy( l => l.Id )
                                        .Select( l => new BulldozerUserLogin
                                        {
                                            UserLoginId = l.Id,
                                            UserLoginPersonId = l.PersonId.Value,
                                            UserLoginUserName = l.UserName,
                                            UserLoginPassword = string.Empty,
                                            UserLoginDateCreated = l.CreatedDateTime,
                                            UserLoginAuthenticationType = l.EntityTypeId.HasValue ? l.EntityType.Name : string.Empty,
                                            UserLoginIsConfirmed = false
                                        } )
                                        .ToList();

                                    LogEvent( "User Login Export Completed." );
                                }

                                #endregion
                            }

                            #region Attendance

                            if ( cbAttendance.Checked && personIdList.Count > 0 )
                            {
                                LogEvent( "Beginning Attendance Export..." );

                                attendanceList = new AttendanceService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( a => a.PersonAliasId != null && personAliasList.Contains( a.PersonAliasId.Value ) )
                                    .OrderBy( a => a.Occurrence.GroupId )
                                    .ThenBy( a => a.Occurrence.Id )
                                    .ThenBy( a => a.Id )
                                    .ToList();

                                bulldozerAttendanceList = attendanceList
                                    .Select( a => new BulldozerAttendance
                                    {
                                        AttendanceId = a.Id,
                                        AttendanceGroupId = a.Occurrence.GroupId,
                                        AttendancePersonId = a.PersonAlias.PersonId,
                                        AttendanceCreatedDate = a.CreatedDateTime,
                                        AttendanceDate = a.StartDateTime,
                                        AttendanceAttended = a.DidAttend,
                                        AttendanceLocationId = a.Occurrence.LocationId
                                    } )
                                    .ToList();

                                LogEvent( "Attendance Export Completed." );
                            }

                            #endregion

                            #region Groups

                            if ( groupDataView != null )
                            {
                                LogEvent( "Beginning Group Export..." );

                                var groupService = new GroupService( rockContext );
                                var errorMessages = new List<string>();
                                var paramExpression = groupService.ParameterExpression;
                                var whereExpression = groupDataView.GetExpression( groupService, paramExpression, out errorMessages );

                                _groupList.AddRange( groupService
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( paramExpression, whereExpression, null )
                                    .ToList() );

                                _groupIdList = _groupList
                                    .Select( g => g.Id )
                                    .ToList();
                            }

                            if ( attendanceList.Count > 0 )
                            {
                                LogEvent( "Beginning Attendance Group Export..." );

                                _groupList.AddRange( attendanceList
                                    .Where( a => a.Occurrence.GroupId.HasValue && !_groupIdList.Contains( a.Occurrence.GroupId.Value ) )
                                    .GroupBy( a => a.Occurrence.GroupId )
                                    .Select( grp => grp.First() )
                                    .Select( a => a.Occurrence.Group )
                                    .ToList() );

                                _groupIdList = _groupList
                                    .Select( g => g.Id )
                                    .ToList();
                            }

                            GetParentGroups( _groupList );

                            if ( _groupList.Count > 0 )
                            {
                                LogEvent( "Fetching Group Locations..." );

                                var bulldozerGroupAddresses = new List<BulldozerGroupAddress>();
                                bulldozerGroupAddresses = _groupList
                                    .Select( g => new BulldozerGroupAddress
                                    {
                                        Group = g,
                                        Location1 = ( g.GroupLocations.Count > 0 && !IsNamedLocation( g.GroupLocations.FirstOrDefault().Location ) ) ? g.GroupLocations.FirstOrDefault().Location : null,
                                        Location2 = ( g.GroupLocations.Count > 1 && !IsNamedLocation( g.GroupLocations.ElementAtOrDefault( 1 ).Location ) ) ? g.GroupLocations.ElementAtOrDefault( 1 ).Location : null,
                                        NamedLocation = ( g.GroupLocations.Count > 0 && IsNamedLocation( g.GroupLocations.FirstOrDefault().Location ) ) ? g.GroupLocations.FirstOrDefault().Location.Name : string.Empty
                                    } )
                                    .ToList();

                                LogEvent( "Finalizing Group Export..." );

                                bulldozerGroupList = bulldozerGroupAddresses
                                    .Select( g => new BulldozerGroup
                                    {
                                        GroupId = g.Group.Id,
                                        GroupName = g.Group.Name,
                                        GroupCreatedDate = g.Group.CreatedDateTime,
                                        GroupType = g.Group.GroupType.Name,
                                        GroupParentGroupId = g.Group.ParentGroupId,
                                        GroupActive = g.Group.IsActive,
                                        GroupOrder = g.Group.Order,
                                        GroupCampus = g.Group.CampusId.HasValue ? g.Group.Campus.Name : string.Empty,
                                        GroupAddress = ( g.Location1 != null ) ? g.Location1.Street1 : string.Empty,
                                        GroupAddress2 = ( g.Location1 != null ) ? g.Location1.Street2 : string.Empty,
                                        GroupCity = ( g.Location1 != null ) ? g.Location1.City : string.Empty,
                                        GroupState = ( g.Location1 != null ) ? g.Location1.State : string.Empty,
                                        GroupZip = ( g.Location1 != null ) ? g.Location1.PostalCode : string.Empty,
                                        GroupCountry = ( g.Location1 != null ) ? g.Location1.Country : string.Empty,
                                        GroupSecondaryAddress = ( g.Location2 != null ) ? g.Location2.Street1 : string.Empty,
                                        GroupSecondaryAddress2 = ( g.Location2 != null ) ? g.Location2.Street2 : string.Empty,
                                        GroupSecondaryCity = ( g.Location2 != null ) ? g.Location2.City : string.Empty,
                                        GroupSecondaryState = ( g.Location2 != null ) ? g.Location2.State : string.Empty,
                                        GroupSecondaryZip = ( g.Location2 != null ) ? g.Location2.PostalCode : string.Empty,
                                        GroupSecondaryCountry = ( g.Location2 != null ) ? g.Location2.Country : string.Empty,
                                        GroupNamedLocation = g.NamedLocation,
                                        GroupDayOfWeek = ( g.Group.ScheduleId.HasValue && g.Group.Schedule.WeeklyDayOfWeek.HasValue ) ? g.Group.Schedule.WeeklyDayOfWeek.Value.ConvertToString() : string.Empty,
                                        GroupTime = ( g.Group.ScheduleId.HasValue && g.Group.Schedule.WeeklyTimeOfDay.HasValue ) ? g.Group.Schedule.WeeklyTimeOfDay.Value.ToTimeString() : string.Empty,
                                        GroupDescription = g.Group.Description.IsNotNullOrWhiteSpace() ? g.Group.Description.RemoveCrLf() : string.Empty,
                                        GroupCapacity = g.Group.GroupCapacity
                                    } )
                                    .ToList();

                                LogEvent( "Group Export Completed." );
                            }

                            #endregion

                            #region Group Types

                            if ( _groupIdList.Count > 0 )
                            {
                                LogEvent( "Beginning Group Type Export..." );

                                var groupTypeList = _groupList
                                    .GroupBy( g => g.GroupTypeId )
                                    .Select( grp => grp.First() )
                                    .Select( g => g.GroupType )
                                    .ToList();

                                bulldozerGroupTypeList = groupTypeList
                                     .Select( t => new BulldozerGroupType
                                     {
                                         GroupTypeId = t.Id,
                                         GroupTypeName = t.Name,
                                         GroupTypeCreatedDate = t.CreatedDateTime,
                                         GroupTypePurpose = t.GroupTypePurposeValueId.HasValue ? t.GroupTypePurposeValue.Guid.ToString() : null,
                                         GroupTypeInheritedGroupType = t.InheritedGroupTypeId.HasValue ? t.InheritedGroupType.Guid.ToString() : null,
                                         GroupTypeTakesAttendance = t.TakesAttendance,
                                         GroupTypeWeekendService = t.AttendanceCountsAsWeekendService,
                                         GroupTypeShowInGroupList = t.ShowInGroupList,
                                         GroupTypeShowInNav = t.ShowInNavigation,
                                         GroupTypeParentId = t.ParentGroupTypes.Count > 0 ? t.ParentGroupTypes.FirstOrDefault().Id : 0,
                                         GroupTypeSelfReference = ( t.ParentGroupTypes.Count > 0 && t.ParentGroupTypes.FirstOrDefault( s => s.Id == t.Id ) != null ) ? true : false,
                                         GroupTypeWeeklySchedule = t.AllowedScheduleTypes == ScheduleType.Weekly ? true : false
                                     } )
                                     .ToList();

                                LogEvent( "Group Type Export Completed." );
                            }

                            #endregion

                            #region Group Members

                            if ( _groupIdList.Count > 0 && personIdList.Count > 0 )
                            {
                                LogEvent( "Beginning Group Member Export..." );

                                groupMemberList = new GroupMemberService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( m => _groupIdList.Contains( m.GroupId ) && personIdList.Contains( m.PersonId ) )
                                    .OrderBy( m => m.GroupId )
                                    .ThenBy( m => m.Id )
                                    .ToList();

                                bulldozerGroupMemberList = groupMemberList
                                    .Select( m => new BulldozerGroupMember
                                    {
                                        GroupMemberId = m.Id,
                                        GroupMemberGroupId = m.GroupId,
                                        GroupMemberPersonId = m.PersonId,
                                        GroupMemberCreatedDate = m.CreatedDateTime,
                                        GroupMemberRole = m.GroupRole.Name,
                                        GroupMemberActive = !m.IsArchived
                                    } )
                                    .ToList();

                                LogEvent( "Group Member Export Completed." );
                            }

                            #endregion

                            #region Named Locations

                            _namedLocationList.AddRange( _groupList
                                .Where( g => g.GroupLocations.Count > 0 && IsNamedLocation( g.GroupLocations.FirstOrDefault().Location ) )
                                .GroupBy( g => g.GroupLocations.FirstOrDefault().LocationId )
                                .Select( grp => grp.First() )
                                .Select( g => g.GroupLocations.FirstOrDefault().Location )
                                .ToList() );

                            _namedLocationIdList = _namedLocationList
                                .Select( l => l.Id )
                                .ToList();

                            if ( attendanceList.Count > 0 )
                            {
                                LogEvent( "Beginning Named Location Export..." );

                                _namedLocationList.AddRange( attendanceList
                                    .Where( a => a.Occurrence.LocationId.HasValue && !_namedLocationIdList.Contains( a.Occurrence.LocationId.Value ) )
                                    .GroupBy( a => a.Occurrence.LocationId )
                                    .Select( grp => grp.First() )
                                    .Select( l => l.Occurrence.Location )
                                    .ToList() );

                                // reset the id list after new adds
                                _namedLocationIdList = _namedLocationList
                                    .Select( l => l.Id )
                                    .ToList();

                                LogEvent( "Named Location Export Completed." );
                            }

                            GetParentLocations( _namedLocationList );

                            bulldozerNamedLocationList = _namedLocationList
                                .OrderBy( l => l.ParentLocationId )
                                .ThenBy( l => l.Id )
                                .Select( l => new BulldozerNamedLocation
                                {
                                    NamedLocationId = l.Id,
                                    NamedLocationName = l.Name,
                                    NamedLocationCreatedDate = l.CreatedDateTime,
                                    NamedLocationType = l.LocationTypeValueId.HasValue ? l.LocationTypeValue.Guid.ToString() : string.Empty,
                                    NamedLocationParent = l.ParentLocationId,
                                    NamedLocationSoftRoomThreshold = l.SoftRoomThreshold,
                                    NamedLocationFirmRoomThreshold = l.FirmRoomThreshold
                                } )
                                .ToList();

                            #endregion

                            #region Attributes

                            if ( personDataView != null || groupDataView != null )
                            {
                                LogEvent( "Beginning Attribute Export..." );

                                if ( groupDataView != null )
                                {
                                    _entityTypeIdList.Add( _groupEntityTypeId.HasValue ? _groupEntityTypeId.Value : 0 );
                                }

                                if ( personDataView != null )
                                {
                                    _entityTypeIdList.Add( _personEntityTypeId.HasValue ? _personEntityTypeId.Value : 0 );
                                    _entityTypeIdList.Add( _prayerRequestEntityTypeId.HasValue ? _prayerRequestEntityTypeId.Value : 0 );

                                    if ( groupDataView != null )
                                    {
                                        _entityTypeIdList.Add( _groupMemberEntityTypeId.HasValue ? _groupMemberEntityTypeId.Value : 0 );
                                    }
                                }

                                var attributeService = new AttributeService( rockContext );

                                if ( attributeDataView == null || ddlAttributeIncludeExlude.SelectedValue == "Exclude" )
                                {
                                    var excludedAttributeIdList = new List<int>();

                                    if ( ddlAttributeIncludeExlude.SelectedValue == "Exclude" && attributeDataView != null )
                                    {
                                        var errorMessages = new List<string>();
                                        var paramExpression = attributeService.ParameterExpression;
                                        var whereExpression = attributeDataView.GetExpression( attributeService, paramExpression, out errorMessages );

                                        var excludedAttributeList = attributeService
                                            .Queryable()
                                            .AsNoTracking()
                                            .Where( paramExpression, whereExpression, null )
                                            .ToList();

                                        excludedAttributeIdList = excludedAttributeList
                                            .Select( a => a.Id )
                                            .ToList();
                                    }

                                    var binaryAttributeIds = new List<int>();
                                    foreach ( var attribute in AttributeCache.All( rockContext ) )
                                    {
                                        var attributeFieldType = Type.GetType( attribute.FieldType.Class + ", " + attribute.FieldType.Assembly );
                                        if ( typeof( Rock.Field.Types.BinaryFileFieldType ).IsAssignableFrom( attributeFieldType ) )
                                        {
                                            binaryAttributeIds.Add( attribute.Id );
                                        }
                                    }

                                    excludedAttributeIdList.AddRange( binaryAttributeIds );

                                    attributeList = attributeService
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( a => a.EntityTypeId.HasValue && _entityTypeIdList.Contains( a.EntityTypeId.Value ) && ( !excludedAttributeIdList.Any() || !excludedAttributeIdList.Contains( a.Id ) ) )
                                        .OrderBy( a => a.EntityTypeId )
                                        .ThenBy( a => a.EntityTypeQualifierValue )
                                        .ThenBy( a => a.Order )
                                        .ToList();
                                }
                                else
                                {
                                    var errorMessages = new List<string>();
                                    var paramExpression = attributeService.ParameterExpression;
                                    var whereExpression = attributeDataView.GetExpression( attributeService, paramExpression, out errorMessages );

                                    attributeList = attributeService
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( paramExpression, whereExpression, null )
                                        .ToList();
                                }

                                attributeIdList = attributeList
                                        .Select( a => a.Id )
                                        .ToList();

                                LogEvent( "Attribute Export Completed." );
                            }

                            #endregion

                            #region Attribute Values

                            if ( attributeIdList.Count > 0 )
                            {
                                LogEvent( "Beginning Attribute Value Export..." );

                                var checkInTemplatePurposeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE ).Id;
                                var CheckInTemplateGroupTypes = new GroupTypeService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( t => t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId == checkInTemplatePurposeId )
                                    .ToList();

                                var groupIdsToIgnore = CheckInTemplateGroupTypes
                                    .SelectMany( t => t.ChildGroupTypes )
                                    .SelectMany( c => c.Groups )
                                    .Select( g => g.Id )
                                    .ToList();

                                var communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST ).Id;

                                groupIdsToIgnore.AddRange( new GroupService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( g => g.GroupTypeId == communicationListGroupTypeId )
                                    .Select( g => g.Id )
                                    .ToList() );

                                attributeValueList = new AttributeValueService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( v =>
                                        attributeIdList.Contains( v.AttributeId ) &&
                                        v.EntityId.HasValue &&
                                        !( v.Value == null || v.Value.Trim() == string.Empty ) &&
                                        (
                                            _groupEntityTypeId.HasValue && v.Attribute.EntityTypeId.Value != _groupEntityTypeId.Value ||
                                            (
                                                !groupIdsToIgnore.Any() ||
                                                ( _groupEntityTypeId.HasValue && v.Attribute.EntityTypeId.Value == _groupEntityTypeId.Value && !groupIdsToIgnore.Contains( v.EntityId.Value ) )
                                            )
                                        ) &&
                                        (
                                            ( _groupEntityTypeId != null && _groupEntityTypeId.Value > 0 && v.Attribute.EntityTypeId == _groupEntityTypeId.Value && _groupIdList.Contains( v.EntityId.Value ) ) ||
                                            ( _personEntityTypeId != null && _personEntityTypeId.Value > 0 && v.Attribute.EntityTypeId == _personEntityTypeId.Value && personIdList.Contains( v.EntityId.Value ) ) ||
                                            ( _prayerRequestEntityTypeId != null && _prayerRequestEntityTypeId.Value > 0 && v.Attribute.EntityTypeId == _prayerRequestEntityTypeId.Value && prayerRequestIdList.Contains( v.EntityId.Value ) ) ||
                                            ( _groupMemberEntityTypeId != null && _groupMemberEntityTypeId.Value > 0 && v.Attribute.EntityTypeId == _groupMemberEntityTypeId.Value && groupMemberIdList.Contains( v.EntityId.Value ) )
                                        ) )
                                        .OrderBy( v => v.Attribute.EntityType.Name )
                                        .ThenBy( v => v.AttributeId )
                                        .ThenBy( v => v.EntityId )
                                        .ToList();

                                attributeIdsInAttributeValueList = attributeValueList
                                    .GroupBy( v => v.AttributeId )
                                    .Select( grp => grp.First() )
                                    .Select( v => v.AttributeId )
                                    .ToList();

                                bulldozerEntityAttributeValueList = attributeValueList
                                    .Select( v => new BulldozerEntityAttributeValue
                                    {
                                        AttributeEntityTypeName = v.Attribute.EntityType.Name,
                                        AttributeId = v.AttributeId,
                                        AttributeRockKey = GetBulldozerAttributeRockKey( v.Attribute ),
                                        AttributeValueId = v.Id,
                                        AttributeValueEntityId = v.EntityId.Value,
                                        AttributeValue = GetBulldozerAttributeValue( v )
                                    } )
                                    .ToList();

                                LogEvent( "Attribute Value Export Completed." );
                            }

                            #endregion

                            #region Attribute Bulldozer Entity Attribute List

                            LogEvent( "Finalizing Attribute Export..." );

                            bulldozerEntityAttributeList = attributeList
                                .Where( a => attributeIdsInAttributeValueList.Contains( a.Id ) )
                                .Select( a => new BulldozerEntityAttribute
                                {
                                    AttributeEntityTypeName = a.EntityType.Name,
                                    AttributeId = a.Id,
                                    AttributeRockKey = GetBulldozerAttributeRockKey( a ),
                                    AttributeName = GetBulldozerAttributeName( a ),
                                    AttributeCategoryName = a.Categories.Count > 0 ? a.Categories.FirstOrDefault().Name : string.Empty,
                                    AttributeType = GetBulldozerAttributeType( a ),
                                    AttributeDefinedTypeId = GetBulldozerAttributeDefinedTypeId( a ),
                                    AttributeEntityTypeQualifierName = a.EntityTypeQualifierColumn,
                                    AttributeEntityTypeQualifierValue = a.EntityTypeQualifierValue
                                } )
                                .OrderBy( b => b.AttributeEntityTypeName )
                                .ThenBy( b => b.AttributeCategoryName )
                                .ThenBy( b => b.AttributeRockKey )
                                .ToList();

                            LogEvent( "Attribute Export Finalized..." );

                            #endregion

                            #region Notes

                            #region Person Notes

                            if ( personIdList.Count > 0 && _personEntityTypeId.HasValue )
                            {
                                LogEvent( "Beginning Person Note Export..." );

                                var personNoteTypeIds = new NoteTypeService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( t => t.EntityTypeId == _personEntityTypeId.Value )
                                    .Select( t => t.Id ).ToList();

                                var personNoteList = new NoteService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( n => personNoteTypeIds.Contains( n.NoteTypeId ) && ( n.EntityId != null && personIdList.Contains( n.EntityId.Value ) ) )
                                    .ToList();

                                if ( personNoteList.Count > 0 )
                                {
                                    var personNotes = personNoteList
                                        .Select( n => new BulldozerNote
                                        {
                                            NoteType = n.NoteType.Name,
                                            EntityTypeName = n.NoteType.EntityType.Name,
                                            EntityForeignId = n.EntityId,
                                            NoteCaption = n.Caption,
                                            NoteText = n.Text,
                                            NoteDate = n.CreatedDateTime.HasValue ? n.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                            CreatedById = n.CreatedByPersonId,
                                            IsAlert = n.IsAlert,
                                            IsPrivate = n.IsPrivateNote
                                        } );

                                    bulldozerNoteList.AddRange( personNotes );
                                }

                                LogEvent( "Person Note Export Completed." );
                            }

                            #endregion

                            #region Group Notes

                            if ( _groupIdList.Count > 0 && _groupEntityTypeId.HasValue )
                            {
                                LogEvent( "Beginning Group Note Export..." );

                                var groupNoteTypeIds = new NoteTypeService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( t => t.EntityTypeId == _groupEntityTypeId.Value )
                                    .Select( t => t.Id ).ToList();

                                var groupNoteList = new NoteService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( n => groupNoteTypeIds.Contains( n.NoteTypeId ) && ( n.EntityId != null && _groupIdList.Contains( n.EntityId.Value ) ) )
                                    .ToList();

                                if ( groupNoteList.Count > 0 )
                                {
                                    var groupNotes = groupNoteList
                                          .Select( n => new BulldozerNote
                                          {
                                              NoteType = n.NoteType.Name,
                                              EntityTypeName = n.NoteType.EntityType.Name,
                                              EntityForeignId = n.EntityId,
                                              NoteCaption = n.Caption,
                                              NoteText = n.Text,
                                              NoteDate = n.CreatedDateTime.HasValue ? n.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                              CreatedById = n.CreatedByPersonId,
                                              IsAlert = n.IsAlert,
                                              IsPrivate = n.IsPrivateNote
                                          } );

                                    bulldozerNoteList.AddRange( groupNotes );
                                }

                                LogEvent( "Group Note Export Completed." );
                            }

                            #endregion

                            #region Group Member Notes

                            if ( groupMemberIdList.Count > 0 && _groupMemberEntityTypeId.HasValue )
                            {
                                LogEvent( "Beginning Group Member Note Export..." );

                                var groupMemberNoteTypeIds = new NoteTypeService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( t => t.EntityTypeId == _groupMemberEntityTypeId.Value )
                                    .Select( t => t.Id ).ToList();

                                var groupMemberNoteList = new NoteService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( n => groupMemberNoteTypeIds.Contains( n.NoteTypeId ) && ( n.EntityId != null && groupMemberIdList.Contains( n.EntityId.Value ) ) )
                                    .ToList();

                                if ( groupMemberNoteList.Count > 0 )
                                {
                                    var groupMemberNotes = groupMemberNoteList
                                            .Select( n => new BulldozerNote
                                            {
                                                NoteType = n.NoteType.Name,
                                                EntityTypeName = n.NoteType.EntityType.Name,
                                                EntityForeignId = n.EntityId,
                                                NoteCaption = n.Caption,
                                                NoteText = n.Text,
                                                NoteDate = n.CreatedDateTime.HasValue ? n.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                                CreatedById = n.CreatedByPersonId,
                                                IsAlert = n.IsAlert,
                                                IsPrivate = n.IsPrivateNote
                                            } );

                                    bulldozerNoteList.AddRange( groupMemberNotes );
                                }

                                LogEvent( "Group Member Note Export Completed." );
                            }

                            #endregion

                            #region Prayer Comments

                            if ( prayerRequestList.Count > 0 && _prayerRequestEntityTypeId.HasValue )
                            {
                                LogEvent( "Beginning Prayer Comment Export..." );

                                var prayerRequestNoteTypeIds = new NoteTypeService( rockContext )
                                      .Queryable()
                                      .AsNoTracking()
                                      .Where( t => t.EntityTypeId == _prayerRequestEntityTypeId.Value )
                                      .Select( t => t.Id ).ToList();

                                var prayerRequestNoteList = new NoteService( rockContext )
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( n => prayerRequestNoteTypeIds.Contains( n.NoteTypeId ) && ( n.EntityId != null && prayerRequestIdList.Contains( n.EntityId.Value ) ) )
                                    .ToList();

                                if ( prayerRequestNoteList.Count > 0 )
                                {
                                    var prayerRequestNotes = prayerRequestNoteList
                                               .Select( n => new BulldozerNote
                                               {
                                                   NoteType = n.NoteType.Name,
                                                   EntityTypeName = n.NoteType.EntityType.Name,
                                                   EntityForeignId = n.EntityId,
                                                   NoteCaption = n.Caption,
                                                   NoteText = n.Text,
                                                   NoteDate = n.CreatedDateTime.HasValue ? n.CreatedDateTime.Value.ToShortDateString() : string.Empty,
                                                   CreatedById = n.CreatedByPersonId,
                                                   IsAlert = n.IsAlert,
                                                   IsPrivate = n.IsPrivateNote
                                               } );

                                    bulldozerNoteList.AddRange( prayerRequestNotes );
                                }

                                LogEvent( "Prayer Comment Export Completed." );
                            }

                            bulldozerNoteList.OrderBy( n => n.EntityTypeName )
                                .ThenBy( n => n.EntityForeignId );

                            #endregion

                            #endregion

                            #region Person Photos

                            if ( personList.Count > 0 && cbPersonImages.Checked )
                            {
                                LogEvent( "Beginning Person Photo Export..." );

                                var personPhotoDictionary = personList
                                    .Where( p => p.PhotoId.HasValue )
                                    .ToDictionary( k => k.Id, v => v.Photo );

                                if ( personPhotoDictionary.Count > 0 )
                                {
                                    foreach ( var personPhoto in personPhotoDictionary )
                                    {
                                        var fileNameParts = personPhoto.Value.FileName.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
                                        var numParts = fileNameParts.Length - 1;
                                        var extension = fileNameParts[numParts];
                                        var physFilePath = string.Format( "{0}{1}.{2}", personImageDirectory, personPhoto.Key, extension );
                                        GetPersonImage( personPhoto.Value.ContentStream, physFilePath );
                                    }
                                }

                                LogEvent( "Person Photo Export Completed." );
                            }

                            #endregion
                        }
                    }

                    #region Create Files

                    if ( bulldozerIndividualList.Count > 0 )
                    {
                        LogEvent( "Creating Individual.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}01-Individual.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerIndividualList );
                        }
                    }

                    if ( bulldozerFamilyList.Count > 0 )
                    {
                        LogEvent( "Creating Family.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}02-Family.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerFamilyList );
                        }
                    }

                    if ( bulldozerPreviousLastNameList.Count > 0 )
                    {
                        LogEvent( "Creating PreviousLastName.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}20-PreviousLastName.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerPreviousLastNameList );
                        }
                    }

                    if ( bulldozerPhoneNumberList.Count > 0 )
                    {
                        LogEvent( "Creating PhoneNumber.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}22-PhoneNumber.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerPhoneNumberList
                                .OrderBy( p => p.PhonePersonId )
                                .ThenBy( p => p.PhoneType ) );
                        }
                    }

                    if ( bulldozerConnectionRequestList.Count > 0 )
                    {
                        LogEvent( "Creating ConnectionRequest.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}25-ConnectionRequest.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerConnectionRequestList
                                .OrderBy( r => r.OpportunityForeignKey )
                                .ThenBy( r => r.RequestForeignKey ) );
                        }
                    }

                    if ( bulldozerPrayerRequestList.Count > 0 )
                    {
                        LogEvent( "Creating PrayerRequest.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}19-PrayerRequest.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerPrayerRequestList );
                        }
                    }

                    if ( bulldozerUserLoginList.Count > 0 )
                    {
                        LogEvent( "Creating UserLogin.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}14-UserLogin.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerUserLoginList );
                        }
                    }

                    if ( bulldozerGroupList.Count > 0 )
                    {
                        LogEvent( "Creating Group.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}10-Group.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerGroupList );
                        }
                    }

                    if ( bulldozerGroupTypeList.Count > 0 )
                    {
                        LogEvent( "Creating GroupType.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}09-GroupType.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerGroupTypeList );
                        }
                    }

                    if ( bulldozerGroupMemberList.Count > 0 )
                    {
                        LogEvent( "Creating GroupMember.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}11-GroupMember.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerGroupMemberList );
                        }
                    }

                    if ( bulldozerNamedLocationList.Count > 0 )
                    {
                        LogEvent( "Creating NamedLocation.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}08-NamedLocation.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerNamedLocationList );
                        }
                    }

                    if ( bulldozerAttendanceList.Count > 0 )
                    {
                        LogEvent( "Creating Attendance.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}12-Attendance.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerAttendanceList );
                        }
                    }

                    if ( bulldozerEntityAttributeList.Count > 0 )
                    {
                        LogEvent( "Creating EntityAttribute.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}97-EntityAttribute.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerEntityAttributeList );
                        }
                    }

                    if ( bulldozerEntityAttributeValueList.Count > 0 )
                    {
                        LogEvent( "Creating EntityAttributeValue.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}98-EntityAttributeValue.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerEntityAttributeValueList );
                        }
                    }

                    if ( bulldozerNoteList.Count > 0 )
                    {
                        LogEvent( "Creating Note.csv" );
                        using ( var writer = new StreamWriter( string.Format( "{0}99-Note.csv", csvFileDirectory ) ) )
                        using ( var csv = new CsvWriter( writer ) )
                        {
                            csv.WriteRecords( bulldozerNoteList );
                        }
                    }

                    if ( Directory.GetFiles( csvFileDirectory ).Any() )
                    {
                        LogEvent( "Zipping CSV files..." );

                        var zippedCSVFiles = bulldozerDirectory + "CSVs.zip";

                        if ( File.Exists( zippedCSVFiles ) )
                        {
                            File.Delete( zippedCSVFiles );
                        }

                        ZipFile.CreateFromDirectory( csvFileDirectory, zippedCSVFiles );

                        Directory.Delete( csvFileDirectory, true );

                        LogEvent( "CSV files zipped." );
                    }

                    if ( Directory.GetFiles( personImageDirectory ).Any() )
                    {
                        LogEvent( "Zipping Person Image files..." );

                        var zippedPersonImageFiles = bulldozerDirectory + "PersonImage.zip";

                        if ( File.Exists( zippedPersonImageFiles ) )
                        {
                            File.Delete( zippedPersonImageFiles );
                        }

                        ZipFile.CreateFromDirectory( personImageDirectory, zippedPersonImageFiles );

                        Directory.Delete( personImageDirectory, true );

                        LogEvent( "Person Image files zipped." );
                    }

                    #endregion

                    #region Tidy up

                    Thread.Sleep( 1000 );
                    LogEvent( "Export Finished." );
                    Session["finished"] = true;

                    #endregion
                } );
            }
            else
            {
                nbError.Heading = "Warning!";
                nbError.Text = errorMessage;
                nbError.Visible = true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the parent groups.
        /// </summary>
        /// <param name="groups">The groups.</param>
        private void GetParentGroups( List<Group> groups )
        {
            var parentIsMissing = _groupList.FirstOrDefault( l => l.ParentGroupId.HasValue && !_groupIdList.Contains( l.ParentGroupId.Value ) );

            if ( parentIsMissing != null && parentIsMissing.ParentGroupId.HasValue )
            {
                var index = _groupList.FindIndex( l => l.ParentGroupId.HasValue && l.ParentGroupId == parentIsMissing.ParentGroup.Id );
                _groupList.Insert( index, parentIsMissing.ParentGroup );
                _groupIdList.Add( parentIsMissing.ParentGroup.Id );

                GetParentGroups( _groupList );
            }
        }

        /// <summary>
        /// Determines whether [is named location] [the specified location].
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>
        ///   <c>true</c> if [is named location] [the specified location]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsNamedLocation( Location location )
        {
            return location.Name.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Gets the parent locations.
        /// </summary>
        /// <param name="locations">The locations.</param>
        private void GetParentLocations( List<Location> locations )
        {
            var parentIsMissing = _namedLocationList.FirstOrDefault( l => l.ParentLocationId.HasValue && !_namedLocationIdList.Contains( l.ParentLocationId.Value ) );

            if ( parentIsMissing != null && parentIsMissing.ParentLocationId.HasValue )
            {
                var index = _namedLocationList.FindIndex( l => l.ParentLocationId.HasValue && l.ParentLocationId == parentIsMissing.ParentLocation.Id );
                _namedLocationList.Insert( index, parentIsMissing.ParentLocation );
                _namedLocationIdList.Add( parentIsMissing.ParentLocation.Id );

                GetParentLocations( _namedLocationList );
            }
        }

        /// <summary>
        /// Gets the bulldozer attribute rock key.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        private string GetBulldozerAttributeRockKey( Rock.Model.Attribute attribute )
        {
            var key = attribute.Key;

            var familyAnalyticsCategory = CategoryCache.Get( "266A1EA8-425C-7BB0-4191-C2E234D60086" );
            if ( familyAnalyticsCategory != null && attribute.Key.StartsWith( "core_" ) )
            {
                foreach ( var category in attribute.Categories )
                {
                    if ( category.Id == familyAnalyticsCategory.Id )
                    {
                        key = attribute.Key.Replace( "core_", "kfs_" );
                    }
                }
            }

            return key;
        }

        /// <summary>
        /// Gets the name of the bulldozer attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        private string GetBulldozerAttributeName( Rock.Model.Attribute attribute )
        {
            var name = attribute.Name;

            var familyAnalyticsCategory = CategoryCache.Get( "266A1EA8-425C-7BB0-4191-C2E234D60086" );
            if ( familyAnalyticsCategory != null && attribute.Key.StartsWith( "core_" ) )
            {
                foreach ( var category in attribute.Categories )
                {
                    if ( category.Id == familyAnalyticsCategory.Id )
                    {
                        name = string.Format( "Previous {0}", attribute.Name );
                    }
                }
            }

            return name;
        }

        /// <summary>
        /// Gets the type of the bulldozer attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        private string GetBulldozerAttributeType( Rock.Model.Attribute attribute )
        {
            var type = string.Empty;

            if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DATE ).Id || attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DATE_TIME ).Id )
            {
                type = "D";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.BOOLEAN ).Id )
            {
                type = "B";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DEFINED_VALUE ).Id )
            {
                type = "V";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.ENCRYPTED_TEXT ).Id )
            {
                type = "E";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.VALUE_LIST ).Id && ( attribute.AttributeQualifiers.Count > 0 && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ) != null && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ).Value.AsIntegerOrNull() != null ) )
            {
                type = "VL";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.VALUE_LIST ).Id )
            {
                type = "L";
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.SINGLE_SELECT ).Id )
            {
                type = "S";
            }

            return type;
        }

        /// <summary>
        /// Gets the bulldozer attribute defined type identifier.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        private int? GetBulldozerAttributeDefinedTypeId( Rock.Model.Attribute attribute )
        {
            int? typeId = null;

            if ( attribute.AttributeQualifiers.Count > 0 && attribute.AttributeQualifiers.Count > 0 && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ) != null && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ).Value.AsIntegerOrNull() != null )
            {
                typeId = attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ).Value.AsIntegerOrNull();
            }

            return typeId;
        }

        /// <summary>
        /// Gets the bulldozer attribute value.
        /// </summary>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        private string GetBulldozerAttributeValue( AttributeValue attributeValue )
        {
            var attribute = attributeValue.Attribute;
            var formattedValue = attributeValue.Value;

            if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DATE ).Id || attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DATE_TIME ).Id )
            {
                formattedValue = attributeValue.ValueAsDateTime.HasValue ? attributeValue.ValueAsDateTime.Value.ToShortDateTimeString() : attributeValue.Value;
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.BOOLEAN ).Id )
            {
                formattedValue = attributeValue.ValueAsBoolean.HasValue ? attributeValue.ValueAsBoolean.Value.ToTrueFalse() : attributeValue.Value;
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.DEFINED_VALUE ).Id )
            {
                var definedValue = DefinedValueCache.Get( attributeValue.Value );
                formattedValue = definedValue != null ? definedValue.Value : string.Empty;
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.ENCRYPTED_TEXT ).Id )
            {
                formattedValue = Encryption.DecryptString( attributeValue.Value );
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.VALUE_LIST ).Id && ( attribute.AttributeQualifiers.Count > 0 && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ) != null && attribute.AttributeQualifiers.FirstOrDefault( q => q.Key.Equals( "definedtype", StringComparison.OrdinalIgnoreCase ) ).Value.AsIntegerOrNull() != null ) )
            {
                var values = attributeValue.Value.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToArray() ?? new string[0];
                values = values.Select( s => HttpUtility.UrlDecode( s ) ).ToArray();

                var definedValues = new List<string>();

                if ( values.Any() )
                {
                    foreach ( var value in values )
                    {
                        var valueGuid = value.AsGuidOrNull();

                        if ( valueGuid.HasValue )
                        {
                            var definedValue = DefinedValueCache.Get( valueGuid.Value );
                            if ( definedValue != null )
                            {
                                definedValues.Add( definedValue.Value );
                            }
                        }
                    }
                }

                if ( definedValues.Count > 0 )
                {
                    formattedValue = string.Join( "^", definedValues );
                }
                else
                {
                    formattedValue = string.Join( "^", values );
                }
            }
            else if ( attribute.FieldTypeId == FieldTypeCache.Get( Rock.SystemGuid.FieldType.VALUE_LIST ).Id )
            {
                var values = attributeValue.Value.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToArray() ?? new string[0];
                values = values.Select( s => HttpUtility.UrlDecode( s ) ).ToArray();
                formattedValue = string.Join( "^", values );
            }

            return formattedValue;
        }

        /// <summary>
        /// Gets the person image.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        /// <param name="physFilePath">The physical file path.</param>
        private void GetPersonImage( Stream fileContent, string physFilePath )
        {
            try
            {
                using ( var writeStream = File.OpenWrite( physFilePath ) )
                {
                    if ( fileContent.CanSeek )
                    {
                        fileContent.Seek( 0, SeekOrigin.Begin );
                    }
                    fileContent.CopyTo( writeStream );
                }
            }
            catch
            {
                // if it fails, do nothing. They might have a hosting provider that doesn't allow writing to disk
            }
        }

        /// <summary>
        /// Determines whether [is drive safe] [the specified drive name].
        /// </summary>
        /// <param name="driveName">Name of the drive.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if [is drive safe] [the specified drive name]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsDriveSafe( string driveName, out string errorMessage )
        {
            errorMessage = string.Empty;
            var remainingPercent = 0;
            var safetyPercent = GetAttributeValue( "RemaingDiskPercentage" ).AsInteger();
            if ( safetyPercent == 0 )
            {
                safetyPercent = 10;
            }

            foreach ( DriveInfo drive in DriveInfo.GetDrives() )
            {
                if ( drive.IsReady && drive.Name == driveName )
                {
                    remainingPercent = ( int ) Math.Round( ( ( decimal ) drive.AvailableFreeSpace / drive.TotalSize ) * 100 );
                    break;
                }
            }

            if ( remainingPercent >= safetyPercent )
            {
                return true;
            }

            errorMessage = string.Format( "Only {0}% remaining free space on the server {1} drive. Current block settings require at least {2}%. The process has been cancelled.", remainingPercent, driveName, safetyPercent );
            return false;
        }

        /// <summary>
        /// Deletes the directory.
        /// Solution from: https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
        /// </summary>
        /// <param name="target_dir">The target dir.</param>
        public static void DeleteDirectory( string target_dir )
        {
            string[] files = Directory.GetFiles( target_dir );
            string[] dirs = Directory.GetDirectories( target_dir );

            foreach ( string file in files )
            {
                File.SetAttributes( file, FileAttributes.Normal );
                File.Delete( file );
            }

            foreach ( string dir in dirs )
            {
                DeleteDirectory( dir );
            }

            Directory.Delete( target_dir, false );
        }

        #endregion

        #region Bulldozer Object Classes

        private class BulldozerIndividual
        {
            public int? FamilyId { get; set; }
            public string FamilyName { get; set; }
            public string CreatedDate { get; set; }
            public int PersonId { get; set; }
            public string Prefix { get; set; }
            public string FirstName { get; set; }
            public string NickName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string Suffix { get; set; }
            public string FamilyRole { get; set; }
            public string MaritalStatus { get; set; }
            public string ConnectionStatus { get; set; }
            public string RecordStatus { get; set; }
            public bool? IsDeceased { get; set; }
            public string HomePhone { get; set; }
            public string MobilePhone { get; set; }
            public string WorkPhone { get; set; }
            public bool? AllowSMS { get; set; }
            public string Email { get; set; }
            public bool? IsEmailActive { get; set; }
            public bool? AllowBulkEmail { get; set; }
            public string Gender { get; set; }
            public string DateOfBirth { get; set; }
            public string School { get; set; }
            public string GraduationDate { get; set; }
            public string Anniversary { get; set; }
            public string GeneralNote { get; set; }
            public string MedicalNote { get; set; }
            public string SecurityNote { get; set; }
        }

        private class BulldozerFamily
        {
            public int? FamilyId { get; set; }
            public string FamilyName { get; set; }
            public string CreatedDate { get; set; }
            public string Campus { get; set; }
            public string Address { get; set; }
            public string Address2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Country { get; set; }
            public string SecondaryAddress { get; set; }
            public string SecondaryAddress2 { get; set; }
            public string SecondaryCity { get; set; }
            public string SecondaryState { get; set; }
            public string SecondaryZip { get; set; }
            public string SecondaryCountry { get; set; }
        }

        private class BulldozerNamedLocation
        {
            public int NamedLocationId { get; set; }
            public string NamedLocationName { get; set; }
            public DateTime? NamedLocationCreatedDate { get; set; }
            public string NamedLocationType { get; set; }
            public int? NamedLocationParent { get; set; }
            public int? NamedLocationSoftRoomThreshold { get; set; }
            public int? NamedLocationFirmRoomThreshold { get; set; }
        }

        private class BulldozerGroupType
        {
            public int GroupTypeId { get; set; }
            public string GroupTypeName { get; set; }
            public DateTime? GroupTypeCreatedDate { get; set; }
            public string GroupTypePurpose { get; set; }
            public string GroupTypeInheritedGroupType { get; set; }
            public bool? GroupTypeTakesAttendance { get; set; }
            public bool? GroupTypeWeekendService { get; set; }
            public bool? GroupTypeShowInGroupList { get; set; }
            public bool? GroupTypeShowInNav { get; set; }
            public int? GroupTypeParentId { get; set; }
            public bool? GroupTypeSelfReference { get; set; }
            public bool? GroupTypeWeeklySchedule { get; set; }
        }

        private class BulldozerGroup
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public DateTime? GroupCreatedDate { get; set; }
            public string GroupType { get; set; }
            public int? GroupParentGroupId { get; set; }
            public bool GroupActive { get; set; }
            public int GroupOrder { get; set; }
            public string GroupCampus { get; set; }
            public string GroupAddress { get; set; }
            public string GroupAddress2 { get; set; }
            public string GroupCity { get; set; }
            public string GroupState { get; set; }
            public string GroupZip { get; set; }
            public string GroupCountry { get; set; }
            public string GroupSecondaryAddress { get; set; }
            public string GroupSecondaryAddress2 { get; set; }
            public string GroupSecondaryCity { get; set; }
            public string GroupSecondaryState { get; set; }
            public string GroupSecondaryZip { get; set; }
            public string GroupSecondaryCountry { get; set; }
            public string GroupNamedLocation { get; set; }
            public string GroupDayOfWeek { get; set; }
            public string GroupTime { get; set; }
            public string GroupDescription { get; set; }
            public int? GroupCapacity { get; set; }
        }

        private class BulldozerGroupAddress
        {
            public Rock.Model.Group Group { get; set; }
            public Location Location1 { get; set; }
            public Location Location2 { get; set; }
            public string NamedLocation { get; set; }
        }

        private class BulldozerGroupMember
        {
            public int GroupMemberId { get; set; }
            public int GroupMemberGroupId { get; set; }
            public int GroupMemberPersonId { get; set; }
            public DateTime? GroupMemberCreatedDate { get; set; }
            public string GroupMemberRole { get; set; }
            public bool GroupMemberActive { get; set; }
        }

        private class BulldozerAttendance
        {
            public int AttendanceId { get; set; }
            public int? AttendanceGroupId { get; set; }
            public int AttendancePersonId { get; set; }
            public DateTime? AttendanceCreatedDate { get; set; }
            public DateTime? AttendanceDate { get; set; }
            public bool? AttendanceAttended { get; set; }
            public int? AttendanceLocationId { get; set; }
        }

        private class BulldozerUserLogin
        {
            public int UserLoginId { get; set; }
            public int UserLoginPersonId { get; set; }
            public string UserLoginUserName { get; set; }
            public string UserLoginPassword { get; set; }
            public DateTime? UserLoginDateCreated { get; set; }
            public string UserLoginAuthenticationType { get; set; }
            public bool? UserLoginIsConfirmed { get; set; }
        }

        private class BulldozerNote
        {
            public string NoteType { get; set; }
            public string EntityTypeName { get; set; }
            public int? EntityForeignId { get; set; }
            public string NoteCaption { get; set; }
            public string NoteText { get; set; }
            public string NoteDate { get; set; }
            public int? CreatedById { get; set; }
            public bool? IsAlert { get; set; }
            public bool IsPrivate { get; set; }
        }

        private class BulldozerPrayerRequest
        {
            public string PrayerRequestCategory { get; set; }
            public string PrayerRequestText { get; set; }
            public DateTime PrayerRequestDate { get; set; }
            public int PrayerRequestId { get; set; }
            public string PrayerRequestFirstName { get; set; }
            public string PrayerRequestLastName { get; set; }
            public string PrayerRequestEmail { get; set; }
            public DateTime? PrayerRequestExpireDate { get; set; }
            public bool? PrayerRequestAllowComments { get; set; }
            public bool? PrayerRequestIsPublic { get; set; }
            public bool? PrayerRequestIsApproved { get; set; }
            public DateTime? PrayerRequestApprovedDate { get; set; }
            public int? PrayerRequestApprovedById { get; set; }
            public int? PrayerRequestCreatedById { get; set; }
            public int? PrayerRequestRequestedById { get; set; }
            public string PrayerRequestAnswerText { get; set; }
            public string PrayerRequestCampus { get; set; }
        }

        private class BulldozerPreviousLastName
        {
            public int PreviousLastNamePersonId { get; set; }
            public string PreviousLastName { get; set; }
            public int PreviousLastNameId { get; set; }
        }

        private class BulldozerPhoneNumber
        {
            public int PhonePersonId { get; set; }
            public string PhoneType { get; set; }
            public string Phone { get; set; }
            public bool PhoneIsMessagingEnabled { get; set; }
            public bool PhoneIsUnlisted { get; set; }
            public int PhoneId { get; set; }
        }

        private class BulldozerEntityAttribute
        {
            public string AttributeEntityTypeName { get; set; }
            public int AttributeId { get; set; }
            public string AttributeRockKey { get; set; }
            public string AttributeName { get; set; }
            public string AttributeCategoryName { get; set; }
            public string AttributeType { get; set; }
            public int? AttributeDefinedTypeId { get; set; }
            public string AttributeEntityTypeQualifierName { get; set; }
            public string AttributeEntityTypeQualifierValue { get; set; }
        }

        private class BulldozerEntityAttributeValue
        {
            public string AttributeEntityTypeName { get; set; }
            public int AttributeId { get; set; }
            public string AttributeRockKey { get; set; }
            public int AttributeValueId { get; set; }
            public int AttributeValueEntityId { get; set; }
            public string AttributeValue { get; set; }
        }

        private class BulldozerConnectionRequest
        {
            public int? OpportunityForeignKey { get; set; }
            public string OpportunityName { get; set; }
            public string ConnectionType { get; set; }
            public string OpportunityDescription { get; set; }
            public bool OpportunityActive { get; set; }
            public DateTime? OpportunityCreated { get; set; }
            public DateTime? OpportunityModified { get; set; }
            public int RequestForeignKey { get; set; }
            public int RequestPersonId { get; set; }
            public int? RequestConnectorId { get; set; }
            public DateTime? RequestCreated { get; set; }
            public DateTime? RequestModified { get; set; }
            public string RequestStatus { get; set; }
            public int RequestState { get; set; }
            public string RequestComments { get; set; }
            public DateTime? RequestFollowUp { get; set; }
            public string ActivityType { get; set; }
            public string ActivityNote { get; set; }
            public DateTime? ActivityDate { get; set; }
            public int? ActivityConnectorId { get; set; }
        }

        #endregion
    }
}
