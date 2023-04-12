<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchesToJournal.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Intacct.BatchesToJournal" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Danger" Visible="true" />

        <asp:Panel ID="pnlBatchesToExport" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-archive"></i>&nbsp;Batches For Export</h1>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:ModalAlert ID="maWarningDialog" runat="server" />
                        <Rock:GridFilter ID="gfBatchesToExportFilter" runat="server">
                            <Rock:RockDropDownList ID="ddlStatus" runat="server" Label="Status" />
                            <Rock:DateRangePicker ID="drpBatchDate" runat="server" Label="Date Range" />
                        </Rock:GridFilter>

                        <Rock:Grid ID="gBatchesToExport" DataKeyNames="Id" runat="server" RowItemText="Batch" OnRowSelected="gBatchesToExport_Click" CssClass="js-grid-batch-list" AllowSorting="true">
                            <Columns>
                                <Rock:SelectField />
                                <Rock:RockBoundField DataField="Id" SortExpression="Id" HeaderText="Id" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" />
                                <Rock:DateField DataField="BatchStartDateTime" SortExpression="BatchStartDateTime" HeaderText="Date" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" />
                                <Rock:RockBoundField DataField="Name" SortExpression="Name" HeaderText="Title" />
                                <Rock:CurrencyField DataField="Total" HeaderText="Total" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" />
                                <Rock:RockBoundField DataField="Transactions" HeaderText="Transactions" DataFormatString="{0:N0}" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" />
                                <Rock:RockBoundField DataField="Status" SortExpression="Status" HeaderText="Status" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" />
                                <Rock:RockBoundField DataField="Note" HeaderText="Note" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>

            <div class="row">
                <asp:Panel runat="server" ID="pnlOtherReceipt" Visible="false">
                    <div class="col-md-3 col-lg-2">
                        <Rock:RockDropDownList ID="ddlReceiptAccountType" runat="server" Label="Deposit To" Required="true" ValidationGroup="KFSIntacctExport" OnSelectedIndexChanged="ddlReceiptAccountType_SelectedIndexChanged" AutoPostBack="true">
                            <asp:ListItem Value="BankAccount" Text="Bank Account"></asp:ListItem>
                            <asp:ListItem Value="UnDepFundAcct" Text="Undeposited Funds"></asp:ListItem>
                        </Rock:RockDropDownList>
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <Rock:RockDropDownList ID="ddlPaymentMethods" runat="server" Label="Payment Method" Required="true" ValidationGroup="KFSIntacctExport" />
                    </div>
                    <asp:Panel runat="server" ID="pnlBankAccounts" CssClass="col-md-3 col-lg-2">
                        <Rock:RockDropDownList ID="ddlBankAccounts" runat="server" Label="Bank Account" Required="true" ValidationGroup="KFSIntacctExport" />
                    </asp:Panel>
                </asp:Panel>
                <div class="col-sm-2">
                    <label>&nbsp;</label>
                    <div>
                        <Rock:BootstrapButton runat="server" ID="btnExportToIntacct" OnClick="btnExportToIntacct_Click" CssClass="btn btn-primary" />
                    </div>
                </div>
            </div>
            <Rock:NotificationBox ID="nbError" runat="server" Visible="false" Dismissable="true"></Rock:NotificationBox>
            <asp:Literal ID="lDebug" runat="server" Visible="false" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
