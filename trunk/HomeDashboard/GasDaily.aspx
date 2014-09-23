<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GasDaily.aspx.cs" Inherits="HomeDashboard.GasDaily1" %>

<%@ Register src="MeterDaily.ascx" tagname="MeterDaily" tagprefix="uc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Расход газа по дням</title>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
</head>
<body>
    <form id="form1" runat="server">
        
        <uc1:MeterDaily ID="gasDailyControl" ColumnName="GasDiff" Title="Расход газа по дням" TitleYAxis="Расход, м3" SeriesName="Расход газа" runat="server" />
        <div style="text-align:center;margin-top:20px;width:100%">
         <a class="btn" href="Menu.html">Назад</a>
        </div>
    </form>
</body>
</html>
