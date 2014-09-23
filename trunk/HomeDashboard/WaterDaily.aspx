<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WaterDaily.aspx.cs" Inherits="HomeDashboard.WaterDaily" %>

<%@ Register src="MeterDaily.ascx" tagname="MeterDaily" tagprefix="uc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Расход воды по дням</title>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
</head>
<body>
    <form id="form1" runat="server">
        
        <uc1:MeterDaily ID="waterDailyControl" ColumnName="WaterDiff" Title="Расход воды по дням" TitleYAxis="Расход, м3" SeriesName="Расход воды" runat="server" />
        <div style="text-align:center;margin-top:20px;width:100%">
         <a class="btn" href="Menu.html">Назад</a>
        </div>
    </form>
</body>
</html>
