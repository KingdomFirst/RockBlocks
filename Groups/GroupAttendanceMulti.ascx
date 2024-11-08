<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceMulti.ascx.cs" Inherits="Plugins.rocks_kfs.Groups.GroupAttendanceMulti" %>
<style>
    .card-checkbox .checkbox {
        padding-left: 0;
    }

        .card-checkbox .checkbox label {
            width: 100%;
        }

            .card-checkbox .checkbox label .label-text:before,
            .card-checkbox .checkbox label .label-text:after {
                display: table;
                content: " ";
                border: 0;
                position: static;
                width: auto;
                height: auto;
                pointer-events: all;
                border-radius: 0;
                user-select: auto;
                background: none;
            }

            .card-checkbox .checkbox label .label-text:after {
                clear: both;
            }

            .card-checkbox .checkbox label .label-text {
                border: solid 1px #000;
                display: block;
                clear: both;
                padding: .5em;
                border-radius: 3px;
                width: 100%;
            }

            .card-checkbox .checkbox label input:checked + .label-text {
                border-color: red;
                background-color: rgba(0,0,0,0.1);
            }
</style>
<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-check-square-o"></i>
                    <asp:Literal ID="lHeading" runat="server" Text="Group Attendance" />
                </h1>
            </div>

            <div class="panel-body">

                <div class="row">
                    <asp:Panel ID="pnlDate" runat="server" CssClass="col-sm-6">
                        <strong>Enter Attendance For:</strong>
                        <asp:Literal ID="lAttendanceDate" runat="server"></asp:Literal>
                    </asp:Panel>
                    <asp:Panel ID="pnlOther" runat="server" class="col-sm-6"></asp:Panel>
                </div>
                <asp:Panel ID="pnlAttendance" runat="server" CssClass="d-flex flex-wrap">
                    <asp:Repeater ID="rptrAttendance" runat="server">
                        <ItemTemplate>
                            <asp:Panel ID="pnlCardCheckbox" runat="server" CssClass="card-checkbox">
                                <Rock:RockCheckBox ID="cbAttendee" runat="server" CssClass="attendeeCheckbox" />
                            </asp:Panel>
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
