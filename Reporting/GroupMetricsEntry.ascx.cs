using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Data.Entity;

namespace RockWeb.Plugins.com_kfs.Reporting
{
    /// <summary>
    /// Block for easily adding/editing metric values for any metric that has partitions of group.
    /// </summary>
    [DisplayName( "Group Toolbox Metrics Entry" )]
    [Category( "KFS > Reporting" )]
    [Description( "Block for easily adding/editing metric values for any metric that has partitions of group." )]

    [MetricCategoriesField( "Metric Categories", "Select the metric categories to display (note: only metrics in those categories with a group partition will displayed).", true, "", "", 3 )]
    [BooleanField( "Show Note Field", "Allow the user to input note along with metric entry.", false )]
    [BooleanField( "Admin Mode", "Enable the block to be used for any group with a group selector.", false )]
    public partial class GroupMetricsEntry : Rock.Web.UI.RockBlock
    {
        #region Fields

        private int _groupId = 0;
        private int? _selectedGroupId { get; set; }
        private DateTime? _selectedDate { get; set; }
        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            _selectedGroupId = ViewState["SelectedGroupId"] as int?;
            _selectedDate = ViewState["SelectedDate"] as DateTime?;
        }

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

            nbMetricsSaved.Visible = false;

            if ( !Page.IsPostBack )
            {
                if ( !string.IsNullOrWhiteSpace( PageParameter( "GroupId" ) ) )
                {
                    _groupId = Convert.ToInt32( PageParameter( "GroupId" ) );
                    _selectedGroupId = _groupId;
                    gpSelectGroup.Visible = false;
                }
                else if ( GetAttributeValue( "AdminMode" ).AsBoolean() )
                {
                    _selectedGroupId = GetBlockUserPreference( "GroupId" ).AsIntegerOrNull();
                }
                else
                {
                    pnlMetrics.Visible = false;
                    pnlError.Visible = true;
                }
                _selectedDate = RockDateTime.Today;

                gpSelectGroup.SetValue( _selectedGroupId );
                dpMetricValueDateTime.SelectedDate = _selectedDate;
                tbNote.Visible = GetAttributeValue( "ShowNoteField" ).AsBoolean();

                BindMetrics();
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["SelectedGroupId"] = _selectedGroupId;
            ViewState["SelectedDate"] = _selectedDate;
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
            BindMetrics();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptrMetric control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptrMetric_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item )
            {
                var nbMetricValue = e.Item.FindControl( "nbMetricValue" ) as NumberBox;
                if ( nbMetricValue != null )
                {
                    nbMetricValue.ValidationGroup = BlockValidationGroup;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            int groupEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Group ) ).Id;
            int campusEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Campus ) ).Id;

            int? groupId = gpSelectGroup.SelectedValueAsInt();
            DateTime? dateVal = dpMetricValueDateTime.SelectedDate;

            if ( groupId.HasValue && dateVal.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var metricService = new MetricService( rockContext );
                    var metricValueService = new MetricValueService( rockContext );

                    var group = new GroupService( rockContext ).Queryable().FirstOrDefault( g => g.Id == groupId );
                    int? campusId = group.CampusId;

                    foreach ( RepeaterItem item in rptrMetric.Items )
                    {
                        var hfMetricIId = item.FindControl( "hfMetricId" ) as HiddenField;
                        var nbMetricValue = item.FindControl( "nbMetricValue" ) as NumberBox;

                        if ( hfMetricIId != null && nbMetricValue != null )
                        {
                            int metricId = hfMetricIId.ValueAsInt();
                            var metric = new MetricService( rockContext ).Get( metricId );

                            if ( metric != null )
                            {
                                int groupPartitionId = metric.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == groupEntityTypeId ).Select( p => p.Id ).FirstOrDefault();
                                int campusPartitionId = metric.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ).Select( p => p.Id ).FirstOrDefault();

                                var metricValue = metricValueService
                                    .Queryable()
                                    .Where( v =>
                                        v.MetricId == metric.Id &&
                                        v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == dateVal.Value &&
                                            (
                                                (
                                                    v.MetricValuePartitions.Count == 2 &&
                                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == campusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusId.Value ) &&
                                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == groupPartitionId && p.EntityId.HasValue && p.EntityId.Value == groupId.Value )
                                                ) ||
                                               (
                                                    v.MetricValuePartitions.Count == 1 &&
                                                    (
                                                        v.MetricValuePartitions.Any( p => p.MetricPartitionId == groupPartitionId && p.EntityId.HasValue && p.EntityId.Value == groupId.Value )
                                                    )
                                                )
                                            )
                                        )
                                    .FirstOrDefault();

                                if ( metricValue == null )
                                {
                                    metricValue = new MetricValue();
                                    metricValue.MetricValueType = MetricValueType.Measure;
                                    metricValue.MetricId = metric.Id;
                                    metricValue.MetricValueDateTime = dateVal.Value;
                                    metricValueService.Add( metricValue );

                                    if ( groupPartitionId > 0 )
                                    {
                                        var groupValuePartition = new MetricValuePartition();
                                        groupValuePartition.MetricPartitionId = groupPartitionId;
                                        groupValuePartition.EntityId = groupId.Value;
                                        metricValue.MetricValuePartitions.Add( groupValuePartition );
                                    }
                                    if ( campusPartitionId > 0 && campusId.HasValue )
                                    {
                                        var campusValuePartition = new MetricValuePartition();
                                        campusValuePartition.MetricPartitionId = campusPartitionId;
                                        campusValuePartition.EntityId = campusId.Value;
                                        metricValue.MetricValuePartitions.Add( campusValuePartition );
                                    }

                                }

                                metricValue.YValue = nbMetricValue.Text.AsDecimalOrNull();
                                metricValue.Note = tbNote.Text;
                            }
                        }
                    }

                    rockContext.SaveChanges();
                }

                nbMetricsSaved.Text = string.Format( "The metrics for '{0}' on {1} have been saved.", gpSelectGroup.ItemName, dpMetricValueDateTime.SelectedDate.ToString() );
                nbMetricsSaved.Visible = true;

                BindMetrics();

            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the filter controls.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bddl_SelectionChanged( object sender, EventArgs e )
        {
            BindMetrics();
        }

        #endregion

        #region Methods
        /// <summary>
        /// Binds the metrics.
        /// </summary>
        private void BindMetrics()
        {
            var groupMetricValues = new List<GroupMetric>();

            int groupEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Group ) ).Id;
            int campusEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Campus ) ).Id;

            int? groupId = gpSelectGroup.SelectedValueAsInt();
            DateTime? weekend = dpMetricValueDateTime.SelectedDate;

            var notes = new List<string>();

            if ( groupId.HasValue && weekend.HasValue )
            {

                SetBlockUserPreference( "GroupId", groupId.HasValue ? groupId.Value.ToString() : "" );

                var metricCategories = MetricCategoriesFieldAttribute.GetValueAsGuidPairs( GetAttributeValue( "MetricCategories" ) );
                var metricGuids = metricCategories.Select( a => a.MetricGuid ).ToList();
                using ( var rockContext = new RockContext() )
                {
                    var group = new GroupService( rockContext ).Queryable().FirstOrDefault( g => g.Id == groupId );
                    int? campusId = group.CampusId;
                    var metricValueService = new MetricValueService( rockContext );
                    foreach ( var metric in new MetricService( rockContext )
                        .GetByGuids( metricGuids )
                        .OrderBy( m => m.Title )
                        .Select( m => new
                        {
                            m.Id,
                            m.Title,
                            GroupPartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == groupEntityTypeId ).Select( p => p.Id ).FirstOrDefault(),
                            CampusPartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ).Select( p => p.Id ).FirstOrDefault()
                        } ) )
                    {

                        var groupMetric = new GroupMetric( metric.Id, metric.Title );

                        if ( groupId.HasValue && weekend.HasValue )
                        {
                            var metricValue = metricValueService
                                .Queryable().AsNoTracking()
                                .Where( v =>
                                    v.MetricId == metric.Id &&
                                    v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == weekend.Value &&
                                        (
                                            (
                                                v.MetricValuePartitions.Count == 2 &&
                                                v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.CampusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusId.Value ) &&
                                                v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.GroupPartitionId && p.EntityId.HasValue && p.EntityId.Value == groupId.Value )
                                            ) ||
                                            (
                                                v.MetricValuePartitions.Count == 1 &&
                                                    (
                                                        v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.GroupPartitionId && p.EntityId.HasValue && p.EntityId.Value == groupId.Value )
                                                    )
                                            )
                                        )
                                    )
                                .FirstOrDefault();

                            if ( metricValue != null )
                            {
                                groupMetric.Value = metricValue.YValue;

                                if ( !string.IsNullOrWhiteSpace( metricValue.Note ) &&
                                    !notes.Contains( metricValue.Note ) )
                                {
                                    notes.Add( metricValue.Note );
                                }

                            }
                        }

                        groupMetricValues.Add( groupMetric );
                    }
                }
            }

            rptrMetric.DataSource = groupMetricValues;
            rptrMetric.DataBind();

            tbNote.Text = notes.AsDelimited( Environment.NewLine + Environment.NewLine );
        }

        #endregion

    }

    public class GroupMetric
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Value { get; set; }

        public GroupMetric( int id, string name )
        {
            Id = id;
            Name = name;
        }
    }
}