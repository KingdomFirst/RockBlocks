<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesConfiguration.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Reporting.ReportingServicesConfiguration" %>
<asp:UpdatePanel ID="upRSConfig" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlRSConfig" runat="server" Visible="true">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-area-chart"></i>&nbsp;Reporting Services Configuration</h1>
                </div>
                <Rock:NotificationBox ID="nbReportingServices" runat="server" Visible="false" />
                <div class="panel-body">
                    <fieldset>
                        <h4>Server Configuration</h4>
                        <Rock:RockTextBox ID="tbReportingServicesURL" runat="server" Label="Web Service URL" Required="true" RequiredErrorMessage="Reporting Server URL required" ToolTip="URL to the Reporting Services Report Server endpoint." ValidationGroup="RSConfig" />
                        <Rock:RockTextBox ID="tbReportRootFolder" runat="server" Label="Root Folder Path" Required="true" RequiredErrorMessage="Root Folder is required" ToolTip="Root Folder for Reporting Services Reports." ValidationGroup="RSConfig" />
                        <h4>Credentials</h4>
                        <asp:Panel ID="pnlAdminUser" runat="server" Visible="false">
                            <Rock:RockTextBox ID="tbAdminUserName" runat="server" Label="Content Manager User" ToolTip="Content Manager (Administration) User" Required="false" ValidationGroup="RSConfig" RequiredErrorMessage="Content Manager User is required." />
                            <Rock:RockTextBox ID="tbAdminPassword" runat="server" Label="Content Manager Password" TextMode="Password" RequiredErrorMessage="Content Manager Password is required" Required="false" ValidationGroup="RSConfig" />
                            <asp:HiddenField ID="hfAdminPasswordSet" runat="server" />
                        </asp:Panel>
                        <Rock:RockTextBox ID="tbUserName" runat="server" Label="Report Server User" Required="true" ValidationGroup="RSConfig" RequiredErrorMessage="Report Server User is required." />
                        <Rock:RockTextBox ID="tbPassword" runat="server" Label="Report Server Password" TextMode="Password" Required="true" ValidationGroup="RSConfig" RequiredErrorMessage="Report Server Password is required." />
                        <asp:HiddenField ID="hfPasswordSet" runat="server" />
                    </fieldset>
                </div>
            </div>
            <div class="actions">
                <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" CausesValidation="true" ValidationGroup="RSConfig" />
                <asp:LinkButton ID="btnVerify" runat="server" AccessKey="v" Text="Verify" Visible="false" CssClass="btn btn-default" OnClick="btnVerify_Click" CausesValidation="false" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
