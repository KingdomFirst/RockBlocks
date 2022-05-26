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
using System.ComponentModel;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.CheckIn
{
    /// <summary>
    /// Block that adds a content channel item for person paging.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Pager Entry Setup" )]
    [Category( "KFS > Check-in" )]
    [Description( "Block that sets up Pager Entry page and block settings." )]
    public partial class PagerEntrySetup : RockBlock
    {
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
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        #endregion Base Control Methods

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        protected void lbInstallPagerEntry_Click( object sender, EventArgs e )
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var blockService = new BlockService( rockContext );
                    var attributeService = new AttributeService( rockContext );
                    var attributeValueService = new AttributeValueService( rockContext );

                    var personSelect = blockService.Get( "0F82C7EB-3E71-496F-B5F4-83F32AD5EBB5".AsGuid() );
                    var locationSelect = blockService.Get( "9D876B07-DF35-4355-85B0-638F65C367C4".AsGuid() );
                    var timeSelect = blockService.Get( "472E00D1-BD9B-407A-92C6-05132039DB65".AsGuid() );

                    var personSelectNextPage = attributeService.Get( "4302646B-F6CD-492D-8850-96B9CA1CEA59".AsGuid() );
                    var locationSelectNextPage = attributeService.Get( "8EB048AF-3A8B-4D55-8045-861B9AE7DF4C".AsGuid() );
                    var timeSelectNextPage = attributeService.Get( "840898DB-A9AB-45C9-9894-0A1E816EFC4C".AsGuid() );

                    var personSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( personSelectNextPage.Id, personSelect.Id );
                    var locationSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( locationSelectNextPage.Id, locationSelect.Id );
                    var timeSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( timeSelectNextPage.Id, timeSelect.Id );

                    personSelectNextValue.Value = "50a1708f-d751-40c5-be99-492c4e81aed0";
                    locationSelectNextValue.Value = "50a1708f-d751-40c5-be99-492c4e81aed0";
                    timeSelectNextValue.Value = "50a1708f-d751-40c5-be99-492c4e81aed0";

                    rockContext.SaveChanges();
                }

                nbWarning.NotificationBoxType = NotificationBoxType.Success;
                nbWarning.Text = "Pager Entry Installed!";
            }
            catch ( Exception ex )
            {
                nbWarning.Text = ex.Message;
            }
        }

        #endregion Events

        protected void lbRemovePagerEntry_Click( object sender, EventArgs e )
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var blockService = new BlockService( rockContext );
                    var attributeService = new AttributeService( rockContext );
                    var attributeValueService = new AttributeValueService( rockContext );

                    var personSelect = blockService.Get( "0F82C7EB-3E71-496F-B5F4-83F32AD5EBB5".AsGuid() );
                    var locationSelect = blockService.Get( "9D876B07-DF35-4355-85B0-638F65C367C4".AsGuid() );
                    var timeSelect = blockService.Get( "472E00D1-BD9B-407A-92C6-05132039DB65".AsGuid() );

                    var personSelectNextPage = attributeService.Get( "4302646B-F6CD-492D-8850-96B9CA1CEA59".AsGuid() );
                    var locationSelectNextPage = attributeService.Get( "8EB048AF-3A8B-4D55-8045-861B9AE7DF4C".AsGuid() );
                    var timeSelectNextPage = attributeService.Get( "840898DB-A9AB-45C9-9894-0A1E816EFC4C".AsGuid() );

                    var personSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( personSelectNextPage.Id, personSelect.Id );
                    var locationSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( locationSelectNextPage.Id, locationSelect.Id );
                    var timeSelectNextValue = attributeValueService.GetByAttributeIdAndEntityId( timeSelectNextPage.Id, timeSelect.Id );

                    personSelectNextValue.Value = "4af7a0e1-e991-4ae5-a2b5-c440f67a2e6a";
                    locationSelectNextValue.Value = "4af7a0e1-e991-4ae5-a2b5-c440f67a2e6a";
                    timeSelectNextValue.Value = "E08230B8-35A4-40D6-A0BB-521418314DA9";

                    rockContext.SaveChanges();
                }

                nbWarning.NotificationBoxType = NotificationBoxType.Info;
                nbWarning.Text = "Pager Entry Removed!";
            }
            catch ( Exception ex )
            {
                nbWarning.Text = ex.Message;
            }
        }
    }
}