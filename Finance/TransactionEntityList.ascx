<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TransactionEntityList.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Finance.TransactionEntityList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlTransactionEntities" CssClass="panel panel-block" runat="server">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-money"></i> Transation Entities</h1>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gTransactionEntities" DisplayType="Light" runat="server" AutoGenerateColumns="False" AllowSorting="false" AllowPaging="false" RowItemText="Transaction Entity">
                        <Columns>
                            <Rock:RockBoundField DataField="AccountName" HeaderText="Account" />
                            <Rock:RockBoundField DataField="EntityTypeName" HeaderText="" />
                            <Rock:RockBoundField DataField="Entity" HeaderText="" />
                            <Rock:CurrencyField DataField="Amount" HeaderText="" />
                            <Rock:DeleteField OnClick="gTransactionEntities_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
