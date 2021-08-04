<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareDashboard.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareDashboard" %>

<asp:UpdatePanel runat="server" ID="upnlCareDashboard">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-hand-holding-heart"></i>Care Needs</h1>
                </div>
                <div class="panel-body">

                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                            <Rock:DateRangePicker ID="drpDate" runat="server" Label="Date Range" />
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                            <Rock:DefinedValuesPicker ID="dvpCategory" runat="server" Label="Category" DataTextField="Value" DataValueField="Id" />
                            <Rock:DefinedValuePicker ID="dvpStatus" runat="server" Label="Status" DataTextField="Value" DataValueField="Id" />
                            <Rock:RockDropDownList ID="ddlSubmitter" runat="server" Label="Submitted By" EnhanceForLongLists="true" />
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                            <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gList" runat="server" DisplayType="Full" AllowSorting="true" OnRowDataBound="gList_RowDataBound" OnRowSelected="gList_Edit" ExportSource="DataSource">
                            <Columns>
                                <Rock:RockTemplateField SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name">
                                    <ItemTemplate>
                                        <asp:Literal ID="lName" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Details" HeaderText="Details" SortExpression="Details" />
                                <Rock:RockBoundField DataField="DateEntered" HeaderText="Date" DataFormatString="{0:d}" SortExpression="DateEntered" />

                                <Rock:DefinedValueField DataField="CategoryValueId" HeaderText="Category" SortExpression="Category.Value" ColumnPriority="DesktopSmall" />

                                <Rock:PersonField DataField="SubmitterPersonAlias.Person" SortExpression="SubmitterPersonAlias.Person.LastName, SubmitterPersonAlias.Person.NickName" HeaderText="Submitted By" ColumnPriority="Tablet" />

                                <Rock:RockTemplateField SortExpression="Status.Value" HeaderText="Status">
                                    <ItemTemplate>
                                        <Rock:HighlightLabel ID="hlStatus" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Campus.Name" HeaderText="Campus" SortExpression="Campus.Name" />
                            </Columns>
                        </Rock:Grid>
                    </div>

                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
