<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceReport.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.AttendanceReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="col-sm-3">
            <Rock:DatePicker ID="dpDate" runat="server" Label="Occurrence Date" AllowFutureDateSelection="false" AutoPostBack="true" OnTextChanged="dpDate_SelectDate" />

            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" IncludeInactive="false" AutoPostBack="true" OnSelectedIndexChanged="cpCampus_SelectedIndexChanged" />

            <Rock:RockDropDownList ID="ddlSchedule" runat="server" Label="Schedule" AutoPostBack="true" OnSelectedIndexChanged="ddlSchedule_SelectedIndexChanged" />

            <Rock:GroupTypePicker ID="ddlCheckInConfiguration" runat="server" Label="Check-In Type" AutoPostBack="true" OnSelectedIndexChanged="ddlCheckInConfiguration_SelectedIndexChanged" />

            <Rock:GroupTypePicker ID="ddlCheckInArea" runat="server" Label="Check-In Area" AutoPostBack="true" OnSelectedIndexChanged="ddlCheckInArea_SelectedIndexChanged" />

            <Rock:RockCheckBoxList ID="cblGroups" runat="server" Label="Check-In Groups" EmptyListMessage="No Groups Found" />

            <Rock:BootstrapButton ID="btnRunReport" runat="server" Text="Run Report" CssClass="btn btn-primary" OnClick="btnRunReport_Click" />
        </div>
        <div class="col-sm-9">
            <Rock:Grid runat="server" ID="gAttendees" EmptyDataText="No Attendees Found" AllowSorting="true">
                <Columns>
                    <Rock:CampusField DataField="Campus" HeaderText="Campus" Visible="false" />
                    <Rock:RockBoundField DataField="ScheduleName" HeaderText="Schedule" />
                    <Rock:RockBoundField DataField="GroupName" HeaderText="Group" />
                    <Rock:RockBoundField DataField="LocationName" HeaderText="Location" />
                    <Rock:PersonField DataField="Person" HeaderText="Person" />
                    <Rock:RockBoundField DataField="AttendanceCodeString" HeaderText="Attendance Code" />
                    <Rock:RockTemplateField HeaderText="Multiple Check-Ins">
                        <ItemTemplate><%#(((List<AttendanceReportSubItem>)Eval("AdditionalReportItems")).Any()) ? "Yes" : ""%></ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
