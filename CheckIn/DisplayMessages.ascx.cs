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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.CheckIn
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Display Messages" )]
    [Category( "KFS > Check-in" )]
    [Description( "Displays the check-in state messages." )]

    #region Block Attributes

    [LavaField( "Detail Message",
        Key = AttributeKey.DetailMessage,
        Description = "By default the messages will come straight from the check-in state 'Messages' array. You can customize the message even further using lava, an array of 'Messages' is available to use what was provided from the current check-in state.",
        IsRequired = false,
        DefaultValue = "" )]

    [CustomDropdownListField( "Type of Alert Box",
        Key = AttributeKey.TypeOfAlert,
        Description = "Select the type of alert box to show the messages in. ('Log only' found in ServiceLog table)",
        ListSource = "-3^None,-2^Log Only,-1^Append to Existing,0^Alert,1^Information,2^Warning,3^No Heading",
        DefaultValue = "-3" )]

    #endregion Block Attributes

    public partial class DisplayMessages : CheckInBlock
    {
        /* 2021-05/07 ETD
         * Use new here because the parent CheckInBlock also has inherited class AttributeKey.
         */
        private new static class AttributeKey
        {
            public const string DetailMessage = "DetailMessage";
            public const string TypeOfAlert = "TypeOfAlert";
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
            }
            else
            {
                if ( Page.IsPostBack )
                {
                    try
                    {
                        string detailMsg = GetAttributeValue( AttributeKey.DetailMessage );
                        int? typeOfAlert = GetAttributeValue( AttributeKey.TypeOfAlert ).AsIntegerOrNull();

                        var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, null, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
                        mergeFields.Add( "CurrentFamily", CurrentCheckInState.CheckIn.CurrentFamily );
                        mergeFields.Add( "Families", CurrentCheckInState.CheckIn.Families );
                        mergeFields.Add( "CheckinCurrentPerson", CurrentCheckInState.CheckIn.CurrentPerson );
                        mergeFields.Add( "Kiosk", CurrentCheckInState.Kiosk );
                        mergeFields.Add( "RegistrationModeEnabled", CurrentCheckInState.Kiosk.RegistrationModeEnabled );
                        mergeFields.Add( "Messages", CurrentCheckInState.Messages );

                        if ( CurrentCheckInState.Messages.Count > 0 )
                        {
                            var messageStr = "";
                            foreach ( var message in CurrentCheckInState.Messages )
                            {
                                if ( message.MessageText.IsNotNullOrWhiteSpace() && !messageStr.Contains( message.MessageText ) )
                                {
                                    messageStr += message.MessageText;
                                }
                            }

                            if ( messageStr.IsNotNullOrWhiteSpace() )
                            {
                                if ( detailMsg.IsNotNullOrWhiteSpace() )
                                {
                                    messageStr = detailMsg.ResolveMergeFields( mergeFields );
                                }
                                messageStr = messageStr.RemoveCrLf();
                                if ( typeOfAlert.HasValue && typeOfAlert.Value >= 0 )
                                {
                                    maWarning.Show( messageStr, ( Rock.Web.UI.Controls.ModalAlertType ) typeOfAlert.Value );
                                }
                                else if ( typeOfAlert.HasValue )
                                {
                                    switch ( typeOfAlert.Value )
                                    {
                                        case -2:
                                            LogEvent( null, "LogMessages", messageStr, string.Format( "Message Count: {0}", CurrentCheckInState.Messages.Count.ToString() ) );
                                            break;
                                        case -1:
                                            var script = $"$('.bootbox-body').append('{messageStr}');";
                                            ScriptManager.RegisterStartupScript( this, this.GetType(), "DisplayMessages", script, true );
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            CurrentCheckInState.Messages.Clear();
                        }

                    }
                    catch ( Exception ex )
                    {
                        LogException( ex );
                    }
                }
            }
        }

        private static ServiceLog LogEvent( RockContext rockContext, string type, string input, string result )
        {
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }
            var rockLogger = new ServiceLogService( rockContext );
            ServiceLog serviceLog = new ServiceLog
            {
                Name = "KFS Check-in - Display Messages",
                Type = type,
                LogDateTime = RockDateTime.Now,
                Input = input,
                Result = result,
                Success = true
            };
            rockLogger.Add( serviceLog );
            rockContext.SaveChanges();
            return serviceLog;
        }


    }
}