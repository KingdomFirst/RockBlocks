<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Spreadsheet.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Import.Spreadsheet" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h3>KFS Spreadsheet Importer</h3>
            </div>

            <div class="panel-body">
                <div class="form">
                    <div class="col-xs-6" style="padding-right: 30px;">
                        <div class="row">
                            <div class="form-group">
                                <asp:HiddenField ID="hfSpreadsheetFileName" runat="server" />
                                <Rock:HiddenFieldValidator ID="hfSpreadsheetValidator" runat="server" ControlToValidate="hfSpreadsheetFileName" CssClass="danger" ErrorMessage="A spreadsheet file must be uploaded" Display="None" />
                                <Rock:FileUploader ID="fupSpreadsheet" runat="server" Label="Select Spreadsheet File" IsBinaryFile="false" RootFolder="~/App_Data/Uploads" DisplayMode="DropZone" OnFileUploaded="fupSpreadsheet_FileUploaded" AllowMultipleUploads="false" />
                            </div>
                        </div>
                    </div>
                    <div class="col-xs-1"></div>
                    <div class="col-xs-3">

                        <%--<div class="row">
                            <div class="form-group">
                                <label runat="server" id="lblParams" for="tbParam1">Additional Parameters:</label>
                                <asp:TextBox runat="server" ID="tbParam1" type="search" class="form-control" required="required" Text="" />
                                <asp:TextBox runat="server" ID="tbParam2" type="search" class="form-control" required="required" Text="" />
                                <asp:TextBox runat="server" ID="tbParam3" type="search" class="form-control" required="required" Text="" />
                                <asp:TextBox runat="server" ID="tbParam4" type="search" class="form-control" required="required" Text="" />
                                <asp:TextBox runat="server" ID="tbParam5" type="search" class="form-control" required="required" Text="" />
                            </div>
                        </div>--%>
                    </div>

                    <div class="col-xs-12">
                        <Rock:NotificationBox ID="nbWarning" runat="server" />

                        <div class="row">
                            <div class="panel-group">
                                <div class="actions">
                                    <asp:Button ID="btnImport" runat="server" Text="Import Spreadsheet" OnClick="btnImport_Click" CausesValidation="false" class="btn btn-primary" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <asp:Timer ID="tmrSyncSQL" runat="server" Interval="50" OnTick="tmrSyncSQL_Tick" Enabled="false" />
                            <asp:UpdatePanel ID="pnlSqlStatus" runat="server">
                                <ContentTemplate>
                                    <asp:Literal ID="lblSqlStatus" runat="server" />
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="tmrSyncSQL" EventName="Tick" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
