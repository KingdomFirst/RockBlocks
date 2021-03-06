﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceReport.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.AttendanceReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="col-sm-3">
            <Rock:DatePicker ID="dpDate" runat="server" Label="Occurrence Date" AllowFutureDateSelection="false" AutoPostBack="true" OnTextChanged="dpDate_SelectDate" />
            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" IncludeInactive="false" AutoPostBack="true" OnSelectedIndexChanged="cpCampus_SelectedIndexChanged" />
            <Rock:RockDropDownList ID="ddlSchedule" runat="server" Label="Schedule" AutoPostBack="true" OnSelectedIndexChanged="ddlSchedule_SelectedIndexChanged" />
            <Rock:GroupTypePicker ID="ddlCheckInConfiguration" runat="server" Label="Check-In Type" AutoPostBack="true" OnSelectedIndexChanged="ddlCheckInConfiguration_SelectedIndexChanged" />
            <asp:Repeater ID="rptGroups" runat="server" OnItemDataBound="BindItem">
                <ItemTemplate>
                    <label><%# DataBinder.Eval(Container.DataItem, "Name") %></label>
                    <Rock:RockCheckBoxList ID="cblGroups" runat="server" EmptyListMessage="No Groups with Attendance" />
                </ItemTemplate>
            </asp:Repeater>
            <br />
            <Rock:BootstrapButton ID="btnRunReport" runat="server" Text="Run Report" CssClass="btn btn-primary" OnClick="btnRunReport_Click" />
        </div>
        <div class="col-sm-9">
            <Rock:Grid runat="server" ID="gAttendees" EmptyDataText="No Attendees Found" DataKeyNames="AttendanceId">
                <Columns>
                    <Rock:SelectField />
                    <Rock:RockBoundField DataField="AttendanceId" HeaderText="Attendance Id" Visible="false" />
                    <Rock:RockBoundField DataField="CampusName" HeaderText="Campus" />
                    <Rock:RockBoundField DataField="ScheduleName" HeaderText="Schedule" />
                    <Rock:RockBoundField DataField="GroupName" HeaderText="Group" />
                    <Rock:RockBoundField DataField="LocationName" HeaderText="Location" />
                    <Rock:RockBoundField DataField="PersonName" HeaderText="Person" />
                    <Rock:RockBoundField DataField="AttendanceCode" HeaderText="Attendance Code" />
                    <Rock:RockTemplateField HeaderText="Multiple Check-Ins">
                        <ItemTemplate><%#(((List<AttendanceReportSubItem>)Eval("AdditionalReportItems")).Any()) ? "Yes" : ""%></ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
