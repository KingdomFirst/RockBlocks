<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceMulti.ascx.cs" Inherits="Plugins.rocks_kfs.Groups.GroupAttendanceMulti" %>
<style>
    .position-sticky-top {
        position: sticky;
        top: 0;
        z-index: 1001;
    }

    @media screen and (min-width: 991px) {
        .position-sticky-top {
            top: 80px;
        }
    }

    .kfs-add-person {
        width: calc(100% - 22px);
        display: inline-block;
        margin-right: 6px;
    }

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
                --checked-border-color: var(--color-primary,#ee7525);
                --checked-background-color: rgba(var(--color-base-primary),0.1);
                background-color: var(--background-color);
                border: 1px solid var(--border-color,var(--input-border));
                display: block;
                clear: both;
                padding: .5em;
                border-radius: 3px;
                width: 100%;
            }

                .card-checkbox .checkbox label .label-text small {
                    font-size: x-small;
                    display: block;
                }

            .card-checkbox .checkbox label input:checked + .label-text {
                --border-color: var(--checked-border-color);
                --background-color: var(--checked-background-color);
                outline: 2px solid var(--checked-border-color);
                outline-offset: -2px
            }
</style>
<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-check-square-o"></i>
                    <asp:Literal ID="lHeading" runat="server" Text="Multiple Group Attendance" />
                </h1>
            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotice" runat="server" />
                <asp:ValidationSummary ID="ValidationSummary1" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                <asp:CustomValidator ID="cvAttendance" runat="server" Display="None" />

                <asp:Literal ID="lIntroLava" runat="server" />

                <div class="row mb-5">
                    <asp:Panel ID="pnlDate" runat="server" CssClass="col-sm-6">
                        <Rock:RockLiteral ID="lAttendanceDate" runat="server" Label="Enter Attendance For" />
                        <Rock:DatePicker ID="dpAttendanceDate" runat="server" Label="Enter Attendance For" AllowFutureDateSelection="false" Required="true" OnSelectDate="dpAttendanceDate_SelectDate" AutoPostBack="true" />
                    </asp:Panel>
                    <asp:Panel ID="pnlOther" runat="server" class="col-sm-6">
                        <Rock:RockLiteral ID="lSchedule" runat="server" Label="Schedule(s)" />
                        <Rock:SchedulePicker ID="spSchedule" runat="server" Label="Schedule" ShowOnlyPublic="false" />
                    </asp:Panel>
                </div>
                <div class="row mb-3">
                    <asp:Panel ID="pnlCount" runat="server" CssClass="col-xs-12 col-sm-3 col-md-2 col-sm-push-9 col-md-push-10 text-center" Style="line-height: 38px;">
                        <strong><asp:Literal ID="lCount" runat="server" /></strong> Attended
                    </asp:Panel>
                    <asp:Panel ID="pnlSearch" runat="server" CssClass="col-xs-12 pb-2 pb-sm-0 col-sm-5 col-md-5 col-sm-pull-3 col-md-pull-2">
                        <Rock:RockTextBox ID="tbSearch" runat="server" OnTextChanged="tbSearch_TextChanged" Placeholder="Search" ToolTip="Enter a last name or a first name/last name combination. Use a comma after the name to search by first name only."></Rock:RockTextBox>
                    </asp:Panel>
                    <asp:Panel ID="pnlAddPerson" runat="server" CssClass="col-xs-12 col-sm-4 col-md-5 col-sm-pull-3 col-md-pull-2">
                        <Rock:RockDropDownList ID="ddlAddPersonGroup" runat="server" OnSelectedIndexChanged="ddlAddPersonGroup_SelectedIndexChanged" AutoPostBack="true"></Rock:RockDropDownList>
                        <Rock:PersonPicker ID="ppAddPerson" runat="server" CssClass="kfs-add-person" OnSelectPerson="ppAddPerson_SelectPerson" />
                        <asp:LinkButton ID="lbClearPerson" runat="server" OnClick="lbClearPerson_Click" CssClass="text-gray-500"><i class="fa fa-times"></i></asp:LinkButton>
                    </asp:Panel>
                </div>
                <div class="row mb-3 bg-white position-sticky-top" runat="server" id="divLastnameButtonRow">
                    <asp:Panel runat="server" ID="lastnameButtons" CssClass="col-xs-12 table-responsive">
                        <div class="btn-group d-flex">
                            <a href="#lastnameA" class="btn btn-lg btn-default">A</a>
                            <a href="#lastnameB" class="btn btn-lg btn-default">B</a>
                            <a href="#lastnameC" class="btn btn-lg btn-default">C</a>
                            <a href="#lastnameD" class="btn btn-lg btn-default">D</a>
                            <a href="#lastnameE" class="btn btn-lg btn-default">E</a>
                            <a href="#lastnameF" class="btn btn-lg btn-default">F</a>
                            <a href="#lastnameG" class="btn btn-lg btn-default">G</a>
                            <a href="#lastnameH" class="btn btn-lg btn-default">H</a>
                            <a href="#lastnameI" class="btn btn-lg btn-default">I</a>
                            <a href="#lastnameJ" class="btn btn-lg btn-default">J</a>
                            <a href="#lastnameK" class="btn btn-lg btn-default">K</a>
                            <a href="#lastnameL" class="btn btn-lg btn-default">L</a>
                            <a href="#lastnameM" class="btn btn-lg btn-default">M</a>
                            <a href="#lastnameN" class="btn btn-lg btn-default">N</a>
                            <a href="#lastnameO" class="btn btn-lg btn-default">O</a>
                            <a href="#lastnameP" class="btn btn-lg btn-default">P</a>
                            <a href="#lastnameQ" class="btn btn-lg btn-default">Q</a>
                            <a href="#lastnameR" class="btn btn-lg btn-default">R</a>
                            <a href="#lastnameS" class="btn btn-lg btn-default">S</a>
                            <a href="#lastnameT" class="btn btn-lg btn-default">T</a>
                            <a href="#lastnameU" class="btn btn-lg btn-default">U</a>
                            <a href="#lastnameV" class="btn btn-lg btn-default">V</a>
                            <a href="#lastnameW" class="btn btn-lg btn-default">W</a>
                            <a href="#lastnameX" class="btn btn-lg btn-default">X</a>
                            <a href="#lastnameY" class="btn btn-lg btn-default">Y</a>
                            <a href="#lastnameZ" class="btn btn-lg btn-default">Z</a>
                        </div>
                    </asp:Panel>
                </div>
                <asp:Panel ID="pnlAttendance" runat="server" CssClass="d-flex flex-wrap">
                    <a name='lastnameA' id='lastnameA'></a>
                    <asp:Repeater ID="rptrAttendance" runat="server">
                        <ItemTemplate>
                            <asp:Literal ID="lAnchor" runat="server" />
                            <asp:HiddenField ID="hdnAttendeeId" runat="server" Value='<%# Eval("PersonId") %>' />
                            <asp:Panel ID="pnlCardCheckbox" runat="server" CssClass="card-checkbox">
                                <Rock:RockCheckBox ID="cbAttendee" runat="server" CssClass="attendeeCheckbox" Checked='<%# Eval("Attended") %>' OnCheckedChanged="cbAttendee_CheckedChanged" AutoPostBack="true" />
                            </asp:Panel>
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
