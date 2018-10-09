<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchToJournal.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.ShelbyFinancials.BatchToJournal" %>

<asp:UpdatePanel ID="upnlSync" runat="server">
    <ContentTemplate>
        <div class="row">
            <asp:Panel runat="server" ID="pnlExportedDetails" CssClass="col-sm-2" Visible="false">
                <asp:Literal runat="server" ID="litDateExported" Visible="false"></asp:Literal>
                <Rock:BootstrapButton runat="server" Visible="false" ID="btnRemoveDate" Text="Remove Date Exported" OnClick="btnRemoveDateExported_Click" CssClass="btn btn-link pull-left" />
            </asp:Panel>
            <div class="col-sm-2">
                <Rock:RockDropDownList ID="ddlJournalType" runat="server" Label="Journal Type" Required="true" ValidationGroup="KFSGLExport" Visible="false">
                    <asp:ListItem Text="" Value="" Selected="True"></asp:ListItem>
                    <asp:ListItem Text="Cash Receipts" Value="CR"></asp:ListItem>
                    <asp:ListItem Text="Accounts Payable" Value="AP"></asp:ListItem>
                    <asp:ListItem Text="Accounts Receivable" Value="AR"></asp:ListItem>
                    <asp:ListItem Text="Cash Disbursements" Value="CD"></asp:ListItem>
                    <asp:ListItem Text="Check Express" Value="CK"></asp:ListItem>
                    <asp:ListItem Text="Contributions" Value="CN"></asp:ListItem>
                    <asp:ListItem Text="Registrations" Value="RG"></asp:ListItem>
                    <asp:ListItem Text="Journal Entry" Value="JE"></asp:ListItem>
                    <asp:ListItem Text="Payroll" Value="PR"></asp:ListItem>
                    <asp:ListItem Text="Gifts" Value="GF"></asp:ListItem>
                    <asp:ListItem Text="Expense Amortization" Value="AM"></asp:ListItem>
                </Rock:RockDropDownList>
            </div>
            <div class="col-sm-2">
                <Rock:RockTextBox ID="tbAccountingPeriod" runat="server" Label="Accounting Period" Required="true" ValidationGroup="KFSGLExport" Visible="false" TextMode="Number"></Rock:RockTextBox>
            </div>
            <div class="col-sm-2">
                <label>&nbsp;</label>
                <div>
                    <Rock:BootstrapButton runat="server" Visible="false" ID="btnExportToShelbyFinancials" OnClick="btnExportToShelbyFinancials_Click" CssClass="btn btn-primary" ValidationGroup="KFSGLExport" />
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
<iframe id="sfDownload" src="/Plugins/com_kfs/ShelbyFinancials/ShelbyFinancialsExcelExport.aspx" frameborder="0" width="0" height="0"></iframe>
