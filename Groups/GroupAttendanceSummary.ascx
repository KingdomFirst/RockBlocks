<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceSummary.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Groups.GroupAttendanceSummary" %>

<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">
                
            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-check-square-o"></i>
                    <asp:Literal ID="lHeading" runat="server" Text="Group Attendance" />
                </h1>
            </div>

            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:ModalAlert ID="mdGridWarning" runat="server" />
                    <Rock:Grid ID="gOccurrences" runat="server" DisplayType="Full" AllowSorting="true" RowItemText="Occurrence" >
                        <Columns>
                            <Rock:DateField DataField="OccurrenceDate" HeaderText="Date" ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" SortExpression="OccurrenceDate" />
		                    <Rock:RockTemplateField HeaderText="Location" SortExpression="LocationPath,LocationName">
		                        <ItemTemplate>
		                            <%#Eval("LocationName")%><br />
		                            <small><%#Eval("ParentLocationPath")%></small>
		                        </ItemTemplate>
		                    </Rock:RockTemplateField>
                            <Rock:RockBoundField DataField="ScheduleName" HeaderText="Schedule" SortExpression="ScheduleName" />
                            <Rock:BoolField DataField="AttendanceEntered" HeaderText="Attendance Entered" SortExpression="AttendanceEntered" />
                            <Rock:BoolField DataField="DidNotOccur" HeaderText="Didn't Meet" SortExpression="DidNotOccur" />
                            <Rock:RockBoundField DataField="DidAttendCount" HeaderText="Attendance Count" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:N0}" SortExpression="DidAttendCount" HeaderStyle-HorizontalAlign="Right" />
                            <Rock:RockBoundField DataField="AttendanceRate" HeaderText="Percent Attended" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:P0}" SortExpression="AttendanceRate" HeaderStyle-HorizontalAlign="Right"/>
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
