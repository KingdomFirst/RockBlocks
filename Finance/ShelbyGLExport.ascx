<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ShelbyGLExport.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Finance.ShelbyGLExport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Danger" Visible="true" />

        <asp:Panel ID="pnlBatchList" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-archive"></i>&nbsp;Batch List</h1>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:ModalAlert ID="maWarningDialog" runat="server" />
                        <Rock:GridFilter ID="gfBatchFilter" runat="server">
                            <Rock:RockDropDownList ID="ddlStatus" runat="server" Label="Status" />
                            <Rock:DateRangePicker ID="drpBatchDate" runat="server" Label="Date Range" />
                            <Rock:CampusPicker ID="campCampus" runat="server" />
                            <Rock:RockDropDownList ID="ddlTransactionType" runat="server" Label="Contains Transaction Type" />
                            <Rock:RockTextBox ID="tbTitle" runat="server" Label="Title"></Rock:RockTextBox>
                            <Rock:RockTextBox ID="tbAccountingCode" runat="server" Label="Accounting Code"></Rock:RockTextBox>
                            <Rock:RockDropDownList ID="ddlBatchExported" runat="server" Label="Batch Exported">
                                <asp:ListItem Text="" Value="" />
                                <asp:ListItem Text="No" Value="No" Selected="True" />
                                <asp:ListItem Text="Yes" Value="Yes" />
                            </Rock:RockDropDownList>
                            <Rock:RockTextBox ID="tbBatchId" runat="server" Label="Batch Id"></Rock:RockTextBox>
                        </Rock:GridFilter>

                        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
                        <Rock:Grid ID="gBatchList" runat="server" RowItemText="Batch" AllowSorting="true" CssClass="js-grid-batch-list">
                            <Columns>
                                <Rock:SelectField />
                                <Rock:RockBoundField DataField="Id" HeaderText="Id" SortExpression="Id" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                <Rock:DateField DataField="BatchStartDateTime" HeaderText="Date" SortExpression="BatchStartDateTime" />
                                <Rock:RockBoundField DataField="Name" HeaderText="Title" SortExpression="Name" />
                                <Rock:RockBoundField DataField="AccountingSystemCode" HeaderText="Accounting Code" SortExpression="AccountingSystemCode" />
                                <Rock:RockBoundField DataField="TransactionCount" HeaderText="<span class='hidden-print'>Transaction Count</span><span class='visible-print-inline'>Txns</span>" HtmlEncode="false" SortExpression="TransactionCount" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Right" />
                                <Rock:CurrencyField DataField="TransactionAmount" HeaderText="<span class='hidden-print'>Transaction Total</span><span class='visible-print-inline'>Txn Total</span>" HtmlEncode="false" SortExpression="TransactionAmount" ItemStyle-HorizontalAlign="Right" />
                                <Rock:RockTemplateField HeaderText="Variance" ItemStyle-HorizontalAlign="Right">
                                    <ItemTemplate>
                                        <span class='<%# (decimal)Eval("Variance") != 0 ? "label label-danger" : "" %>'><%# this.FormatValueAsCurrency((decimal)Eval("Variance")) %></span>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="AccountSummaryHtml" HeaderText="Accounts" HtmlEncode="false" />
                                <Rock:RockBoundField DataField="CampusName" HeaderText="Campus" SortExpression="Campus.Name" ColumnPriority="Desktop" />
                                <Rock:RockTemplateField HeaderText="Status" SortExpression="Status" HeaderStyle-CssClass="grid-columnstatus" ItemStyle-CssClass="grid-columnstatus" FooterStyle-CssClass="grid-columnstatus" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <span class='<%# Eval("StatusLabelClass") %>'><%# Eval("StatusText") %></span>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Notes" HeaderText="Note" HtmlEncode="false" ColumnPriority="Desktop" />
                                <Rock:DateTimeField DataField="batchExportedDT" HeaderText="Date Exported" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-4 margin-t-md">
                    <asp:Panel ID="pnlSummary" runat="server" CssClass="panel panel-block">
                        <div class="panel-heading">
                            <h1 class="panel-title">GL File Export</h1>
                        </div>
                        <div class="panel-body">
                            <Rock:RockDropDownList ID="ddlJournalType" runat="server" Label="Journal Type" Required="true" ValidationGroup="KFSGLExport">
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
                            <Rock:RockTextBox ID="tbAccountingPeriod" runat="server" Label="Accounting Period" Required="true" Width="50" ValidationGroup="KFSGLExport"></Rock:RockTextBox>
                            <Rock:DatePicker ID="dpExportDate" runat="server" Label="Date" Required="true" ValidationGroup="KFSGLExport"></Rock:DatePicker>
                            <div class="actions">
                                <asp:LinkButton ID="btnExport" runat="server" CssClass="btn btn-primary" Text="Export" OnClick="btnExport_Click" ValidationGroup="KFSGLExport" />
                                <asp:LinkButton ID="btnPreview" runat="server" CssClass="btn btn-default" Text="Preview" OnClick="btnPreview_Click" ValidationGroup="KFSGLExport" />
                            </div>
                        </div>

                    </asp:Panel>
                </div>
                <div class="col-md-1"></div>
                <div class="col-md-7 margin-t-md">
                    <div class="panel panel-block">
                        <div class="panel-heading">
                            <h1 class="panel-title" id="batchPreviewHdr" runat="server">Preview</h1>
                        </div>
                        <div class="panel-body">
                            <div class="grid grid-panel">
                                <Rock:Grid ID="BatchExportList" runat="server" RowItemText="Transaction" AllowSorting="false" CssClass="js-grid-batch-list">
                                    <Columns>
                                        <Rock:RockBoundField DataField="CompanyNumber" HeaderText="Company" SortExpression="CompanyNumber" />
                                        <Rock:RockBoundField DataField="FundNumber" HeaderText="Fund" SortExpression="FundNumber" />
                                        <Rock:RockBoundField DataField="AccountNumber" HeaderText="Account" SortExpression="AccountNumber" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                        <Rock:RockBoundField DataField="DepartmentNumber" HeaderText="Dept" SortExpression="DepartmentNumber" />
                                        <Rock:RockBoundField DataField="ProjectCode" HeaderText="Proj" />
                                        <Rock:CurrencyField DataField="Amount" HeaderText="Amount" SortExpression="Amount" ItemStyle-HorizontalAlign="Right" />
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <Rock:NotificationBox ID="nbResult" runat="server" Visible="false" Dismissable="true"></Rock:NotificationBox>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
<iframe width="1" height="1" src="" id="downloadIframe" name="downloadIframe" class="hidden"></iframe>
