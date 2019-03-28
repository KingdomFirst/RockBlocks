<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PluginImport.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Utility.PluginImport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h3>Plugin Import</h3>
            </div>
            <div class="panel-body">
                <div class="form">
                    <div class="col-xs-6" style="padding-right: 30px;">
                        <div class="row">
                            <div class="form-group">
                                <asp:HiddenField ID="hfPluginFileName" runat="server" />
                                <Rock:HiddenFieldValidator ID="hfPluginValidator" runat="server" ControlToValidate="hfPluginFileName" CssClass="danger" ErrorMessage="A Plugin file must be uploaded" Display="None" />
                                <Rock:FileUploader ID="fupPlugin" runat="server" Label="Select Plugin File" IsBinaryFile="false" RootFolder="~/App_Data/Uploads" DisplayMode="DropZone" OnFileUploaded="fupPlugin_FileUploaded" AllowMultipleUploads="false" Help="Accepted file types: .plugin or .zip" />
                            </div>
                        </div>
                    </div>
                    <div class="col-xs-12">
                        <Rock:NotificationBox ID="nbWarning" runat="server" />
                        <div class="row">
                            <div class="panel-group">
                                <div class="actions">
                                    <asp:Button ID="btnImport" runat="server" Text="Import Plugin" OnClick="btnImport_Click" CausesValidation="false" class="btn btn-primary" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
