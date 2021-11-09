﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchToJournal.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Intacct.BatchToJournal" %>

<asp:UpdatePanel ID="upnlSync" runat="server">
    <ContentTemplate>
        <div class="row">
            <asp:Panel runat="server" ID="pnlExportedDetails" CssClass="col-sm-2" Visible="false">
                <asp:Literal runat="server" ID="litDateExported" Visible="false"></asp:Literal>
                <Rock:BootstrapButton runat="server" Visible="false" ID="btnRemoveDate" Text="Remove Date Exported" OnClick="btnRemoveDateExported_Click" CssClass="btn btn-link" />
            </asp:Panel>
            <asp:Panel runat="server" ID="pnlOtherReceipt" Visible="false">
                <div class="col-sm-2">
                    <Rock:RockDropDownList ID="ddlReceiptAccountType" runat="server" Label="Deposit To" Required="true" ValidationGroup="KFSIntacctExport" OnSelectedIndexChanged="ddlReceiptAccountType_SelectedIndexChanged" AutoPostBack="true" >
                        <asp:ListItem Value="BankAccount" Text="Bank Account" ></asp:ListItem>
                        <asp:ListItem Value="UnDepFundAcct" Text="Undeposited Funds"></asp:ListItem>
                    </Rock:RockDropDownList>
                </div>
                <div class="col-sm-2">
                    <Rock:RockDropDownList ID="ddlPaymentMethods" runat="server" Label="Payment Method" Required="true" ValidationGroup="KFSIntacctExport" />
                </div>
                <div class="col-sm-2">
                    <Rock:RockDropDownList ID="ddlBankAccounts" runat="server" Label="Bank Account" Required="true" ValidationGroup="KFSIntacctExport" />
                </div>
                <Rock:RockDropDownList ID="ddlUndepositedFundAccounts" runat="server" Label="Bank Account" Required="true" ValidationGroup="KFSIntacctExport" Visible="false" />
            </asp:Panel>
            <div class="col-sm-2">
                <label>&nbsp;</label>
                <div>
                    <Rock:BootstrapButton runat="server" Visible="false" ID="btnExportToIntacct" OnClick="btnExportToIntacct_Click" CssClass="btn btn-primary" ValidationGroup="KFSIntacctExport" />
                </div>
            </div>
            <asp:Literal ID="lDebug" runat="server" Visible="false" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
