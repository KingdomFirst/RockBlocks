<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ExportTool.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Bulldozer.ExportTool" %>

<style>
    #console {
        width: 100%;
        height: 350px;
        min-height: 12px;
        padding: 12px;
        margin-top: 4px;
        margin-bottom: 48px;
        overflow-x: hidden;
        overflow-y: scroll;
        font-family: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
        color: #d3cec5;
        background-color: #282526;
        border-radius: 2px;
    }

        #console ol {
            padding: 0;
            margin-left: 0;
            list-style-type: none;
            transform: rotate(180deg);
        }

            #console ol:first-child {
                counter-reset: customlistcounter;
            }

            #console ol > li {
                font-size: 13px;
                counter-increment: customlistcounter;
                transform: rotate(-180deg);
            }

                #console ol > li span {
                    float: left;
                    width: 90%;
                    overflow: auto;
                    text-overflow: ellipsis;
                    white-space: nowrap;
                }

                #console ol > li::before {
                    float: left;
                    width: 2em;
                    width: 5%;
                    margin-right: 12px;
                    color: #717171;
                    text-align: right;
                    content: counter(customlistcounter) " ";
                }
</style>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="container-fluid">
            <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" Visible="false" />
            <div class="row">
                <h5>Data Views</h5>
                <div class="col-sm-6">
                    <Rock:DataViewItemPicker runat="server" ID="dvpPersonDataView" Label="Person Data View" />
                </div>
                <div class="col-sm-6">
                    <Rock:DataViewItemPicker runat="server" ID="dvpGroupDataView" Label="Group Data View" />
                </div>
            </div>

            <Rock:PanelWidget runat="server" ID="pwAdvanced" Title="Advanced Settings">
                <div class="container-fluid">
                    <div class="row">
                        <h5>Additional Person Items</h5>
                        <div class="col-sm-12 form-inline">
                            <Rock:RockCheckBox runat="server" ID="cbConnectionRequests" Label="Connection Requests" DisplayInline="true" />
                            <Rock:RockCheckBox runat="server" ID="cbPrayerRequets" Label="Prayer Requests" DisplayInline="true" />
                            <Rock:RockCheckBox runat="server" ID="cbUserLogins" Label="User Logins" DisplayInline="true" />
                            <Rock:RockCheckBox runat="server" ID="cbAttendance" Label="Attendance" DisplayInline="true" />
                        </div>
                    </div>
                    <div class="row">
                        <h5>Attributes</h5>
                        <div class="col-sm-5 col-md-4 col-lg-3 col-xl-3">
                            <Rock:DataViewItemPicker runat="server" ID="dvpAttributeDataView" Label="Attribute Data View" Help="By default all Person, Group, Group Member, and Prayer Request attributes will be exported." />
                        </div>
                        <div class="col-xs-6 col-sm-3">
                            <Rock:RockDropDownList runat="server" ID="ddlAttributeIncludeExlude" Label="Attribute Processing" Help="The way that the selected attribute data view will be processed." />
                        </div>
                    </div>
                    <div class="row">
                        <h5>Binary Files</h5>
                        <div class="col-sm-12 form-inline">
                            <Rock:RockCheckBox runat="server" ID="cbPersonImages" Label="Person Images" DisplayInline="true" />
                        </div>
                    </div>
                </div>
            </Rock:PanelWidget>

            <Rock:BootstrapButton runat="server" ID="btnExport" OnClick="btnExport_Click" Text="Create Export Bundle" CssClass="btn btn-primary" />

            <div class="row">
                <asp:Timer ID="tmrSyncExport" runat="server" Interval="1000" OnTick="tmrSyncExport_Tick" Enabled="false" />
                <asp:UpdatePanel ID="pnlExportStatus" runat="server">
                    <ContentTemplate>
                        <div id="console">
                            <ol>
                                <asp:Literal ID="lblExportStatus" runat="server" />
                            </ol>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="tmrSyncExport" EventName="Tick" />
                    </Triggers>
                </asp:UpdatePanel>
            </div>
        </div>
        <iframe runat="server" id="iDownloadCsv" src="DownloadCsv.aspx" frameborder="0" width="0" height="0"></iframe>
        <iframe runat="server" id="iDownloadPersonImage" src="DownloadPersonImage.aspx" frameborder="0" width="0" height="0"></iframe>
    </ContentTemplate>
</asp:UpdatePanel>
