<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceSummary.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Groups.GroupAttendanceSummary" %>

<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-check-square-o"></i>
                    <asp:Literal ID="lHeading" runat="server" Text="Group Attendance" />
                </h1>
            </div>

            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:NotificationBox ID="nbAttendeesError" runat="server" NotificationBoxType="Danger" Dismissable="true" Visible="false" />
                    <Rock:Grid ID="gAttendeesAttendance" runat="server" AllowSorting="true" RowItemText="Attendee" OnRowDataBound="gAttendeesAttendance_RowDataBound" ExportSource="ColumnOutput" ExportFilename="AttendanceSummary">
                        <Columns>
                            <Rock:SelectField />
                            <asp:HyperLinkField DataNavigateUrlFields="PersonId" DataTextField="Person" Visible="false" HeaderText="Name" SortExpression="Person.LastName, Person.NickName" />
                            <Rock:RockBoundField DataField="Person" HeaderText="Name" ExcelExportBehavior="NeverInclude" SortExpression="Person.LastName, Person.NickName" />
                            <Rock:RockBoundField DataField="Person" HeaderText="Person" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                            <Rock:DateField DataField="LastVisit.StartDateTime" HeaderText="Last Visit" SortExpression="LastVisit.StartDateTime" />
                            <Rock:RockLiteralField HeaderText="Count" ID="lAttendanceCount" SortExpression="AttendanceSummary.Count" />
                            <Rock:RockLiteralField HeaderText="%" ID="lAttendancePercent" SortExpression="AttendanceSummary.Count" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
